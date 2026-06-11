using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Plottables;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class MainWindow : Window
{
    private const float ZoomedMarkerSize = 5;

    private readonly Dictionary<SeriesKey, SeriesState> _series = [];
    private readonly SteppedXAxisViewport _steppedXAxisViewport = new();
    private readonly XRangeAnimator _xRangeAnimator = new();
    private readonly DispatcherTimer _xRangeAnimationTimer;
    private MainWindowViewModel? _attachedViewModel;
    private EventHandler<PlotDataChangedEventArgs>? _plotDataChangedHandler;
    private long _sampleRateVersion;
    private DateTime _sampleRateTime = DateTime.UtcNow;
    private double _sampleRatePerSecond;
    private XAutoscaleMode _lastXAutoscaleMode = XAutoscaleMode.ContinuousFollowNewest;
    private bool _lastAutoScaleX = true;
    private DateTime _lastHoverProcessedUtc = DateTime.MinValue;

    private static readonly TimeSpan MinimumHoverInterval = TimeSpan.FromMilliseconds(33);

    public MainWindow()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => AttachViewModel();
        DataContextChanged += (_, _) => AttachViewModel();
        Closing += async (_, _) =>
        {
            if (_attachedViewModel is not null)
            {
                await _attachedViewModel.DisposeAsync();
            }
        };
        Plot.PointerWheelChanged += PlotPointerInput;
        Plot.PointerPressed += PlotPointerInput;
        Plot.PointerMoved += PlotPointerMoved;
        Plot.PointerExited += (_, _) => HideCursorOverlay();
        Plot.Plot.RenderManager.AxisLimitsChanged += (_, _) => ScheduleMarkerVisibilityRefresh();

        _xRangeAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _xRangeAnimationTimer.Tick += (_, _) => TickXRangeAnimation();
    }

    private void AttachViewModel()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (ReferenceEquals(_attachedViewModel, vm))
        {
            return;
        }

        if (_attachedViewModel is not null && _plotDataChangedHandler is not null)
        {
            _attachedViewModel.PlotDataChanged -= _plotDataChangedHandler;
        }

        _attachedViewModel = vm;
        _plotDataChangedHandler = (_, args) => RefreshPlot(args);
        vm.PlotDataChanged += _plotDataChangedHandler;
        ConfigurePlot();
        vm.Start();
    }

    private void ConfigurePlot()
    {
        foreach (var series in _series.Values)
        {
            series.Remove();
        }

        _series.Clear();
        Plot.Plot.Clear();
        Plot.Plot.Title("Serial CSV Plotter");
        Plot.Plot.XLabel("X");
        Plot.Plot.YLabel("Left");
        Plot.Plot.Axes.Right.Label.Text = "Right";
        Plot.Plot.Axes.Right.IsVisible = true;
        SetPlotAntiAlias(enabled: true);
        _steppedXAxisViewport.Reset();
        ResetXRangeAnimation();
        HideCursorOverlay();
        Plot.Refresh();
    }

    private void RefreshPlot(PlotDataChangedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        SynchronizeSeries(vm);
        foreach (var series in _series.Values)
        {
            UpdateSeriesData(vm, series, args);
        }

        Plot.Plot.ShowLegend();
        UpdateSampleRate(args.BufferVersion);
        ApplyAutoscale(vm, args);
        UpdateMarkerVisibility();
        if (args.Kind is PlotDataChangeKind.Clear)
        {
            HideCursorOverlay();
        }

        Plot.Refresh();
    }

    private void SynchronizeSeries(MainWindowViewModel vm)
    {
        var selected = vm.SelectedTraces.ToArray();

        var selectedKeys = selected.Select(x => new SeriesKey(x.Source, x.Channel.Index, x.Side)).ToHashSet();
        foreach (var key in _series.Keys.Where(key => !selectedKeys.Contains(key)).ToArray())
        {
            _series[key].Remove();
            _series.Remove(key);
        }

        foreach (var selection in selected)
        {
            var key = new SeriesKey(selection.Source, selection.Channel.Index, selection.Side);
            if (_series.ContainsKey(key))
            {
                continue;
            }

            _series.Add(key, CreateSeries(vm, selection));
        }
    }

    private SeriesState CreateSeries(MainWindowViewModel vm, TraceSelection selection)
    {
        var buffer = new FixedXyRingBuffer(vm.BufferCapacity);
        var color = Plot.Plot.Add.GetNextColor();
        var traceBrush = ToBrush(color);
        var older = Plot.Plot.Add.SignalXY(buffer.Xs, buffer.Ys, color);
        var newer = Plot.Plot.Add.SignalXY(buffer.Xs, buffer.Ys, color);
        older.LegendText = $"{selection.Source.DisplayName}: {selection.Channel.Name}";
        newer.LegendText = string.Empty;
        ConfigureSignalMarkers(older, color);
        ConfigureSignalMarkers(newer, color);

        var yAxis = selection.Side == TraceAxisSide.Left ? Plot.Plot.Axes.Left : Plot.Plot.Axes.Right;
        older.Axes.YAxis = yAxis;
        newer.Axes.YAxis = yAxis;

        UpdateSignalRange(older, null);
        UpdateSignalRange(newer, null);
        SetTraceBrush(selection.Channel, selection.Side, traceBrush);

        return new SeriesState(
            selection.Channel,
            selection.Source,
            selection.Side,
            traceBrush,
            buffer,
            () =>
            {
                Plot.Plot.Remove(older);
                Plot.Plot.Remove(newer);
                SetTraceBrush(selection.Channel, selection.Side, null);
            },
            older,
            newer,
            new double[selection.Source.BufferCapacity],
            new double[selection.Source.BufferCapacity]);
    }

    private static void UpdateSeriesData(MainWindowViewModel vm, SeriesState series, PlotDataChangedEventArgs args)
    {
        if (args.Kind is PlotDataChangeKind.Clear)
        {
            series.Buffer.Clear();
            series.LastBufferVersion = series.Source.Buffer.Version;
            series.InvalidateHoverCache();
            series.UpdateSegments();
            return;
        }

        if (args.Kind is PlotDataChangeKind.Append && !args.TryGetSourceVersion(series.Source, out _))
        {
            return;
        }

        var sourceBufferVersion = args.TryGetSourceVersion(series.Source, out var sourceVersion)
            ? sourceVersion
            : series.Source.Buffer.Version;

        var mustRebuild = args.Kind is PlotDataChangeKind.SelectionChanged or PlotDataChangeKind.XChannelChanged or PlotDataChangeKind.Rebuild
            || series.LastBufferVersion < 0
            || !vm.IsBufferVersionAvailable(series.Source, series.LastBufferVersion);

        if (mustRebuild)
        {
            var length = vm.CopyValidPairs(series.Source, series.Channel, series.TempXs, series.TempYs);
            series.Buffer.Rebuild(series.TempXs, series.TempYs, length);
        }
        else if (sourceBufferVersion > series.LastBufferVersion)
        {
            var length = vm.CopyValidPairsSince(series.Source, series.LastBufferVersion, series.Channel, series.TempXs, series.TempYs);
            for (var i = 0; i < length; i++)
            {
                series.Buffer.Append(series.TempXs[i], series.TempYs[i]);
            }
        }

        series.LastBufferVersion = sourceBufferVersion;
        series.InvalidateHoverCache();
        series.UpdateSegments();
    }

    private static void UpdateSignalRange(SignalXY signal, RingIndexRange? range)
    {
        signal.IsVisible = range is not null;
        if (range is { } value)
        {
            signal.Data.MinimumIndex = value.Minimum;
            signal.Data.MaximumIndex = value.Maximum;
        }
    }

    private static void ConfigureSignalMarkers(SignalXY signal, ScottPlot.Color color)
    {
        signal.MarkerColor = color;
        signal.MarkerFillColor = color;
        signal.MarkerLineColor = color;
        signal.MarkerLineWidth = 1;
        signal.MarkerSize = 0;
    }

    private static void SetTraceBrush(ChannelViewModel channel, TraceAxisSide side, IBrush? brush)
    {
        if (side is TraceAxisSide.Left)
        {
            channel.LeftTraceBrush = brush;
        }
        else
        {
            channel.RightTraceBrush = brush;
        }
    }

    private static SolidColorBrush ToBrush(ScottPlot.Color color)
        => new(Avalonia.Media.Color.Parse(color.ToHex()));

    private void ApplyAutoscale(MainWindowViewModel vm, PlotDataChangedEventArgs args)
    {
        if (args.Kind is PlotDataChangeKind.Clear or PlotDataChangeKind.SelectionChanged or PlotDataChangeKind.XChannelChanged
            || vm.XAutoscaleMode != _lastXAutoscaleMode
            || (vm.AutoScaleX && !_lastAutoScaleX))
        {
            _steppedXAxisViewport.Reset();
            ResetXRangeAnimation();
        }

        _lastXAutoscaleMode = vm.XAutoscaleMode;
        _lastAutoScaleX = vm.AutoScaleX;

        if (vm.AutoScaleX && TryGetXExtent(out var minX, out var maxX))
        {
            if (vm.XAutoscaleMode is XAutoscaleMode.SteppedExpansion or XAutoscaleMode.SteppedPan)
            {
                var spacing = TryGetRecentXSpacing(out var value) ? value : double.NaN;
                XRange? visibleRange = TryGetVisibleXRange(out var currentRange) ? currentRange : null;
                var targetRange = _steppedXAxisViewport.Update(
                    minX,
                    maxX,
                    vm.XAutoscaleMode,
                    visibleRange,
                    _sampleRatePerSecond,
                    spacing,
                    vm.SteppedFutureSpaceSeconds);
                if (targetRange is { } xRange)
                {
                    ApplySteppedXRange(xRange);
                }
            }
            else
            {
                ResetXRangeAnimation();
                Plot.Plot.Axes.SetLimitsX(minX, maxX);
            }
        }
        else if (!vm.AutoScaleX)
        {
            ResetXRangeAnimation();
        }

        if (vm.AutoScaleLeftY && HasSeriesData(TraceAxisSide.Left))
        {
            Plot.Plot.Axes.AutoScaleY(Plot.Plot.Axes.Left);
        }

        if (vm.AutoScaleRightY && HasSeriesData(TraceAxisSide.Right))
        {
            Plot.Plot.Axes.AutoScaleY(Plot.Plot.Axes.Right);
        }
    }

    private void ApplySteppedXRange(XRange targetRange)
    {
        if (!TryGetVisibleXRange(out var currentRange))
        {
            SetPlotAntiAlias(enabled: true);
            Plot.Plot.Axes.SetLimitsX(targetRange.Minimum, targetRange.Maximum);
            return;
        }

        if (_xRangeAnimator.Target == targetRange)
        {
            if (_xRangeAnimator.IsActive)
            {
                var range = _xRangeAnimator.Tick(DateTime.UtcNow);
                Plot.Plot.Axes.SetLimitsX(range.Minimum, range.Maximum);
                if (_xRangeAnimator.IsActive)
                {
                    EnsureXRangeAnimationTimer();
                }
                else
                {
                    SetPlotAntiAlias(enabled: true);
                    _xRangeAnimationTimer.Stop();
                }
            }

            return;
        }

        _xRangeAnimator.Retarget(currentRange, targetRange, DateTime.UtcNow);
        EnsureXRangeAnimationTimer();
    }

    private void TickXRangeAnimation()
    {
        if (!_xRangeAnimator.IsActive)
        {
            SetPlotAntiAlias(enabled: true);
            _xRangeAnimationTimer.Stop();
            return;
        }

        SetPlotAntiAlias(enabled: false);
        var range = _xRangeAnimator.Tick(DateTime.UtcNow);
        Plot.Plot.Axes.SetLimitsX(range.Minimum, range.Maximum);
        Plot.Refresh();
        if (!_xRangeAnimator.IsActive)
        {
            SetPlotAntiAlias(enabled: true);
            _xRangeAnimationTimer.Stop();
            Plot.Refresh();
        }
    }

    private void EnsureXRangeAnimationTimer()
    {
        if (!_xRangeAnimationTimer.IsEnabled)
        {
            SetPlotAntiAlias(enabled: false);
            _xRangeAnimationTimer.Start();
        }
    }

    private void ResetXRangeAnimation()
    {
        _xRangeAnimator.Reset();
        _xRangeAnimationTimer.Stop();
        SetPlotAntiAlias(enabled: true);
    }

    private void SetPlotAntiAlias(bool enabled)
    {
        Plot.Plot.Axes.AntiAlias(enabled);
    }

    private void UpdateSampleRate(long bufferVersion)
    {
        var now = DateTime.UtcNow;
        var deltaVersion = bufferVersion - _sampleRateVersion;
        var elapsed = (now - _sampleRateTime).TotalSeconds;
        if (deltaVersion > 0 && elapsed > 0)
        {
            _sampleRatePerSecond = deltaVersion / elapsed;
        }

        _sampleRateVersion = bufferVersion;
        _sampleRateTime = now;
    }

    private bool TryGetXExtent(out double min, out double max)
    {
        min = double.PositiveInfinity;
        max = double.NegativeInfinity;
        foreach (var series in _series.Values)
        {
            if (series.Buffer.TryGetOldestAndNewestX(out var oldestX, out var newestX))
            {
                min = Math.Min(min, Math.Min(oldestX, newestX));
                max = Math.Max(max, Math.Max(oldestX, newestX));
            }
        }

        return NormalizeRange(ref min, ref max);
    }

    private bool HasSeriesData(TraceAxisSide side)
        => _series.Values.Any(x => x.Side == side && x.Buffer.Count > 0);

    private bool TryGetRecentXSpacing(out double spacing)
    {
        spacing = double.NaN;
        foreach (var series in _series.Values)
        {
            if (series.Buffer.TryGetRecentXSpacing(out spacing))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NormalizeRange(ref double min, ref double max)
    {
        if (!double.IsFinite(min) || !double.IsFinite(max))
        {
            return false;
        }

        if (max > min)
        {
            return true;
        }

        var padding = Math.Max(Math.Abs(min) * 0.01, 1);
        min -= padding;
        max += padding;
        return true;
    }

    private bool TryGetVisibleXRange(out XRange range)
    {
        var limits = Plot.Plot.Axes.GetLimits();
        range = new XRange(limits.Left, limits.Right);
        return double.IsFinite(range.Minimum) && double.IsFinite(range.Maximum) && range.Maximum > range.Minimum;
    }

    private bool UpdateMarkerVisibility()
    {
        if (!TryGetVisibleXRange(out var visibleXRange))
        {
            return false;
        }

        var changed = false;
        foreach (var series in _series.Values)
        {
            changed |= series.UpdateMarkerVisibility(visibleXRange);
        }

        return changed;
    }

    private void ScheduleMarkerVisibilityRefresh()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (UpdateMarkerVisibility())
            {
                Plot.Refresh();
            }
        });
    }


    private async void ExportPngClicked(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export plot",
            SuggestedFileName = "serialplot.png",
            DefaultExtension = "png",
            FileTypeChoices = [new FilePickerFileType("PNG image") { Patterns = ["*.png"] }],
        });

        if (file?.Path.LocalPath is { Length: > 0 } path)
        {
            Plot.Plot.SavePng(path, 1600, 900);
        }
    }

    private async void SaveCsvClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.Sources.Count > 1)
        {
            var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Choose folder for captured CSV files",
                AllowMultiple = false,
            });

            if (folder.Count > 0 && folder[0].Path.LocalPath is { Length: > 0 } folderPath)
            {
                await vm.SaveRawCsvAsync(folderPath);
            }

            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save captured CSV",
            SuggestedFileName = "capture.csv",
            DefaultExtension = "csv",
            FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }],
        });

        if (file?.Path.LocalPath is { Length: > 0 } path)
        {
            await vm.SaveRawCsvAsync(path);
        }
    }

    private void ManageSourcesClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var window = new SourceManagerWindow
        {
            DataContext = vm,
        };
        window.Show(this);
    }

    private void PlotPointerInput(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            DisableAutoscaleForPointerLocation(vm, e.GetPosition(Plot));
        }
    }

    private void DisableAutoscaleForPointerLocation(MainWindowViewModel vm, Point position)
    {
        var width = Plot.Bounds.Width;
        var height = Plot.Bounds.Height;
        const double axisPanelSize = 70;

        var onBottomAxis = position.Y >= height - axisPanelSize;
        var onLeftAxis = position.X <= axisPanelSize;
        var onRightAxis = position.X >= width - axisPanelSize;

        if (onBottomAxis && !onLeftAxis && !onRightAxis)
        {
            vm.AutoScaleX = false;
            return;
        }

        if (onLeftAxis && !onBottomAxis)
        {
            vm.AutoScaleLeftY = false;
            return;
        }

        if (onRightAxis && !onBottomAxis)
        {
            vm.AutoScaleRightY = false;
            return;
        }

        vm.AutoScaleX = false;
        vm.AutoScaleLeftY = false;
        vm.AutoScaleRightY = false;
    }

    private void PlotPointerMoved(object? sender, PointerEventArgs e)
    {
        if (IsPointerButtonPressed(e))
        {
            _lastHoverProcessedUtc = DateTime.MinValue;
            HideCursorOverlay();
            return;
        }

        var now = DateTime.UtcNow;
        var position = e.GetPosition(Plot);
        if (now - _lastHoverProcessedUtc < MinimumHoverInterval)
        {
            return;
        }

        _lastHoverProcessedUtc = now;
        ProcessHover(position);
    }

    private static bool IsPointerButtonPressed(PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(null).Properties;
        return properties.IsLeftButtonPressed
            || properties.IsMiddleButtonPressed
            || properties.IsRightButtonPressed
            || properties.IsXButton1Pressed
            || properties.IsXButton2Pressed;
    }

    private void ProcessHover(Point position)
    {
        if (_series.Count == 0)
        {
            HideCursorOverlay();
            return;
        }

        var displayScale = Plot.DisplayScale == 0 ? 1 : Plot.DisplayScale;
        var mousePixelX = position.X * displayScale;
        var mousePixelY = position.Y * displayScale;
        var hitRadius = 30 * displayScale;
        if (!TryGetVisibleXRange(out var visibleXRange))
        {
            HideCursorOverlay();
            return;
        }

        CursorHit? nearest = null;
        foreach (var series in _series.Values)
        {
            var yAxis = series.Side == TraceAxisSide.Left ? Plot.Plot.Axes.Left : Plot.Plot.Axes.Right;
            series.EnsureHoverCache(visibleXRange);
            var xRange = GetCandidateXRange(mousePixelX, mousePixelY, hitRadius, yAxis);
            var point = series.HoverIndex.FindNearest(
                mousePixelX,
                mousePixelY,
                xRange,
                (x, y) =>
                {
                    var pixel = Plot.Plot.GetPixel(new Coordinates(x, y), Plot.Plot.Axes.Bottom, yAxis);
                    return (pixel.X, pixel.Y);
                },
                hitRadius);

            if (point is { } value && (nearest is null || value.DistanceSquared < nearest.Value.DistanceSquared))
            {
                nearest = new CursorHit($"{series.Source.DisplayName}: {series.Channel.Name}", value.X, value.Y, value.DistanceSquared, yAxis, series.TraceBrush);
            }
        }

        if (nearest is not { } hit)
        {
            HideCursorOverlay();
            return;
        }

        var markerPixel = Plot.Plot.GetPixel(new Coordinates(hit.X, hit.Y), Plot.Plot.Axes.Bottom, hit.YAxis);
        ShowCursorOverlay(markerPixel.X / displayScale, markerPixel.Y / displayScale, hit);
    }

    private XRange GetCandidateXRange(double mousePixelX, double mousePixelY, double hitRadius, IYAxis yAxis)
    {
        var left = Plot.Plot.GetCoordinates((float)(mousePixelX - hitRadius), (float)mousePixelY, Plot.Plot.Axes.Bottom, yAxis);
        var right = Plot.Plot.GetCoordinates((float)(mousePixelX + hitRadius), (float)mousePixelY, Plot.Plot.Axes.Bottom, yAxis);
        return new XRange(Math.Min(left.X, right.X), Math.Max(left.X, right.X));
    }

    private void ShowCursorOverlay(double x, double y, CursorHit hit)
    {
        CursorMarker.IsVisible = true;
        CursorLabel.IsVisible = true;
        CursorMarker.Stroke = hit.TraceBrush;
        CursorLabel.BorderBrush = hit.TraceBrush;
        CursorLabelText.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"{hit.SeriesName}: X={hit.X:G6}, Y={hit.Y:G6}");

        Canvas.SetLeft(CursorMarker, x - (CursorMarker.Width / 2));
        Canvas.SetTop(CursorMarker, y - (CursorMarker.Height / 2));

        var labelLeft = Math.Min(Math.Max(0, x + 12), Math.Max(0, CursorOverlay.Bounds.Width - 220));
        var labelTop = Math.Max(0, y - 32);
        Canvas.SetLeft(CursorLabel, labelLeft);
        Canvas.SetTop(CursorLabel, labelTop);
    }

    private void HideCursorOverlay()
    {
        CursorMarker.IsVisible = false;
        CursorLabel.IsVisible = false;
    }

    private readonly record struct SeriesKey(InputSourceViewModel Source, int ChannelIndex, TraceAxisSide Side);

    private readonly record struct CursorHit(string SeriesName, double X, double Y, double DistanceSquared, IYAxis YAxis, IBrush TraceBrush);

    private sealed class SeriesState(
        ChannelViewModel channel,
        InputSourceViewModel source,
        TraceAxisSide side,
        IBrush traceBrush,
        FixedXyRingBuffer buffer,
        Action remove,
        SignalXY older,
        SignalXY newer,
        double[] tempXs,
        double[] tempYs)
    {
        public ChannelViewModel Channel { get; } = channel;
        public InputSourceViewModel Source { get; } = source;
        public TraceAxisSide Side { get; } = side;
        public IBrush TraceBrush { get; } = traceBrush;
        public FixedXyRingBuffer Buffer { get; } = buffer;
        public Action Remove { get; } = remove;
        public double[] TempXs { get; } = tempXs;
        public double[] TempYs { get; } = tempYs;
        public HoverPointIndex HoverIndex { get; } = new();
        public long LastBufferVersion { get; set; } = -1;
        private bool MarkersVisible { get; set; }
        private long HoverCacheBufferVersion { get; set; } = -1;
        private XRange? HoverCacheVisibleXRange { get; set; }

        public void UpdateSegments()
        {
            var segments = Buffer.GetSegments();
            UpdateSignalRange(older, segments.Older);
            UpdateSignalRange(newer, segments.Newer);
        }

        public void InvalidateHoverCache()
        {
            HoverCacheBufferVersion = -1;
            HoverCacheVisibleXRange = null;
        }

        public void EnsureHoverCache(XRange visibleXRange)
        {
            if (HoverCacheBufferVersion == LastBufferVersion && HoverCacheVisibleXRange == visibleXRange)
            {
                return;
            }

            HoverIndex.Rebuild(Buffer.EnumeratePoints(), visibleXRange);
            HoverCacheBufferVersion = LastBufferVersion;
            HoverCacheVisibleXRange = visibleXRange;
        }

        public bool UpdateMarkerVisibility(XRange visibleXRange)
        {
            var shouldShow = VisiblePointMarkerPolicy.ShouldShowMarkers(Buffer.EnumeratePoints(), visibleXRange);
            if (MarkersVisible == shouldShow)
            {
                return false;
            }

            MarkersVisible = shouldShow;
            var markerSize = shouldShow ? ZoomedMarkerSize : 0;
            older.MarkerSize = markerSize;
            newer.MarkerSize = markerSize;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScottPlot;
using ScottPlot.Plottables;
using SerialPlot.Services;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<SeriesKey, SeriesState> _series = [];
    private MainWindowViewModel? _attachedViewModel;
    private EventHandler<PlotDataChangedEventArgs>? _plotDataChangedHandler;

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
        if (vm.FollowNewest)
        {
            Plot.Plot.Axes.AutoScale();
        }

        Plot.Refresh();
    }

    private void SynchronizeSeries(MainWindowViewModel vm)
    {
        var selected = vm.SelectedLeftChannels
            .Select(channel => new SeriesSelection(channel, SeriesSide.Left))
            .Concat(vm.SelectedRightChannels.Select(channel => new SeriesSelection(channel, SeriesSide.Right)))
            .ToArray();

        var selectedKeys = selected.Select(x => new SeriesKey(x.Channel.Index, x.Side)).ToHashSet();
        foreach (var key in _series.Keys.Where(key => !selectedKeys.Contains(key)).ToArray())
        {
            _series[key].Remove();
            _series.Remove(key);
        }

        foreach (var selection in selected)
        {
            var key = new SeriesKey(selection.Channel.Index, selection.Side);
            if (_series.ContainsKey(key))
            {
                continue;
            }

            _series.Add(key, CreateSeries(vm, selection));
        }
    }

    private SeriesState CreateSeries(MainWindowViewModel vm, SeriesSelection selection)
    {
        var buffer = new FixedXyRingBuffer(vm.BufferCapacity);
        var color = Plot.Plot.Add.GetNextColor();
        var older = Plot.Plot.Add.SignalXY(buffer.Xs, buffer.Ys, color);
        var newer = Plot.Plot.Add.SignalXY(buffer.Xs, buffer.Ys, color);
        older.LegendText = selection.Channel.Name;
        newer.LegendText = string.Empty;

        var yAxis = selection.Side == SeriesSide.Left ? Plot.Plot.Axes.Left : Plot.Plot.Axes.Right;
        older.Axes.YAxis = yAxis;
        newer.Axes.YAxis = yAxis;

        UpdateSignalRange(older, null);
        UpdateSignalRange(newer, null);

        return new SeriesState(
            selection.Channel,
            buffer,
            () =>
            {
                Plot.Plot.Remove(older);
                Plot.Plot.Remove(newer);
            },
            older,
            newer,
            new double[vm.BufferCapacity],
            new double[vm.BufferCapacity]);
    }

    private static void UpdateSeriesData(MainWindowViewModel vm, SeriesState series, PlotDataChangedEventArgs args)
    {
        if (args.Kind is PlotDataChangeKind.Clear)
        {
            series.Buffer.Clear();
            series.LastBufferVersion = args.BufferVersion;
            series.UpdateSegments();
            return;
        }

        var mustRebuild = args.Kind is PlotDataChangeKind.SelectionChanged or PlotDataChangeKind.XChannelChanged or PlotDataChangeKind.Rebuild
            || series.LastBufferVersion < 0
            || !vm.IsBufferVersionAvailable(series.LastBufferVersion);

        if (mustRebuild)
        {
            var length = vm.CopyValidPairs(series.Channel, series.TempXs, series.TempYs);
            series.Buffer.Rebuild(series.TempXs, series.TempYs, length);
        }
        else if (args.Kind is PlotDataChangeKind.Append)
        {
            var length = vm.CopyValidPairsSince(series.LastBufferVersion, series.Channel, series.TempXs, series.TempYs);
            for (var i = 0; i < length; i++)
            {
                series.Buffer.Append(series.TempXs[i], series.TempYs[i]);
            }
        }

        series.LastBufferVersion = args.BufferVersion;
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

    private void PlotPointerInput(object? sender, PointerEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.FollowNewest = false;
        }
    }

    private enum SeriesSide
    {
        Left,
        Right,
    }

    private readonly record struct SeriesKey(int ChannelIndex, SeriesSide Side);

    private readonly record struct SeriesSelection(ChannelViewModel Channel, SeriesSide Side);

    private sealed class SeriesState(
        ChannelViewModel channel,
        FixedXyRingBuffer buffer,
        Action remove,
        SignalXY older,
        SignalXY newer,
        double[] tempXs,
        double[] tempYs)
    {
        public ChannelViewModel Channel { get; } = channel;
        public FixedXyRingBuffer Buffer { get; } = buffer;
        public Action Remove { get; } = remove;
        public double[] TempXs { get; } = tempXs;
        public double[] TempYs { get; } = tempYs;
        public long LastBufferVersion { get; set; } = -1;

        public void UpdateSegments()
        {
            var segments = Buffer.GetSegments();
            UpdateSignalRange(older, segments.Older);
            UpdateSignalRange(newer, segments.Newer);
        }
    }
}

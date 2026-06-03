using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ScottPlot;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class MainWindow : Window
{
    private readonly Dictionary<SeriesKey, SeriesState> _series = [];
    private MainWindowViewModel? _attachedViewModel;
    private EventHandler? _plotDataChangedHandler;

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
        _plotDataChangedHandler = (_, _) => RefreshPlot();
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

    private void RefreshPlot()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        SynchronizeSeries(vm);
        foreach (var series in _series.Values)
        {
            UpdateSeriesData(vm, series);
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
        var xs = new double[vm.BufferCapacity];
        var ys = new double[vm.BufferCapacity];
        Array.Fill(xs, double.NaN);
        Array.Fill(ys, double.NaN);

        var scatter = Plot.Plot.Add.Scatter(xs, ys);
        scatter.LegendText = selection.Channel.Name;
        scatter.Axes.YAxis = selection.Side == SeriesSide.Left ? Plot.Plot.Axes.Left : Plot.Plot.Axes.Right;

        return new SeriesState(selection.Channel, xs, ys, () => Plot.Plot.Remove(scatter));
    }

    private static void UpdateSeriesData(MainWindowViewModel vm, SeriesState series)
    {
        var length = vm.CopySeries(series.Channel, series.Xs, series.Ys);
        if (length < series.PreviousLength)
        {
            Array.Fill(series.Xs, double.NaN, length, series.PreviousLength - length);
            Array.Fill(series.Ys, double.NaN, length, series.PreviousLength - length);
        }

        series.PreviousLength = length;
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

    private sealed class SeriesState(ChannelViewModel channel, double[] xs, double[] ys, Action remove)
    {
        public ChannelViewModel Channel { get; } = channel;
        public double[] Xs { get; } = xs;
        public double[] Ys { get; } = ys;
        public Action Remove { get; } = remove;
        public int PreviousLength { get; set; }
    }
}

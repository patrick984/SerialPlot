using System;
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
    private MainWindowViewModel? _attachedViewModel;

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

        _attachedViewModel = vm;
        vm.PlotDataChanged += (_, _) => RefreshPlot();
        ConfigurePlot();
        vm.Start();
    }

    private void ConfigurePlot()
    {
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

        Plot.Plot.Clear();
        foreach (var channel in vm.SelectedLeftChannels)
        {
            var (xs, ys) = vm.GetSeries(channel);
            if (xs.Length == 0)
            {
                continue;
            }

            var scatter = Plot.Plot.Add.Scatter(xs, ys);
            scatter.LegendText = channel.Name;
            scatter.Axes.YAxis = Plot.Plot.Axes.Left;
        }

        foreach (var channel in vm.SelectedRightChannels)
        {
            var (xs, ys) = vm.GetSeries(channel);
            if (xs.Length == 0)
            {
                continue;
            }

            var scatter = Plot.Plot.Add.Scatter(xs, ys);
            scatter.LegendText = channel.Name;
            scatter.Axes.YAxis = Plot.Plot.Axes.Right;
        }

        Plot.Plot.ShowLegend();
        if (vm.FollowNewest)
        {
            Plot.Plot.Axes.AutoScale();
        }

        Plot.Refresh();
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
}

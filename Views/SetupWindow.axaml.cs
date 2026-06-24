using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class SetupWindow : Window
{
    private const double MaxSetupHeightScreenFraction = 0.8;

    private readonly RecentSetupService? _recentSetupService;
    private bool _autoSizeQueued;

    public SetupWindow()
        : this(new SetupWindowViewModel(), null)
    {
    }

    public SetupWindow(SetupWindowViewModel viewModel, RecentSetupService? recentSetupService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _recentSetupService = recentSetupService;
        viewModel.PropertyChanged += ViewModelPropertyChanged;
        Loaded += SetupWindowLoaded;
        Closed += SetupWindowClosed;
    }

    public AppConfig? Config { get; private set; }

    private async void StartClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWindowViewModel vm && vm.TryBuild(out var config))
        {
            Config = config;
            if (_recentSetupService is not null)
            {
                await _recentSetupService.RememberAsync(config.Sources[0]);
            }

            Close(config);
        }
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(null);

    private void SetupWindowLoaded(object? sender, RoutedEventArgs e) => QueueAutoSizeToContent();

    private void SetupWindowClosed(object? sender, EventArgs e)
    {
        if (DataContext is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged -= ViewModelPropertyChanged;
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SetupWindowViewModel.ShowSerialSettings)
            or nameof(SetupWindowViewModel.ShowNetworkSettings)
            or nameof(SetupWindowViewModel.ShowUdpMessage)
            or nameof(SetupWindowViewModel.HasRecentSetups)
            or nameof(SetupWindowViewModel.HasError))
        {
            QueueAutoSizeToContent();
        }
    }

    private void QueueAutoSizeToContent()
    {
        if (_autoSizeQueued)
        {
            return;
        }

        _autoSizeQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _autoSizeQueued = false;
            AutoSizeToContent();
        }, DispatcherPriority.Loaded);
    }

    private void AutoSizeToContent()
    {
        if (Bounds.Width <= 0)
        {
            return;
        }

        SetupContent.Measure(new Size(Bounds.Width, double.PositiveInfinity));
        var desiredHeight = Math.Ceiling(SetupContent.DesiredSize.Height);
        if (desiredHeight <= 0 || double.IsNaN(desiredHeight) || double.IsInfinity(desiredHeight))
        {
            return;
        }

        var maxHeight = GetMaxSetupHeight();
        Height = Math.Clamp(desiredHeight, MinHeight, maxHeight);
    }

    private double GetMaxSetupHeight()
    {
        var screen = Screens.ScreenFromWindow(this);
        var scaling = screen?.Scaling ?? 1;
        var workingHeight = screen?.WorkingArea.Height / scaling;
        return Math.Max(MinHeight, Math.Floor((workingHeight ?? 900) * MaxSetupHeightScreenFraction));
    }
}

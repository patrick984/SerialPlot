using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class SetupWindow : Window
{
    private readonly RecentSetupService? _recentSetupService;

    public SetupWindow()
        : this(new SetupWindowViewModel(), null)
    {
    }

    public SetupWindow(SetupWindowViewModel viewModel, RecentSetupService? recentSetupService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _recentSetupService = recentSetupService;
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
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialPlot.Services;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class SourceManagerWindow : Window
{
    public SourceManagerWindow()
    {
        InitializeComponent();
    }

    private async void AddSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var recentSetupService = new RecentSetupService();
        var setup = new SetupWindow(
            new SetupWindowViewModel(null, await recentSetupService.LoadAsync()),
            recentSetupService);
        await setup.ShowDialog(this);
        if (setup.Config is { } config)
        {
            vm.AddSource(config.Sources[0]);
        }
    }

    private async void RemoveSourceClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel { SelectedSource: { } source } vm)
        {
            await vm.RemoveSourceAsync(source);
        }
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

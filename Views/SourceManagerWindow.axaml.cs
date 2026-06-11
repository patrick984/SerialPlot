using Avalonia.Controls;
using Avalonia.Interactivity;
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

        var setup = new SetupWindow();
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

using Avalonia.Controls;
using Avalonia.Interactivity;
using SerialPlot.Models;
using SerialPlot.ViewModels;

namespace SerialPlot.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
        DataContext = new SetupWindowViewModel();
    }

    public AppConfig? Config { get; private set; }

    private void StartClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWindowViewModel vm && vm.TryBuild(out var config))
        {
            Config = config;
            Close(config);
        }
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SerialPlot.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void CloseClicked(object? sender, RoutedEventArgs e) => Close();
}

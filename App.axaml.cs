using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;
using SerialPlot.Views;

namespace SerialPlot;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var cli = CliConfigParser.Parse(desktop.Args ?? [], Console.IsInputRedirected);
            if (cli.IsComplete)
            {
                desktop.MainWindow = CreateMainWindow(cli.Config);
            }
            else
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                desktop.Startup += async (_, _) =>
                {
                    var recentSetupService = new RecentSetupService();
                    var setupViewModel = cli.HadAnyArgs
                        ? new SetupWindowViewModel(cli.Config, RecentSetupHistory.Empty)
                        : new SetupWindowViewModel(null, await recentSetupService.LoadAsync());
                    var setup = new SetupWindow(setupViewModel, recentSetupService);
                    if (!string.IsNullOrWhiteSpace(cli.Error) && setup.DataContext is SetupWindowViewModel vm)
                    {
                        vm.ErrorMessage = cli.Error;
                    }

                    desktop.MainWindow = setup;
                    setup.Closed += (_, _) =>
                    {
                        if (setup.Config is null)
                        {
                            desktop.Shutdown();
                            return;
                        }

                        desktop.MainWindow = CreateMainWindow(setup.Config);
                        desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                        desktop.MainWindow.Show();
                    };
                    setup.Show();
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainWindow CreateMainWindow(AppConfig config) => new()
    {
        DataContext = new MainWindowViewModel(config),
    };
}

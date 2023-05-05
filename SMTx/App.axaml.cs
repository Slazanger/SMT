using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SMTx.Themes;
using SMTx.ViewModels;
using SMTx.Views;

namespace SMTx;

public class App : Application
{
    public static IThemeManager? ThemeManager;

    public override void Initialize()
    {
        ThemeManager = new FluentThemeManager();
        ThemeManager.Initialize(this);

        AvaloniaXamlLoader.Load(this);
        ThemeManager.Switch(1);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // DockManager.s_enableSplitToWindow = true;

        var mainWindowViewModel = new MainWindowViewModel();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktopLifetime:
            {
                var mainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };

                mainWindow.Closing += (_, _) =>
                {
                    mainWindowViewModel.CloseLayout();
                };

                desktopLifetime.MainWindow = mainWindow;

                desktopLifetime.Exit += (_, _) =>
                {
                    mainWindowViewModel.CloseLayout();
                };
                    
                break;
            }
            case ISingleViewApplicationLifetime singleViewLifetime:
            {
                var mainView = new MainView()
                {
                    DataContext = mainWindowViewModel
                };

                singleViewLifetime.MainView = mainView;

                break;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}

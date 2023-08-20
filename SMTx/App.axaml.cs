using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SMT.EVEData;
using SMTx.Themes;
using SMTx.ViewModels;
using SMTx.Views;




namespace SMTx;

public class App : Application
{
    public static IThemeManager? ThemeManager;


    public EveManager EVEManager { get; set; }

    private void CreateEVEManager()
    {

        EVEManager = new EveManager(EveAppConfig.SMT_VERSION);
        EveManager.Instance = EVEManager;

        EVEManager.LoadFromDisk();
        EVEManager.SetupIntelWatcher();
        EVEManager.SetupGameLogWatcher();
        EVEManager.SetupLogFileTriggers();
        EVEManager.LoadJumpBridgeData();
        EVEManager.UpdateESIUniverseData();
        EVEManager.InitNavigation();
        EVEManager.UpdateMetaliminalStorms();
    }



    public override void Initialize()
    {
        CreateEVEManager();
        ThemeManager = new FluentThemeManager();
        ThemeManager.Initialize(this);

        AvaloniaXamlLoader.Load(this);
        ThemeManager.Switch(1);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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

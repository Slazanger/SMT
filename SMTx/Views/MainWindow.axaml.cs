using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ESI.NET.Models.FactionWarfare;
using SMT.EVEData;

namespace SMTx.Views;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        // Set the windows title 
        Title = $"SMT - {SMT.EVEData.EveAppConfig.SMT_TITLE} ({SMT.EVEData.EveAppConfig.SMT_VERSION})";
    }
}

using System;
using System.Windows;

namespace SMT
{
    /// <summary>
    /// Interaction logic for LogonWindow.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {
        public LogonWindow()
        {
            InitializeComponent();
        }

        private void LogonBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            // Catch any custom auth schemes and pass onto the
            string scheme = e.Uri.Scheme;
            if (scheme == "eveauth-smt")
            {
                // issue the close after the esi auth event has finished
                EVEData.EveManager.Instance.HandleEveAuthSMTUri(new Uri(e.Uri.AbsoluteUri));
                e.Cancel = true;
                Close();
            }
        }
    }
}
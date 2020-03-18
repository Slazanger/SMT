using System.Diagnostics;
using System.Windows;

namespace SMT
{
    /// <summary>
    /// Interaction logic for NewVersion.xaml
    /// </summary>
    public partial class NewVersionWindow : Window
    {
        public NewVersionWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        public string CurrentVersion
        {
            get;
            set;
        }

        public string NewVersion
        {
            get;
            set;
        }

        public string ReleaseInfo
        {
            get;
            set;
        }

        public string ReleaseURL
        {
            get;
            set;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Application.Current.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
        }
    }
}
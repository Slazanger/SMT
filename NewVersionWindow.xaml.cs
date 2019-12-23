using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SMT
{
    /// <summary>
    /// Interaction logic for NewVersion.xaml
    /// </summary>
    public partial class NewVersionWindow : Window
    {
        public string NewVersion
        {
            get;
            set;
        }

        public string CurrentVersion
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



        public NewVersionWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window mainWindow = Application.Current.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;
        }


        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

    }
}

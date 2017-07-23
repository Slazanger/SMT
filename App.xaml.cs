using System;
using System.Threading;
using System.Windows;

namespace SMT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex SingleAppInstanceMutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool newInstance = false;
            SingleAppInstanceMutex = new Mutex(true, "SMT Map Tool", out newInstance);
            if (!newInstance)
            {
                MessageBox.Show("SMT is already running");

                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    Uri uri = null;
                    // we have a url to handle..
                    try
                    {
                        uri = new Uri(args[1].Trim());
                    }
                    catch (UriFormatException)
                    {
                    }

                    EVEData.IUriHandler handler = EVEData.ESIAuthURIHandler.GetHandler();
                    if (handler != null && uri !=null)
                    {
                        handler.HandleUri(uri);
                    }
                }


                App.Current.Shutdown();
            }
        }

    }
}

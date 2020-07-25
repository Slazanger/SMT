using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SMT
{
    /// <summary>
    /// Interaction logic for LogonWindow.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {
        private HttpListener listener;

        public LogonWindow()
        {
            InitializeComponent();
            new Task(StartServer).Start();
        }

        private void StartServer()
        {
            // create the http Server
            listener = new HttpListener();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string challengeCode = Utils.RandomString(32);
            string esiLogonURL = EVEData.EveManager.Instance.GetESILogonURL(challengeCode);
            System.Diagnostics.Process.Start(esiLogonURL);


            try
            {
                listener.Prefixes.Add(EVEData.EveAppConfig.CallbackURL);
                listener.Start();
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;

                EVEData.EveManager.Instance.HandleEveAuthSMTUri(request.Url, challengeCode);

                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                string responseString = "<HTML><BODY>SMT Character Added, please close</BODY></HTML>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
                listener.Stop();

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    // now close the window
                    Close();
                }), DispatcherPriority.Normal, null);
            }
            catch
            {
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }
    }
}
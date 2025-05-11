using System;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

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

        private bool serverDone = false;

        private void StartServer()
        {
            // create the http Server
            listener = new HttpListener();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string challengeCode = EVEDataUtils.Misc.RandomString(32);
            string esiLogonURL = EVEData.EveManager.Instance.GetESILogonURL(challengeCode);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(esiLogonURL) { UseShellExecute = true });

            try
            {
                listener.Prefixes.Add(EVEData.EveAppConfig.CallbackURL);
                listener.Start();

                while (!serverDone)
                {
                    Console.WriteLine("Listening...");

                    // Note: The GetContext method blocks while waiting for a request.
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    EVEData.EveManager.Instance.HandleEveAuthSMTUri(request.Url, challengeCode);

                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    // Construct a response.
                    //                    string responseString = $"<HTML><BODY>SMT Character Added.. close logon window when done or click <a href=\"{esiLogonURL}\"> here </a> to add another character</BODY></HTML>";
                    string responseString = $"<HTML><HEAD title=\"SMT Auth\"><meta http-equiv=\"refresh\" content=\"1;url={esiLogonURL}\"></HEAD><BODY>SMT Character Added..</HTML>";

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                }
            }
            catch
            {
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                serverDone = true;

                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                }
            }
            catch
            {
            }
        }
    }
}
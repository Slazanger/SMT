using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EVEDataUtils;

namespace SMT
{
    /// <summary>
    /// Interaction logic for LogonWindow.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {
        private HttpListener listener;
        private readonly CancellationTokenSource cancellationSource = new CancellationTokenSource();
        private readonly Task serverTask;

        public LogonWindow()
        {
            InitializeComponent();
            serverTask = StartServerAsync(cancellationSource.Token);
        }

        private async Task StartServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Create the local callback server before opening the browser so startup failures are observable.
                listener = new HttpListener();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string challengeCode = EVEDataUtils.Misc.RandomString(32);
                string esiLogonURL = EVEData.EveManager.Instance.GetESILogonURL(challengeCode);

                listener.Prefixes.Add(EVEData.EveAppConfig.CallbackURL);
                listener.Start();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(esiLogonURL) { UseShellExecute = true });

                while(!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context = await listener.GetContextAsync().WaitAsync(cancellationToken);
                    HttpListenerRequest request = context.Request;

                    await EVEData.EveManager.Instance.HandleEveAuthSMTUriAsync(request.Url, challengeCode);

                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    // Construct a response.
                    //                    string responseString = $"<HTML><BODY>SMT Character Added.. close logon window when done or click <a href=\"{esiLogonURL}\"> here </a> to add another character</BODY></HTML>";
                    string responseString = $"<HTML><HEAD title=\"SMT Auth\"><meta http-equiv=\"refresh\" content=\"1;url={esiLogonURL}\"></HEAD><BODY>SMT Character Added..</HTML>";

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    await using System.IO.Stream output = response.OutputStream;
                    await output.WriteAsync(buffer, cancellationToken);
                }
            }
            catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested)
            {
            }
            catch(HttpListenerException) when(cancellationToken.IsCancellationRequested)
            {
            }
            catch(Exception exception)
            {
                AppLog.Error("ESI login listener", exception);
            }
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                cancellationSource.Cancel();

                if (listener != null && listener.IsListening)
                {
                    listener.Stop();
                }
                listener?.Close();
                await serverTask;
            }
            catch(Exception exception)
            {
                AppLog.Error("Close ESI login listener", exception);
            }
            finally
            {
                cancellationSource.Dispose();
            }
        }
    }
}

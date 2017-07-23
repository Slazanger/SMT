using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Web;

namespace SMT.EVEData
{
    public interface IUriHandler
    {
        bool HandleUri(Uri uri);
    }


    public class ESIAuthURIHandler  : MarshalByRefObject, IUriHandler
    {
        const string ipcChannel = "SMT_ESI_URL_Handler";


        public bool HandleUri(Uri uri)
        {
            // parse the uri
            var Query = HttpUtility.ParseQueryString(uri.Query);

            if(Query["state"] == null || Int32.Parse(Query["state"]) != Process.GetCurrentProcess().Id)
            {
                // this query isnt for us..
                return false;
            }

            if(Query["code"] == null)
            {
                // we're missing a query code
                return false;
            }


            // now we have the initial uri call back we can verify the auth code

            string url = @"https://login.eveonline.com/oauth/token";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Timeout = 20000;
            request.Proxy = null;

            string code = Query["code"];
            string clientID = "ace68fde71fc4749bb27f33e8aad0b70";
            string secretKey = "kT7fsRg8WiRb9lujedQVyKEPgaJr40hevUdTdKaF";
            string authHeader = clientID + ":" + secretKey;
            string authHeader_64 = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            request.Headers[HttpRequestHeader.Authorization] = authHeader_64;

            var httpData = HttpUtility.ParseQueryString(string.Empty); ;
            httpData["grant_type"] = "authorization_code";
            httpData["code"] = code;

            string httpDataStr = httpData.ToString();
            byte[] data = UTF8Encoding.UTF8.GetBytes(httpDataStr);
            request.ContentLength = data.Length;
            request.ContentType = "application/x-www-form-urlencoded";

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);






            request.BeginGetResponse(new AsyncCallback(ESIValidateAuthCodeCallback), request);


        


            return true;
        }

        private void ESIValidateAuthCodeCallback(IAsyncResult asyncResult)
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult))
                {
                    Stream responseStream = response.GetResponseStream();
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        //Need to return this response 
                        string strContent = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                /// ....
            }
        }



        // register this instance of the app as the handler for the esi protocol, any other instances will then pass onto this
        public static bool Register()
        {
            try
            {
                IpcServerChannel channel = new IpcServerChannel(ipcChannel);
                ChannelServices.RegisterChannel(channel, true);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(ESIAuthURIHandler), "ESIURIHandler", WellKnownObjectMode.SingleCall);

                return true;
            }
            catch
            {
                // something went wrong
            }

            return false;
        }

        /// Returns the URI handler from the initial instance of the application, or null nothing else..
        public static IUriHandler GetHandler()
        {
            try
            {
                IpcClientChannel channel = new IpcClientChannel();
                ChannelServices.RegisterChannel(channel, true);
                string address = String.Format("ipc://{0}/ESIURIHandler", ipcChannel);
                IUriHandler handler = (IUriHandler)RemotingServices.Connect(typeof(IUriHandler), address);

                // need to test whether connection was established
                TextWriter.Null.WriteLine(handler.ToString());

                return handler;
            }
            catch
            {
            }

            return null;
        }

    }


}

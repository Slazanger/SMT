using System;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace SMT.EVEData
{
    public interface IUriHandler
    {
        bool HandleUri(Uri uri);
    }


    public class ESIURIHandler  : MarshalByRefObject, IUriHandler
    {
        const string ipcChannel = "SMT_ESI_URL_Handler";


        public bool HandleUri(Uri uri)
        {
            return true;
        }


        // register this instance of the app as the handler for this, any others will be passed to it
        public static bool Register()
        {
            try
            {
                IpcServerChannel channel = new IpcServerChannel(ipcChannel);
                ChannelServices.RegisterChannel(channel, true);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(ESIURIHandler), "ESIURIHandler", WellKnownObjectMode.SingleCall);

                return true;
            }
            catch
            {
                // something went wrong
            }

            return false;
        }

        /// Returns the URI handler from the singular instance of the application, or null if there is no other instance.
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

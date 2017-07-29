using CefSharp;
using System;
using System.Windows;

namespace SMT
{
    /// <summary>
    /// Interaction logic for LogonWindow.xaml
    /// </summary>
    public partial class LogonWindow : Window
    {

        static EveAuthSMTSchemeFactory eveAuthSchemeHandlerFactory; 

        static public void InitCef()
        {
            eveAuthSchemeHandlerFactory = new EveAuthSMTSchemeFactory();

            // setup Cef
            CefSettings settings = new CefSettings();
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = EveAuthSMTSchemeFactory.SchemeName,
                SchemeHandlerFactory = eveAuthSchemeHandlerFactory,
            });

            Cef.Initialize(settings);
        }


        public LogonWindow()
        {
            InitializeComponent();

            // stop the right click menus
            logonBrowser.MenuHandler = new CustomMenuHandler();

            eveAuthSchemeHandlerFactory.ActiveLogonWindow = this;

        }


        


        ~LogonWindow()
        {
        }
    }


    public class CustomMenuHandler : IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
        }

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {

        }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }





    public class EveAuthSMTSchemeFactory : ISchemeHandlerFactory
    {
        public const string SchemeName = "eveauth-smt";

        // hacky way to get back to the orig window so we can close it
        public LogonWindow ActiveLogonWindow;



        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            string smtauthURL = request.Url;

            // issue the close after the esi auth event has finished
            Application.Current.Dispatcher.Invoke(() =>
            {
                EVEData.EveManager.GetInstance().HandleEveAuthSMTUri(new Uri(smtauthURL));
                ActiveLogonWindow.Close();
            });
            return null;
        }
    }
}

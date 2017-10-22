using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp.WinForms;
using CefSharp;


namespace SpeckleRhino
{
    public class Interop
    {
        private static ChromiumWebBrowser Browser;
        private static WinForm mainForm;

        public Interop(ChromiumWebBrowser _originalBrowser, WinForm _mainForm)
        {
            Browser = _originalBrowser;
            mainForm = _mainForm;
        }

        public void ShowDev()
        {
            Browser.ShowDevTools();
        }

        // JS Calls these:

        public string GetUserAccounts()
        {
            return "Hello world.";
        }

        public string GetFileStreams()
        {
            return "lol";
        }


        public void AddAccount()
        {

        }

        public void RemoveAccount()
        {

        }

        public void AddClient()
        {

        }

        public void RemoveClient()
        {

        }

    }
}

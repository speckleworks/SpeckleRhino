using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Rhino;

namespace SpeckleRhino
{
    public partial class WinForm : Form
    {
        public ChromiumWebBrowser chromeBrowser;

        public WinForm()
        {
            InitializeComponent();
            // Start the browser after initialize global component
            InitializeChromium();
        }

        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            settings.RemoteDebuggingPort = 8088;
            // Initialize cef with the provided settings
            Cef.Initialize(settings);

            // Create a browser component
            //chromeBrowser = new ChromiumWebBrowser(@"https://speckle.works");
            chromeBrowser = new ChromiumWebBrowser(@"http://10.211.55.2:9090/");
           

            // Add it to the form and fill it to the form window.
            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            // Allow the use of local resources in the browser
            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browserSettings.UniversalAccessFromFileUrls = CefState.Enabled;
            chromeBrowser.BrowserSettings = browserSettings;

            chromeBrowser.RegisterAsyncJsObject("Interop", new Interop(chromeBrowser, this));
        }

        private void WinForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Action ShutDownCef = () =>
            {
                Cef.Shutdown();
            };

            Rhino.RhinoApp.MainApplicationWindow.Invoke(ShutDownCef);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
            base.OnFormClosing(e);
        }
    }
}

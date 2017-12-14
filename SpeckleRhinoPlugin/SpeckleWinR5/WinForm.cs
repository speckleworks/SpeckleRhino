using Newtonsoft.Json;
using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Rhino;
using System.Reflection;
using System.IO;

namespace SpeckleRhino
{
    public partial class WinForm : Form
    {
        public ChromiumWebBrowser chromeBrowser;
        public Interop Store;


        public WinForm()
        {
            InitializeComponent();
            // Start the browser after initialize global component
            InitializeChromium();
        }

        public void InitializeChromium()
        {
            Cef.EnableHighDPISupport();

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyPath = Path.GetDirectoryName(assemblyLocation);
            string pathSubprocess = Path.Combine(assemblyPath, "CefSharp.BrowserSubprocess.exe");

            CefSettings settings = new CefSettings();
            settings.LogSeverity = LogSeverity.Verbose;
            settings.LogFile = "ceflog.txt";
            settings.BrowserSubprocessPath = pathSubprocess;
            settings.CefCommandLineArgs.Add("disable-gpu", "1");


            // Initialize cef with the provided settings
            Cef.Initialize(settings);

            // Create a browser component. 
            // Change the below to wherever your webpack ui server is running.
            //chromeBrowser = new ChromiumWebBrowser(@"http://10.211.55.2:9090/");
            chromeBrowser = new ChromiumWebBrowser(@"http://localhost:9090/");
            // Add it to the form and fill it to the form window.

            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            // Allow the use of local resources in the browser
            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browserSettings.UniversalAccessFromFileUrls = CefState.Enabled;

            Store = new Interop(chromeBrowser, this);

            chromeBrowser.RegisterAsyncJsObject("Interop", Store);
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

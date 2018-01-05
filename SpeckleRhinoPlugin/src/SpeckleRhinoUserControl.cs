using System;
using System.Windows.Forms;
using CefSharp.WinForms;
using CefSharp;
using System.Reflection;
using System.IO;
using Rhino;
using System.Net;
using System.Diagnostics;

namespace SpeckleRhino
{
    /// <summary>
    /// This is the user control that is buried in the tabbed, docking panel.
    /// </summary>
    [System.Runtime.InteropServices.Guid("5736E01B-1459-48FF-8021-AE8E71257795")]
    public partial class SpeckleRhinoUserControl : UserControl
    {

        public ChromiumWebBrowser chromeBrowser;
        public Interop Store;

        public bool CefInit = false;
        /// <summary>
        /// Public constructor
        /// </summary>
        public SpeckleRhinoUserControl()
        {
            InitializeComponent();
            // Start the browser after initialize global component
            InitializeChromium();

            // Set the user control property on our plug-in
            SpecklePlugIn.Instance.PanelUserControl = this;

            //When Rhino closes, we need to shutdown Cef.
            RhinoApp.Closing += OnClosing;
        }

        public void InitializeChromium()
        {

            Cef.EnableHighDPISupport();

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyPath = Path.GetDirectoryName(assemblyLocation);
            string pathSubprocess = Path.Combine(assemblyPath, "CefSharp.BrowserSubprocess.exe");

            CefSettings settings = new CefSettings
            {
                LogSeverity = LogSeverity.Verbose,
                LogFile = "ceflog.txt",
                BrowserSubprocessPath = pathSubprocess,
            };
#if WINR5
            //Not needed in Rhino 6
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
#endif

            // Initialize cef with the provided settings
            if (!Cef.IsInitialized)
                Cef.Initialize(settings);

            // Create a browser component.

            

#if DEBUG
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"http://localhost:9090/");
            request.Timeout = 100;
            request.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // IF NORMAL PERSON 
                    chromeBrowser = new ChromiumWebBrowser(@"http://localhost:9090/");
                }
            }
            catch (WebException)
            {
                chromeBrowser = new ChromiumWebBrowser(@"http://localhost:9090/");
                // IF DIMITRIE ON PARALLELS
                //chromeBrowser = new ChromiumWebBrowser(@"http://10.211.55.2:9090/");
            }
#else


            //#IF RELEASE
            // TODO: Load app from local file

            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            Debug.WriteLine(path, "SPK");

            var indexPath = string.Format(@"{0}\app\index.html", path);

            if (!File.Exists(indexPath))
                Debug.WriteLine("Speckle for Rhino: Error. The html file doesn't exists : {0}", "SPK");

            indexPath = indexPath.Replace("\\", "/");

            chromeBrowser = new ChromiumWebBrowser(indexPath);

            //chromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged;
#endif
            // Allow the use of local resources in the browser
            chromeBrowser.BrowserSettings = new BrowserSettings
            {
                FileAccessFromFileUrls = CefState.Enabled,
                UniversalAccessFromFileUrls = CefState.Enabled
            };

            this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            Store = new Interop(chromeBrowser, this);

            chromeBrowser.RegisterAsyncJsObject("Interop", Store);
            
        }

        private void ChromeBrowser_IsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized)
                chromeBrowser.ShowDevTools();
        }

        private void OnClosing(object sender, EventArgs e)
        {
            chromeBrowser.Dispose();
            Cef.Shutdown();
            SpecklePlugIn.Instance.PanelUserControl = null;
        }

        /// <summary>
        /// Returns the ID of this panel.
        /// </summary>
        public static Guid PanelId
        {
            get
            {
                return typeof(SpeckleRhinoUserControl).GUID;
            }
        }
    }
}


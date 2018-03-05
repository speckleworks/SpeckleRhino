using System;
using System.IO;
using CefSharp;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using System.Reflection;
using CefSharp.WinForms;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using SpeckleRhinoConverter;

namespace SpeckleRhino
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class SpecklePlugIn : Rhino.PlugIns.PlugIn
    {

        public static Interop Store;
        public static ChromiumWebBrowser Browser;

        public SpecklePlugIn()
        {
            Instance = this;
            var hack = new ConverterHack();
        }

        ///<summary>Gets the only instance of the TestEtoWebkitPlugIn plug-in.</summary>
        public static SpecklePlugIn Instance
        {
            get; private set;
        }

        /// <summary>
        /// The tabbed dockbar user control
        /// </summary>
        public SpeckleRhinoUserControl PanelUserControl { get; set; }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            var panel_type = typeof(SpeckleRhinoUserControl);

            Panels.RegisterPanel(this, panel_type, "Speckle", SpeckleRhino.Properties.Resources.Speckle);
            // initialise cef
            if (!Cef.IsInitialized)
                InitializeCef();

            // initialise one browser instance
            InitializeChromium();

            // initialise one store
            Store = new Interop(Browser);

            // make them talk together
            Browser.RegisterAsyncJsObject("Interop", SpecklePlugIn.Store);

            return base.OnLoad(ref errorMessage);
        }

        protected override void OnShutdown()
        {
            Browser.Dispose();
            Cef.Shutdown();

            Store.Dispose();
            SpecklePlugIn.Instance.PanelUserControl?.Dispose();
            base.OnShutdown();
        }

        void InitializeCef()
        {

            Cef.EnableHighDPISupport();

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyPath = Path.GetDirectoryName(assemblyLocation);
            var pathSubprocess = Path.Combine(assemblyPath, "CefSharp.BrowserSubprocess.exe");
            CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            var settings = new CefSettings
            {
                LogSeverity = LogSeverity.Verbose,
                LogFile = "ceflog.txt",
                BrowserSubprocessPath = pathSubprocess
            };

#if WINR5
            //Not needed in Rhino 6
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
#endif

            // Initialize cef with the provided settings

            Cef.Initialize(settings);

        }


        public void InitializeChromium()
        {

#if DEBUG

      HttpWebRequest request = ( HttpWebRequest ) WebRequest.Create( @"http://localhost:9090/" );
      request.Timeout = 100;
      request.Method = "HEAD";
      HttpWebResponse response;
      try
      {
        response = ( HttpWebResponse ) request.GetResponse();
        var copy = response;
        Browser = new ChromiumWebBrowser( @"http://localhost:9090/" );
      }
      catch ( WebException )
      {
        //Browser = new ChromiumWebBrowser(@"http://localhost:9090/");
        // IF DIMITRIE ON PARALLELS
        Browser = new ChromiumWebBrowser( @"http://10.211.55.2:9090/" );
      }

#else
            var path = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            Debug.WriteLine(path, "SPK");

            var indexPath = string.Format(@"{0}\app\index.html", path);

            if (!File.Exists(indexPath))
                Debug.WriteLine("Speckle for Rhino: Error. The html file doesn't exists : {0}", "SPK");

            indexPath = indexPath.Replace("\\", "/");

            Browser = new ChromiumWebBrowser(indexPath);

            //chromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged;
#endif

            // Allow the use of local resources in the browser
            Browser.BrowserSettings = new BrowserSettings
            {
                FileAccessFromFileUrls = CefState.Enabled,
                UniversalAccessFromFileUrls = CefState.Enabled
            };


            Browser.Dock = DockStyle.Fill;
        }

    }
}

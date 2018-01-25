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
  [System.Runtime.InteropServices.Guid( "5736E01B-1459-48FF-8021-AE8E71257795" )]
  public partial class SpeckleRhinoUserControl : UserControl
  {

    public ChromiumWebBrowser chromeBrowser;
    public Interop Store;

    public bool CefInit = false;

    public SpeckleRhinoUserControl( )
    {
      InitializeComponent();
      // Start the browser after initialize global component
      InitializeChromium();
      
      // Set the user control property on our plug-in
      SpecklePlugIn.Instance.PanelUserControl = this;

      Rhino.RhinoDoc.BeginOpenDocument += RhinoDoc_BeginOpenDocument;
    }

    // important to flush this before any open happens:
    // somehow this instance of UserControl never really gets disposed.
    // it still exists - somewhere. 
    private void RhinoDoc_BeginOpenDocument( object sender, DocumentOpenEventArgs e )
    {
      MessageBox.Show( "Begin open doc!" );
      Store?.Dispose();
      Store = null;

      chromeBrowser.Dispose();
      Rhino.RhinoDoc.BeginOpenDocument -= RhinoDoc_BeginOpenDocument;
      this.Dispose();
    }

    public void InitializeChromium( )
    {
      MessageBox.Show( "Initialising Chromium! " + Cef.IsInitialized );
      if ( Cef.IsInitialized )
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
          chromeBrowser = new ChromiumWebBrowser( @"http://localhost:9090/" );
        }
        catch ( WebException )
        {
          //chromeBrowser = new ChromiumWebBrowser( @"http://localhost:9090/" );
          // IF DIMITRIE ON PARALLELS
          chromeBrowser = new ChromiumWebBrowser( @"http://10.211.55.2:9090/" );
        }
#else
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

        this.Controls.Add( chromeBrowser );
        chromeBrowser.Dock = DockStyle.Fill;

        if ( Store == null )
          Store = new Interop( chromeBrowser );
        else
        {
          Store.SpeckleIsReady = false;
          Store.Browser = chromeBrowser;
        }

        chromeBrowser.RegisterAsyncJsObject( "Interop", Store );
      }
      else
      {
        Debug.WriteLine( "For some reason, Cef didn't initialize", "SPK" );
      }

    }

    /// <summary>
    /// Returns the ID of this panel.
    /// </summary>
    public static Guid PanelId
    {
      get
      {
        return typeof( SpeckleRhinoUserControl ).GUID;
      }
    }
  }
}


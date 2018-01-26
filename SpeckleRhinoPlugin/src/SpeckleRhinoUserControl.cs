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

    public bool CefInit = false;

    public int myPanelId = 0;

    public SpeckleRhinoUserControl( )
    {
      myPanelId = ( new Random() ).Next( 0, 10000 );
      Debug.WriteLine( "New Panel ID: " + myPanelId );

      SpecklePlugIn.Store.RemoveAllClients();
      //SpecklePlugIn.Store.SpeckleIsReady = false;

      InitializeComponent();
      // Start the browser after initialize global component
      InitializeChromium();

      // Set the user control property on our plug-in
      SpecklePlugIn.Instance.PanelUserControl = this;

    }

    public void InitializeChromium( )
    {
      this.Controls.Add( SpecklePlugIn.Browser );
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


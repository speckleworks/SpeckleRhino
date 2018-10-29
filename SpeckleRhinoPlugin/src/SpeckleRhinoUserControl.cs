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
    /// <summary>
    /// This gets called every time a new file is opened. 
    /// Therefore, we must init things here. It's the best way.
    /// </summary>
    public SpeckleRhinoUserControl( )
    {
      if( SpecklePlugIn.Store!=null)
        SpecklePlugIn.Store.Dispose();

      InitializeComponent();

      SpecklePlugIn.InitializeCef();
      SpecklePlugIn.InitializeChromium();

      SpecklePlugIn.Store = new Interop( SpecklePlugIn.Browser );

      SpecklePlugIn.Browser.RegisterAsyncJsObject( "Interop", SpecklePlugIn.Store );

      this.Controls.Add( SpecklePlugIn.Browser );
      
      // Set the user control property on our plug-in
      SpecklePlugIn.Instance.PanelUserControl = this;
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


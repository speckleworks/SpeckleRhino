using System;
using System.IO;
using CefSharp;
using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
using System.Reflection;

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
    public SpecklePlugIn( )
    {
      Instance = this;
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

    protected override LoadReturnCode OnLoad( ref string errorMessage )
    {
      var panel_type = typeof( SpeckleRhinoUserControl );

      Panels.RegisterPanel( this, panel_type, "Speckle", SpeckleRhino.Properties.Resources.Speckle );
      InitializeCef();

      return base.OnLoad( ref errorMessage );
    }

    protected override void OnShutdown( )
    {
      base.OnShutdown();
      SpecklePlugIn.Instance.PanelUserControl?.Dispose();
      Cef.Shutdown();
    }

    void InitializeCef( )
    {
      Cef.EnableHighDPISupport();

      string assemblyLocation = Assembly.GetExecutingAssembly().Location;
      string assemblyPath = Path.GetDirectoryName( assemblyLocation );
      string pathSubprocess = Path.Combine( assemblyPath, "CefSharp.BrowserSubprocess.exe" );

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
      if ( !Cef.IsInitialized )
        Cef.Initialize( settings );
    }

  }
}
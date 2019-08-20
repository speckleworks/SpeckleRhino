using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Interop;
using Grasshopper.Kernel;

namespace SpeckleGrasshopper
{
  public class Loader : GH_AssemblyPriority
  {
    System.Timers.Timer loadTimer;

    public Loader( ) { }

    public override GH_LoadingInstruction PriorityLoad( )
    {
      loadTimer = new System.Timers.Timer( 500 );
      loadTimer.Start();
      loadTimer.Elapsed += AddSpeckleMenu;
      return GH_LoadingInstruction.Proceed;
    }

    private void AddSpeckleMenu( object sender, ElapsedEventArgs e )
    {
      if ( Grasshopper.Instances.DocumentEditor == null ) return;

      var speckleMenu = new ToolStripMenuItem( "Speckle" );
      speckleMenu.DropDown.Items.Add( "Speckle Account Manager", null, ( s, a ) =>
      {
        var signInWindow = new SpecklePopup.SignInWindow( false );
        var helper = new System.Windows.Interop.WindowInteropHelper( signInWindow );
        helper.Owner = Rhino.RhinoApp.MainWindowHandle();
        signInWindow.Show();
      } );

      speckleMenu.DropDown.Items.Add( new ToolStripSeparator() );

      speckleMenu.DropDown.Items.Add( "Speckle Home", null, ( s, a ) =>
      {
        Process.Start( @"https://speckle.works" );
      } );

      speckleMenu.DropDown.Items.Add( "Speckle Documentation", null, ( s, a ) =>
      {
        Process.Start( @"https://speckle.works/docs/essentials/start" );
      } );

      speckleMenu.DropDown.Items.Add( "Speckle Forum", null, ( s, a ) =>
      {
        Process.Start( @"https://discourse.speckle.works" );
      } );

      try
      {
        var mainMenu = Grasshopper.Instances.DocumentEditor.MainMenuStrip;
        Grasshopper.Instances.DocumentEditor.Invoke( new Action( ( ) =>
        {
          mainMenu.Items.Insert( mainMenu.Items.Count - 2, speckleMenu );
        } ) );
        loadTimer.Stop();
      }
      catch ( Exception err )
      {
        Debug.WriteLine( err.Message );
      }
    }
  }
}

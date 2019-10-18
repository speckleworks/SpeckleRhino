using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CefSharp;
using Newtonsoft.Json;
using Rhino;
using SpeckleCore;
using SpeckleUiBase;

namespace SpeckleRhino.UIBindings
{
  internal partial class RhinoUiBindings : SpeckleUIBindings
  {
    // Ui store. 
    public List<dynamic> Clients;

    // For receiver in memory diffing.
    public List<SpeckleStream> LocalState;

    public bool SelectionExpired = true;

    public RhinoUiBindings( IWebBrowser myBrowser )
    {
      Browser = myBrowser;
      Clients = new List<dynamic>();
      LocalState = new List<SpeckleStream>();

      // Selection Events
      RhinoDoc.SelectObjects += ( sender, e ) => { if( !Browser.IsBrowserInitialized ) return; SelectionExpired = true; };
      RhinoDoc.DeselectObjects += ( sender, e ) => { if( !Browser.IsBrowserInitialized ) return; SelectionExpired = true; };
      RhinoDoc.DeselectAllObjects += ( sender, e ) => { if( !Browser.IsBrowserInitialized ) return; SelectionExpired = true; };
      RhinoApp.Idle += RhinoApp_Idle;

      RhinoDoc.BeginSaveDocument += RhinoDoc_BeginSaveDocument;
    }

    private void RhinoDoc_BeginSaveDocument( object sender, DocumentSaveEventArgs e )
    {
      SaveClients();
    }

    private void RhinoApp_Idle( object sender, EventArgs e )
    {
      if( !Browser.IsBrowserInitialized ) return;
      if( SelectionExpired )
      {
        SelectionExpired = false;
        var selectedObjectsCount = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects( false, false ).ToList().Count;
        NotifyUi( "update-selection-count", JsonConvert.SerializeObject( new
        {
          selectedObjectsCount
        } ) );
      }
    }

    public void GetOldFileClients()
    {
      //TODO: migrate old clients to new clients. somehow.
      string[ ] receiverKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames( "speckle-client-receivers" );
      string[ ] senderKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames( "speckle-client-senders" );
    }

    public override void ShowAccountsPopup()
    {
      Rhino.RhinoApp.InvokeOnUiThread( new Action( () =>
      {
        var signInWindow = new SpecklePopup.SignInWindow();
        var helper = new System.Windows.Interop.WindowInteropHelper( signInWindow );
        helper.Owner = Rhino.RhinoApp.MainWindowHandle();

        signInWindow.ShowDialog();
        DispatchStoreActionUi( "getAccounts" );
      } ) );
    }

    public override void RemoveClient( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var index = Clients.FindIndex( cl => cl.clientId == client.clientId );
      if( index < 0 ) return;

      Clients.RemoveAt( index );

      if( DCRS.ContainsKey( (string) client.clientId ) )
      {
        DCRS[ (string) client.clientId ].Enabled = false;
        DCRS[ (string) client.clientId ].Geometry = new List<Rhino.Geometry.GeometryBase>(); // will it be GC'ed?
        DCRS.Remove( (string) client.clientId ); // will it be GC'ed?
        RhinoDoc.ActiveDoc.Views.Redraw();
      }

      SaveClients();
    }

    // TODO: Add to baseui
    public void ClientUpdated( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      // TODO: Check if receiver or sender, and do the right thing YO
    }

    public void SaveClients()
    {
      var doc = RhinoDoc.ActiveDoc;
      doc.Strings.SetString( "speckle", JsonConvert.SerializeObject( Clients ) );
      doc.Strings.SetString( "speckle-localstate", JsonConvert.SerializeObject( LocalState ) );
    }

    public override void SelectClientObjects( string args )
    {
      RhinoDoc.ActiveDoc.Objects.UnselectAll();
      var client = JsonConvert.DeserializeObject<dynamic>( args );

      // TODO: figure out what kind of filter this is, and "select" it. somehow.

      RhinoDoc.ActiveDoc.Views.Redraw();
    }

    public override string GetFileClients()
    {
      var clientsString = RhinoDoc.ActiveDoc.Strings.GetValue( "speckle" );
      var localStateString = RhinoDoc.ActiveDoc.Strings.GetValue( "speckle-localstate" );
      try
      {
        Clients = JsonConvert.DeserializeObject<List<dynamic>>( clientsString );
      }
      catch( Exception e )
      {
        Clients = new List<dynamic>();
      }

      try
      {
        LocalState = JsonConvert.DeserializeObject<List<SpeckleStream>>( localStateString );
      }
      catch( Exception e )
      {
        LocalState = new List<SpeckleStream>();
      }

      return clientsString;
    }

    public override string GetDocumentId()
    {
      return RhinoDoc.ActiveDoc.RuntimeSerialNumber.ToString();
    }

    public override string GetFileName()
    {
      return RhinoDoc.ActiveDoc.Name;
    }

    public override string GetDocumentLocation()
    {
      return RhinoDoc.ActiveDoc.Path;
    }

    public override string GetApplicationHostName()
    {
      return "Rhino";
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var layerNames = RhinoDoc.ActiveDoc.Layers.Select( layer => layer.FullPath ).ToList();
      return new List<ISelectionFilter>
      {
        new ElementsSelectionFilter
        {
          Name = "Selection",
          Icon = "mouse",
          Selection = new List<string>()
        },
        new ListSelectionFilter
        {
          Name = "Layers",
          Icon = "layers",
          Values = layerNames
        }
      };
    }

    // TODO: Add to baseui
    public bool CanTogglePreview() => true;
    public bool CanSelectObjects() => false;
  }

  public class RhinoLayerSelectionFilter : ISelectionFilter
  {
    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string Icon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string Type => throw new NotImplementedException();

    public List<String> Selection = new List<string>();
  }
}

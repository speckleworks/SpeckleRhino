using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp.WinForms;
using CefSharp;
using Newtonsoft.Json;
using System.IO;

using SpeckleCore;
using SpeckleRhinoConverter;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using Rhino;
using System.Dynamic;
using Rhino.DocObjects;
using System.Windows.Forms;
using Newtonsoft.Json.Serialization;

namespace SpeckleRhino
{
  // CEF Bound object. 
  // If CEF will be removed, porting to url hacks will be necessary,
  // so let's keep the methods as simple as possible.

  public class Interop : IDisposable
  {
    public ChromiumWebBrowser Browser;

    private List<SpeckleAccount> UserAccounts;
    public List<ISpeckleRhinoClient> UserClients;

    public Dictionary<string, SpeckleObject> SpeckleObjectCache;

    public bool SpeckleIsReady = false;

    public bool SelectionInfoNeedsToBeSentYeMighty = false; // should be false

    public Interop( ChromiumWebBrowser _originalBrowser )
    {
      // Makes sure we always get some camelCaseLove
      //JsonConvert.DefaultSettings = ( ) => new JsonSerializerSettings()
      //{
      //  ContractResolver = new CamelCasePropertyNamesContractResolver()
      //};

      Browser = _originalBrowser;

      UserAccounts = new List<SpeckleAccount>();

      UserClients = new List<ISpeckleRhinoClient>();

      SpeckleObjectCache = new Dictionary<string, SpeckleObject>();

      ReadUserAccounts();

      RhinoDoc.NewDocument += RhinoDoc_NewDocument;

      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

      RhinoDoc.BeginSaveDocument += RhinoDoc_BeginSaveDocument;

      RhinoDoc.SelectObjects += RhinoDoc_SelectObjects;

      RhinoDoc.DeselectObjects += RhinoDoc_DeselectObjects;

      RhinoDoc.DeselectAllObjects += RhinoDoc_DeselectAllObjects;

      RhinoApp.Idle += RhinoApp_Idle;
    }

    private void RhinoApp_Idle( object sender, EventArgs e )
    {
      //System.Diagnostics.Debug.WriteLine( "I am idle...  " + SelectionInfoNeedsToBeSentYeMighty );
      if ( SelectionInfoNeedsToBeSentYeMighty )
      {
        NotifySpeckleFrame( "object-selection", "", this.getLayersAndObjectsInfo() );
        SelectionInfoNeedsToBeSentYeMighty = false;
      }
    }

    public void SetBrowser( ChromiumWebBrowser _Browser )
    {
      Browser = _Browser;
    }

    public void Dispose( )
    {

      this.RemoveAllClients();

      RhinoDoc.NewDocument -= RhinoDoc_NewDocument;

      RhinoDoc.EndOpenDocument -= RhinoDoc_EndOpenDocument;

      RhinoDoc.BeginSaveDocument -= RhinoDoc_BeginSaveDocument;

      RhinoDoc.SelectObjects -= RhinoDoc_SelectObjects;

      RhinoDoc.DeselectObjects -= RhinoDoc_DeselectObjects;

      RhinoDoc.DeselectAllObjects -= RhinoDoc_DeselectAllObjects;

      RhinoApp.Idle -= RhinoApp_Idle;
    }

    #region Global Events

    private void RhinoDoc_NewDocument( object sender, DocumentEventArgs e )
    {
      Debug.WriteLine( "New document event" );
      NotifySpeckleFrame( "purge-clients", "", "" );
      RemoveAllClients();
    }

    private void RhinoDoc_DeselectAllObjects( object sender, RhinoDeselectAllObjectsEventArgs e )
    {
      Debug.WriteLine( "Deselect all event" );
      SelectionInfoNeedsToBeSentYeMighty = true;
      return;
    }

    private void RhinoDoc_DeselectObjects( object sender, RhinoObjectSelectionEventArgs e )
    {
      Debug.WriteLine( "Deselect event" );
      SelectionInfoNeedsToBeSentYeMighty = true;
      return;
    }

    private void RhinoDoc_SelectObjects( object sender, RhinoObjectSelectionEventArgs e )
    {
      Debug.WriteLine( "Select objs event" );
      SelectionInfoNeedsToBeSentYeMighty = true;
      return;
    }

    private void RhinoDoc_EndOpenDocument( object sender, DocumentOpenEventArgs e )
    {
      Debug.WriteLine( "END OPEN DOC" );
      // this seems to cover the copy paste issues
      if ( e.Merge ) return;
      // purge clients from ui
      NotifySpeckleFrame( "client-purge", "", "" );
      // purge clients from here
      RemoveAllClients();
      // read clients from document strings
      InstantiateFileClients();
    }

    private void RhinoDoc_BeginSaveDocument( object sender, DocumentSaveEventArgs e )
    {
      Debug.WriteLine( "BEGIN SAVE DOC" );
      SaveFileClients();
    }


    #endregion

    #region General Utils

    public void ShowDev( )
    {
      Browser.ShowDevTools();
    }

    public string GetDocumentName( )
    {
      return Rhino.RhinoDoc.ActiveDoc.Name;
    }

    public string GetDocumentGuid( )
    {
      return Rhino.RhinoDoc.ActiveDoc.DocumentId.ToString();
    }
    #endregion

    #region Serialisation & Init. 

    /// <summary>
    /// Do not call this from the constructor as you'll get confilcts with 
    /// browser load, etc.
    /// </summary>
    public void AppReady( )
    {
      SpeckleIsReady = true;
      InstantiateFileClients();
    }

    public void SaveFileClients( )
    {
      RhinoDoc myDoc = RhinoDoc.ActiveDoc;
      foreach ( ISpeckleRhinoClient rhinoClient in UserClients )
      {
        using ( var ms = new MemoryStream() )
        {
          var formatter = new BinaryFormatter();
          formatter.Serialize( ms, rhinoClient );
          string section = rhinoClient.GetRole() == ClientRole.Receiver ? "speckle-client-receivers" : "speckle-client-senders";
          var client = Convert.ToBase64String( ms.ToArray() );
          var clientId = rhinoClient.GetClientId();
          RhinoDoc.ActiveDoc.Strings.SetString( section, clientId, client );
        }
      }
    }

    public void InstantiateFileClients( )
    {
      if ( !SpeckleIsReady ) return;

      Debug.WriteLine( "Instantiate file clients." );

      string[ ] receiverKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames( "speckle-client-receivers" );

      foreach ( string rec in receiverKeys )
      {
        //if ( UserClients.Any( cl => cl.GetClientId() == rec ) )
        //  continue;

        byte[ ] serialisedClient = Convert.FromBase64String( RhinoDoc.ActiveDoc.Strings.GetValue( "speckle-client-receivers", rec ) );
        using ( var ms = new MemoryStream() )
        {
          ms.Write( serialisedClient, 0, serialisedClient.Length );
          ms.Seek( 0, SeekOrigin.Begin );
          RhinoReceiver client = ( RhinoReceiver ) new BinaryFormatter().Deserialize( ms );
          client.Context = this;
        }
      }

      string[ ] senderKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames( "speckle-client-senders" );

      foreach ( string sen in senderKeys )
      {
        byte[ ] serialisedClient = Convert.FromBase64String( RhinoDoc.ActiveDoc.Strings.GetValue( "speckle-client-senders", sen ) );

        using ( var ms = new MemoryStream() )
        {
          ms.Write( serialisedClient, 0, serialisedClient.Length );
          ms.Seek( 0, SeekOrigin.Begin );
          RhinoSender client = ( RhinoSender ) new BinaryFormatter().Deserialize( ms );
          client.CompleteDeserialisation( this );
        }
      }
    }
    #endregion

    #region Account Management

    public string GetUserAccounts( )
    {
      ReadUserAccounts();
      return JsonConvert.SerializeObject( UserAccounts, new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
      } );
    }

    private void ReadUserAccounts( )
    {
      UserAccounts = new List<SpeckleAccount>();
      string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData );
      strPath = strPath + @"\SpeckleSettings";

      if ( Directory.Exists( strPath ) && Directory.EnumerateFiles( strPath, "*.txt" ).Count() > 0 )
        foreach ( string file in Directory.EnumerateFiles( strPath, "*.txt" ) )
        {
          string content = File.ReadAllText( file );
          string[ ] pieces = content.TrimEnd( '\r', '\n' ).Split( ',' );
          UserAccounts.Add( new SpeckleAccount() { email = pieces[ 0 ], apiToken = pieces[ 1 ], serverName = pieces[ 2 ], restApi = pieces[ 3 ], rootUrl = pieces[ 4 ], fileName = file } );
        }
    }

    public void AddAccount( string payload )
    {
      var pieces = payload.Split( ',' );

      string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData );
      System.IO.Directory.CreateDirectory( strPath + @"\SpeckleSettings" );

      strPath = strPath + @"\SpeckleSettings\";

      string fileName = pieces[ 0 ] + "." + pieces[ 2 ] + ".txt";

      System.IO.StreamWriter file = new System.IO.StreamWriter( strPath + fileName );
      file.WriteLine( payload );
      file.Close();
    }

    public void RemoveAccount( string payload )
    {
      var x = UserAccounts.RemoveAll( account => { return account.fileName == payload; } );
      if ( File.Exists( payload ) )
        File.Delete( payload );
    }
    #endregion

    #region Client Management
    public bool AddReceiverClient( string _payload )
    {
      var myReceiver = new RhinoReceiver( _payload, this );
      return true;
    }

    public bool AddSenderClientFromSelection( string _payload )
    {
      var mySender = new RhinoSender( _payload, this );
      return true;
    }

    public bool RemoveClient( string _payload )
    {
      var myClient = UserClients.FirstOrDefault( client => client.GetClientId() == _payload );
      if ( myClient == null ) return false;

      RhinoDoc.ActiveDoc.Strings.Delete( myClient.GetRole() == ClientRole.Receiver ? "speckle-client-receivers" : "speckle-client-senders", myClient.GetClientId() );

      myClient.Dispose( true );

      return UserClients.Remove( myClient );
    }

    public bool RemoveAllClients( )
    {
      foreach ( var uc in UserClients )
      {
        uc.Dispose();
      }
      UserClients.RemoveAll( c => true );
      return true;
    }

    public string GetAllClients( )
    {
      foreach ( var client in UserClients )
      {
        if ( client is RhinoSender )
        {
          var rhSender = client as RhinoSender;
          NotifySpeckleFrame( "client-add", rhSender.StreamId, JsonConvert.SerializeObject( new { stream = rhSender.Client.Stream, client = rhSender.Client } ) );
          continue;
        }

        var rhReceiver = client as RhinoReceiver;
        NotifySpeckleFrame( "client-add", rhReceiver.StreamId, JsonConvert.SerializeObject( new { stream = rhReceiver.Client.Stream, client = rhReceiver.Client } ) );
      }

      return JsonConvert.SerializeObject( UserClients );
    }

    #endregion

    #region To UI (Generic)
    public void NotifySpeckleFrame( string EventType, string StreamId, string EventInfo )
    {
      if ( !SpeckleIsReady )
      {
        Debug.WriteLine( "Speckle wwas not ready, trying to send " + EventType );
        return;
      }

      var script = string.Format( "window.EventBus.$emit('{0}', '{1}', '{2}')", EventType, StreamId, EventInfo );
      try
      {
        Browser.GetMainFrame().EvaluateScriptAsync( script );
      }
      catch
      {
        Debug.WriteLine( "For some reason, this browser was not initialised." );
      }
    }
    #endregion

    #region From UI (..)

    public void bakeClient( string clientId )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null || myClient is RhinoReceiver )
        ( ( RhinoReceiver ) myClient ).Bake();

    }

    public void bakeLayer( string clientId, string layerGuid )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null || myClient is RhinoReceiver )
        ( ( RhinoReceiver ) myClient ).BakeLayer( layerGuid );
    }

    public void setClientPause( string clientId, bool status )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        myClient.TogglePaused( status );
    }

    public void setClientVisibility( string clientId, bool status )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        myClient.ToggleVisibility( status );
    }

    public void setClientHover( string clientId, bool status )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        myClient.ToggleVisibility( status );
    }


    public void setLayerVisibility( string clientId, string layerId, bool status )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        myClient.ToggleLayerVisibility( layerId, status );
    }

    public void setLayerHover( string clientId, string layerId, bool status )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        myClient.ToggleLayerHover( layerId, status );
    }

    public void setObjectHover( string clientId, string layerId, bool status )
    {

    }

    public void AddRemoveObjects( string clientId, string _guids, bool remove )
    {
      string[ ] guids = JsonConvert.DeserializeObject<string[ ]>( _guids );

      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        try
        {
          if ( !remove )
            ( ( RhinoSender ) myClient ).AddTrackedObjects( guids );
          else ( ( RhinoSender ) myClient ).RemoveTrackedObjects( guids );

        }
        catch { throw new Exception( "Force send client was not a sender. whoopsie poopsiee." ); }
    }

    public void refreshClient( string clientId )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        try
        {
          ( ( RhinoReceiver ) myClient ).UpdateGlobal();
        }
        catch { throw new Exception( "Refresh client was not a receiver. whoopsie poopsiee." ); }
    }

    public void forceSend( string clientId )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null )
        try
        {
          ( ( RhinoSender ) myClient ).ForceUpdate();
        }
        catch { throw new Exception( "Force send client was not a sender. whoopsie poopsiee." ); }
    }

    public void openUrl( string url )
    {
      System.Diagnostics.Process.Start( url );
    }

    public void setName( string clientId, string name )
    {
      var myClient = UserClients.FirstOrDefault( c => c.GetClientId() == clientId );
      if ( myClient != null && myClient is RhinoSender )
      {
        ( ( RhinoSender ) myClient ).Client.Stream.Name = name;
        ( ( RhinoSender ) myClient ).Client.BroadcastMessage( new { eventType = "update-name" } );
      }
    }

    #endregion

    #region Sender Helpers

    public string getLayersAndObjectsInfo( bool ignoreSelection = false )
    {
      List<RhinoObject> SelectedObjects;
      List<LayerSelection> layerInfoList = new List<LayerSelection>();

      if ( !ignoreSelection )
      {
        SelectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects( false, false ).ToList();
        if ( SelectedObjects.Count == 0 || SelectedObjects[ 0 ] == null )
          return JsonConvert.SerializeObject( layerInfoList );
      }
      else
      {
        SelectedObjects = RhinoDoc.ActiveDoc.Objects.ToList();
        if ( SelectedObjects.Count == 0 || SelectedObjects[ 0 ] == null )
          return JsonConvert.SerializeObject( layerInfoList );

        foreach ( Rhino.DocObjects.Layer ll in RhinoDoc.ActiveDoc.Layers )
        {
          layerInfoList.Add( new LayerSelection()
          {
            objectCount = 0,
            layerName = ll.FullPath,
            color = System.Drawing.ColorTranslator.ToHtml( ll.Color ),
            ObjectGuids = new List<string>(),
            ObjectTypes = new List<string>()
          } );
        }
      }

      SelectedObjects = SelectedObjects.OrderBy( o => o.Attributes.LayerIndex ).ToList();

      foreach ( var obj in SelectedObjects )
      {
        var layer = RhinoDoc.ActiveDoc.Layers[ obj.Attributes.LayerIndex ];
        var myLInfo = layerInfoList.FirstOrDefault( l => l.layerName == layer.FullPath );

        if ( myLInfo != null )
        {
          myLInfo.objectCount++;
          myLInfo.ObjectGuids.Add( obj.Id.ToString() );
          myLInfo.ObjectTypes.Add( obj.Geometry.GetType().ToString() );
        }
        else
        {
          var myNewLinfo = new LayerSelection()
          {
            objectCount = 1,
            layerName = layer.FullPath,
            color = System.Drawing.ColorTranslator.ToHtml( layer.Color ),
            ObjectGuids = new List<string>( new string[ ] { obj.Id.ToString() } ),
            ObjectTypes = new List<string>( new string[ ] { obj.Geometry.GetType().ToString() } )
          };
          layerInfoList.Add( myNewLinfo );
        }
      }

      return Convert.ToBase64String( System.Text.Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( layerInfoList ) ) );
    }
    #endregion
  }

  [Serializable]
  public class LayerSelection
  {
    public string layerName;
    public int objectCount;
    public string color;
    public List<string> ObjectGuids;
    public List<string> ObjectTypes;
  }
}

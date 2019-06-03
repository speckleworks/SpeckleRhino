extern alias SpeckleNewtonsoft;
using SNJ = SpeckleNewtonsoft.Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SpeckleRhino
{
  /// <summary>
  /// Rhino Sender Client
  /// </summary>
  [Serializable]
  public class RhinoSender : ISpeckleRhinoClient
  {
    public Interop Context { get; set; }

    public SpeckleApiClient Client { get; private set; }

    public List<SpeckleObject> Objects { get; set; }

    public SpeckleDisplayConduit Display;

    public string StreamId { get; set; }

    public bool Paused { get; set; } = false;

    public bool Visible { get; set; } = true;

    System.Timers.Timer DataSender, MetadataSender;

    public string StreamName;

    public bool IsSendingUpdate = false, Expired = false;

    public RhinoSender( string _payload, Interop _Context )
    {
      Context = _Context;

      dynamic InitPayload = SNJ.JsonConvert.DeserializeObject<ExpandoObject>( _payload );

      Client = new SpeckleApiClient( ( string ) InitPayload.account.restApi, true );

      StreamName = ( string ) InitPayload.streamName;

      SetClientEvents();
      SetRhinoEvents();
      SetTimers();

      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;

      Context.NotifySpeckleFrame( "set-gl-load", "", "true" );

      Client.IntializeSender( ( string ) InitPayload.account.token, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid() )
        .ContinueWith( res =>
            {
              StreamId = Client.Stream.StreamId;
              Client.Stream.Name = StreamName;

              Context.NotifySpeckleFrame( "set-gl-load", "", "false" );
              Context.NotifySpeckleFrame( "client-add", StreamId, SNJ.JsonConvert.SerializeObject( new { stream = Client.Stream, client = Client, authToken = Client.AuthToken }, Interop.camelCaseSettings ) );
              Context.UserClients.Add( this );

              InitTrackedObjects( InitPayload );
              DataSender.Start();
            } );

    }

    public void InitTrackedObjects( dynamic payload )
    {
      foreach ( string guid in payload.selection )
        RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, StreamId );
    }

    public void AddTrackedObjects( string[ ] guids )
    {
      foreach ( string guid in guids )
        RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, StreamId );

      DataSender.Start();
    }

    public void RemoveTrackedObjects( string[ ] guids )
    {
      foreach ( string guid in guids )
        RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, null );

      DataSender.Start();
    }

    public void SetRhinoEvents( )
    {
      RhinoDoc.ModifyObjectAttributes += RhinoDoc_ModifyObjectAttributes;
      RhinoDoc.DeleteRhinoObject += RhinoDoc_DeleteRhinoObject;
      RhinoDoc.AddRhinoObject += RhinoDoc_AddRhinoObject;
      RhinoDoc.UndeleteRhinoObject += RhinoDoc_UndeleteRhinoObject;
      RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;
    }

    public void UnsetRhinoEvents( )
    {
      RhinoDoc.ModifyObjectAttributes -= RhinoDoc_ModifyObjectAttributes;
      RhinoDoc.DeleteRhinoObject -= RhinoDoc_DeleteRhinoObject;
      RhinoDoc.AddRhinoObject -= RhinoDoc_AddRhinoObject;
      RhinoDoc.UndeleteRhinoObject -= RhinoDoc_UndeleteRhinoObject;
      RhinoDoc.LayerTableEvent -= RhinoDoc_LayerTableEvent;
    }

    private void RhinoDoc_LayerTableEvent( object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e )
    {
      if ( e.EventType == Rhino.DocObjects.Tables.LayerTableEventType.Added ) return;
      var x = e;
      DataSender.Start();
    }

    private void RhinoDoc_UndeleteRhinoObject( object sender, RhinoObjectEventArgs e )
    {
      //Debug.WriteLine("UNDELETE Event");
      if ( Paused )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }
      if ( e.TheObject.Attributes.GetUserString( "spk_" + StreamId ) == StreamId )
      {
        DataSender.Start();
      }
    }

    private void RhinoDoc_AddRhinoObject( object sender, RhinoObjectEventArgs e )
    {
      //Debug.WriteLine("ADD Event");
      if ( Paused )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }
      if ( e.TheObject.Attributes.GetUserString( "spk_" + StreamId ) == StreamId )
      {
        DataSender.Start();
      }
    }

    private void RhinoDoc_DeleteRhinoObject( object sender, RhinoObjectEventArgs e )
    {
      if ( Paused )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }
      if ( e.TheObject.Attributes.GetUserString( "spk_" + StreamId ) == StreamId )
      {
        DataSender.Start();
      }
    }

    private void RhinoDoc_ModifyObjectAttributes( object sender, RhinoModifyObjectAttributesEventArgs e )
    {
      //Debug.WriteLine("MODIFY Event");
      //Prevents https://github.com/speckleworks/SpeckleRhino/issues/51 from happening
      if ( Converter.getBase64( e.NewAttributes ) == Converter.getBase64( e.OldAttributes ) ) return;

      if ( Paused )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }
      if ( e.RhinoObject.Attributes.GetUserString( "spk_" + StreamId ) == StreamId )
      {
        DataSender.Start();
      }
    }

    public void SetClientEvents( )
    {
      Client.OnError += Client_OnError;
      Client.OnLogData += Client_OnLogData;
      Client.OnWsMessage += Client_OnWsMessage;
      Client.OnReady += Client_OnReady;
    }

    public void SetTimers( )
    {
      MetadataSender = new System.Timers.Timer( 500 ) { AutoReset = false, Enabled = false };
      MetadataSender.Elapsed += MetadataSender_Elapsed;

      DataSender = new System.Timers.Timer( 2000 ) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;
    }

    private void Client_OnReady( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-log", StreamId, SNJ.JsonConvert.SerializeObject( "Ready Event." ) );
    }

    private void DataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      Debug.WriteLine( "Boing! Boing!" );
      DataSender.Stop();
      SendStaggeredUpdate();
      Context.NotifySpeckleFrame( "client-log", StreamId, SNJ.JsonConvert.SerializeObject( "Update Sent." ) );
    }

    private void MetadataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      Debug.WriteLine( "Ping! Ping!" );
      MetadataSender.Stop();
      Context.NotifySpeckleFrame( "client-log", StreamId, SNJ.JsonConvert.SerializeObject( "Update Sent." ) );
    }

    private void Client_OnWsMessage( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-log", StreamId, SNJ.JsonConvert.SerializeObject( "WS message received and ignored." ) );
    }

    private void Client_OnLogData( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-log", StreamId, SNJ.JsonConvert.SerializeObject( e.EventData ) );
    }

    private void Client_OnError( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-error", StreamId, SNJ.JsonConvert.SerializeObject( e.EventData ) );
    }

    public void ForceUpdate( )
    {

      SendStaggeredUpdate( true );
    }

    public async void SendStaggeredUpdate( bool force = false )
    {

      if ( Paused && !force )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      } else
      {
        // create a clone
        var cloneResult = Client.StreamCloneAsync( StreamId ).Result;
        Client.Stream.Children.Add( cloneResult.Clone.StreamId );

        Client.BroadcastMessage( "stream", StreamId, new { eventType = "update-children" } );
      }

      if ( IsSendingUpdate )
      {
        Expired = true;
        return;
      }

      IsSendingUpdate = true;

      Context.NotifySpeckleFrame( "client-is-loading", StreamId, "" );

      var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + this.StreamId, "*", false ).OrderBy( obj => obj.Attributes.LayerIndex );

      Context.NotifySpeckleFrame( "client-progress-message", StreamId, "Converting " + objs.Count() + " objects..." );

      // layer list creation
      var pLayers = new List<SpeckleCore.Layer>();
      int lindex = -1, count = 0, orderIndex = 0;
      foreach ( RhinoObject obj in objs )
      {
        Rhino.DocObjects.Layer layer = RhinoDoc.ActiveDoc.Layers[ obj.Attributes.LayerIndex ];
        if ( lindex != obj.Attributes.LayerIndex )
        {
          var spkLayer = new SpeckleCore.Layer()
          {
            Name = layer.FullPath,
            Guid = layer.Id.ToString(),
            ObjectCount = 1,
            StartIndex = count,
            OrderIndex = orderIndex++,
            Properties = new LayerProperties() { Color = new SpeckleCore.SpeckleBaseColor() { A = 1, Hex = System.Drawing.ColorTranslator.ToHtml( layer.Color ) }, }
          };

          pLayers.Add( spkLayer );
          lindex = obj.Attributes.LayerIndex;
        }
        else
        {
          var spkl = pLayers.FirstOrDefault( pl => pl.Name == layer.FullPath );
          spkl.ObjectCount++;
        }

        count++;
      }

      // convert objects
      var convertedObjects = new List<SpeckleObject>();
      foreach ( RhinoObject obj in objs )
      {
        var myObj = Converter.Serialise( obj.Geometry ) as SpeckleObject;
        myObj.ApplicationId = obj.Id.ToString();
        convertedObjects.Add( myObj );
      }

      LocalContext.PruneExistingObjects( convertedObjects, Client.BaseUrl );

      List<SpeckleObject> persistedObjects = new List<SpeckleObject>();

      if ( convertedObjects.Count( obj => obj.Type == "Placeholder" ) != convertedObjects.Count )
      {
        // create the update payloads
        count = 0;
        var objectUpdatePayloads = new List<List<SpeckleObject>>();
        long totalBucketSize = 0;
        long currentBucketSize = 0;
        var currentBucketObjects = new List<SpeckleObject>();
        var allObjects = new List<SpeckleObject>();
        foreach ( SpeckleObject convertedObject in convertedObjects )
        {

          if ( count++ % 100 == 0 )
            Context.NotifySpeckleFrame( "client-progress-message", StreamId, "Converted " + count + " objects out of " + objs.Count() + "." );

          // size checking & bulk object creation payloads creation
          long size = Converter.getBytes( convertedObject ).Length;
          currentBucketSize += size;
          totalBucketSize += size;
          currentBucketObjects.Add( convertedObject );

          // Object is too big?
          if ( size > 2e6 )
          {
            Context.NotifySpeckleFrame( "client-error", StreamId, SNJ.JsonConvert.SerializeObject( "This stream contains a super big object. These will fail. Sorry for the bad error message - we're working on improving this." ) );
            currentBucketObjects.Remove( convertedObject );
          }

          if ( currentBucketSize > 5e5 ) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
          {
            Debug.WriteLine( "Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count );
            objectUpdatePayloads.Add( currentBucketObjects );
            currentBucketObjects = new List<SpeckleObject>();
            currentBucketSize = 0;
          }
        }

        // add in the last bucket
        if ( currentBucketObjects.Count > 0 )
          objectUpdatePayloads.Add( currentBucketObjects );

        Debug.WriteLine( "Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000 );

        // create bulk object creation tasks
        int k = 0;
        List<ResponseObject> responses = new List<ResponseObject>();
        foreach ( var payload in objectUpdatePayloads )
        {
          Context.NotifySpeckleFrame( "client-progress-message", StreamId, String.Format( "Sending payload {0} out of {1}", k++, objectUpdatePayloads.Count ) );
          try
          {
            var objResponse = await Client.ObjectCreateAsync( payload );
            responses.Add( objResponse );
            persistedObjects.AddRange( objResponse.Resources );

            int m = 0;
            foreach ( var oL in payload )
            {
              oL._id = objResponse.Resources[ m++ ]._id;
            }

            // push sent objects in the cache non-blocking
            Task.Run( ( ) =>
            {
              foreach ( var oL in payload )
              {
                if ( oL.Type != "Placeholder" )
                  LocalContext.AddSentObject( oL, Client.BaseUrl );
              }
            } );
          }
          catch ( Exception err )
          {
            Context.NotifySpeckleFrame( "client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject( err.Message ) );
            Context.NotifySpeckleFrame( "client-done-loading", StreamId, "" );
            IsSendingUpdate = false;
            return;
          }
        }
      }
      else
      {
        persistedObjects = convertedObjects;
      }

      Context.NotifySpeckleFrame( "client-progress-message", StreamId, "Updating stream..." );

      // finalise layer creation
      foreach ( var layer in pLayers )
        layer.Topology = "0-" + layer.ObjectCount + " ";

      // create placeholders for stream update payload
      List<SpeckleObject> placeholders = new List<SpeckleObject>();

      //foreach ( var myResponse in responses )
      foreach ( var obj in persistedObjects )
        placeholders.Add( new SpecklePlaceholder() { _id = obj._id } );

      // create stream update payload
      SpeckleStream streamUpdatePayload = new SpeckleStream();
      streamUpdatePayload.Layers = pLayers;
      streamUpdatePayload.Objects = placeholders;
      streamUpdatePayload.Name = Client.Stream.Name;

      // set some base properties (will be overwritten)
      var baseProps = new Dictionary<string, object>();
      baseProps[ "units" ] = RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      baseProps[ "tolerance" ] = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      baseProps[ "angleTolerance" ] = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      streamUpdatePayload.BaseProperties = baseProps;

      // update the stream
      ResponseBase response = null;
      try
      {
        response = await Client.StreamUpdateAsync( Client.Stream.StreamId, streamUpdatePayload );
      }
      catch ( Exception err )
      {
        Context.NotifySpeckleFrame( "client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject( err.Message ) );
        IsSendingUpdate = false;
        return;
      }

      // emit  events, etc.
      Client.Stream.Layers = streamUpdatePayload.Layers.ToList();
      Client.Stream.Objects = placeholders;

      Context.NotifySpeckleFrame( "client-metadata-update", StreamId, Client.Stream.ToJson() );
      Context.NotifySpeckleFrame( "client-done-loading", StreamId, "" );

      Client.BroadcastMessage(  "stream", StreamId, new { eventType = "update-global" } );

      IsSendingUpdate = false;
      if ( Expired )
      {
        DataSender.Start();
      }
      Expired = false;
    }

    public SpeckleCore.ClientRole GetRole( )
    {
      return ClientRole.Sender;
    }

    public string GetClientId( )
    {
      return Client.ClientId;
    }

    public void TogglePaused( bool status )
    {
      Paused = status;
    }

    public void ToggleVisibility( bool status )
    {
      this.Visible = status;
    }

    public void ToggleLayerHover( string layerId, bool status )
    {
      Debug.WriteLine( "OHAI: " + layerId + " " + status );
      Display.Enabled = true;
      Display.Geometry = new List<GeometryBase>();
      if ( !status )
      {
        Display.HoverRange = new Interval( 0, 0 );
        RhinoDoc.ActiveDoc.Views.Redraw();
        return;
      }

      int myLIndex = RhinoDoc.ActiveDoc.Layers.Find( new Guid( layerId ), true );

      var objs1 = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + this.StreamId, "*", false );
      var cop = objs1;
      var objs = objs1.OrderBy( obj => obj.Attributes.LayerIndex ).ToList();
      var count = objs.Count;
      foreach ( var obj in objs )
      {
        if ( obj.Attributes.LayerIndex == myLIndex )
          Display.Geometry.Add( obj.Geometry );
      }

      Display.HoverRange = new Interval( 0, Display.Geometry.Count );
      RhinoDoc.ActiveDoc.Views.Redraw();

    }

    public void ToggleLayerVisibility( string layerId, bool status )
    {
      throw new NotImplementedException();
    }

    public void Dispose( bool delete = false )
    {
      if ( delete )
      {
        var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + StreamId, "*", false );
        foreach ( var o in objs )
          o.Attributes.SetUserString( "spk_" + StreamId, null );
      }

      DataSender.Dispose();
      MetadataSender.Dispose();
      UnsetRhinoEvents();
      Client.Dispose( delete );
    }

    public void Dispose( )
    {
      DataSender.Dispose();
      MetadataSender.Dispose();
      UnsetRhinoEvents();
      Client.Dispose();
    }

    public void CompleteDeserialisation( Interop _Context )
    {
      Context = _Context;

      Context.NotifySpeckleFrame( "client-add", StreamId, SNJ.JsonConvert.SerializeObject( new { stream = Client.Stream, client = Client, authToken = Client.AuthToken }, Interop.camelCaseSettings ) );
      Context.UserClients.Add( this );
    }

    protected RhinoSender( SerializationInfo info, StreamingContext context )
    {
      byte[ ] serialisedClient = Convert.FromBase64String( ( string ) info.GetString( "client" ) );

      using ( var ms = new MemoryStream() )
      {
        ms.Write( serialisedClient, 0, serialisedClient.Length );
        ms.Seek( 0, SeekOrigin.Begin );
        Client = ( SpeckleApiClient ) new BinaryFormatter().Deserialize( ms );
        StreamId = Client.StreamId;
      }

      SetClientEvents();
      SetRhinoEvents();
      SetTimers();

      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;
    }

    public void GetObjectData( SerializationInfo info, StreamingContext context )
    {
      using ( var ms = new MemoryStream() )
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize( ms, Client );
        info.AddValue( "client", Convert.ToBase64String( ms.ToArray() ) );
        info.AddValue( "paused", Paused );
        info.AddValue( "visible", Visible );
      }
    }
  }
}

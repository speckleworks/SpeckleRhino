using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleRhinoConverter;
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
  public enum SenderType
  {
    ByLayers,
    BySelection
  };
  /// <summary>
  /// TODO
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

    public SenderType Type;

    System.Timers.Timer DataSender, MetadataSender;

    public List<string> TrackedObjects = new List<string>();

    public List<string> TrackedLayers = new List<string>();

    public string StreamName;

    PayloadStreamUpdate QueuedUpdate;

    public bool IsSendingUpdate = false, Expired = false;

    public RhinoSender( string _payload, Interop _Context, SenderType _Type )
    {
      Context = _Context;
      Type = _Type;

      dynamic InitPayload = JsonConvert.DeserializeObject<ExpandoObject>( _payload );

      Client = new SpeckleApiClient( ( string ) InitPayload.account.restApi, new RhinoConverter(), true );

      StreamName = ( string ) InitPayload.streamName;

      SetClientEvents();
      SetRhinoEvents();
      SetTimers();

      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;

      Context.NotifySpeckleFrame( "set-gl-load", "", "true" );

      Client.IntializeSender( ( string ) InitPayload.account.apiToken, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid() )
        .ContinueWith( res =>
            {
              StreamId = Client.Stream.StreamId;
              Client.Stream.Name = StreamName;

              Context.NotifySpeckleFrame( "set-gl-load", "", "false" );
              Context.NotifySpeckleFrame( "client-add", StreamId, JsonConvert.SerializeObject( new { stream = Client.Stream, client = Client } ) );
              Context.UserClients.Add( this );

              InitTrackedObjects( InitPayload );
              DataSender.Start();
              //SendUpdate( CreateUpdatePayload() );
            } );

    }

    public void InitTrackedObjects( dynamic payload )
    {
      switch ( Type )
      {
        case SenderType.BySelection:
          foreach ( string guid in payload.selection )
            RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, StreamId );
          //TrackedObjects.Add(guid);

          break;

        case SenderType.ByLayers:
          Debug.WriteLine( "TODO SenderType.byLayers" );
          break;
      }
    }

    public void AddTrackedObjects( string[ ] guids )
    {
      foreach ( string guid in guids )
        RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, StreamId );

      DataSender.Start();
      //SendUpdate( CreateUpdatePayload() );
    }

    public void RemoveTrackedObjects( string[ ] guids )
    {
      foreach ( string guid in guids )
        RhinoDoc.ActiveDoc.Objects.Find( new Guid( guid ) ).Attributes.SetUserString( "spk_" + StreamId, null );

      DataSender.Start();
      //SendUpdate( CreateUpdatePayload() );
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
      //throw new NotImplementedException();
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
      //Debug.WriteLine("DELETE Event");
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
      Context.NotifySpeckleFrame( "client-log", StreamId, JsonConvert.SerializeObject( "Ready Event." ) );
    }

    private void DataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      Debug.WriteLine( "Boing! Boing!" );
      DataSender.Stop();
      //SendUpdate( CreateUpdatePayload() );
      SendStaggeredUpdate();
      Context.NotifySpeckleFrame( "client-log", StreamId, JsonConvert.SerializeObject( "Update Sent." ) );
    }

    private void MetadataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      Debug.WriteLine( "Ping! Ping!" );
      MetadataSender.Stop();
      Context.NotifySpeckleFrame( "client-log", StreamId, JsonConvert.SerializeObject( "Update Sent." ) );
    }

    private void Client_OnWsMessage( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-log", StreamId, JsonConvert.SerializeObject( "WS message received and ignored." ) );
    }

    private void Client_OnLogData( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-log", StreamId, JsonConvert.SerializeObject( e.EventData ) );
    }

    private void Client_OnError( object source, SpeckleEventArgs e )
    {
      Context.NotifySpeckleFrame( "client-error", StreamId, JsonConvert.SerializeObject( e.EventData ) );
    }

    public void ForceUpdate( )
    {
      SendStaggeredUpdate( true );
      //SendUpdate( CreateUpdatePayload(), true );
    }

    public void SendUpdate( PayloadStreamUpdate payload, bool force = false )
    {
      if ( Paused && !force )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }

      if ( IsSendingUpdate )
      {
        //Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        Expired = true;
        return;
      }

      IsSendingUpdate = true;

      Debug.WriteLine( "Sending update " + DateTime.Now );
      Context.NotifySpeckleFrame( "client-is-loading", StreamId, "" );

      var response = Client.StreamUpdate( payload, Client.Stream.StreamId );

      Client.BroadcastMessage( new { eventType = "update-global" } );

      // commit to cache
      int k = 0;
      foreach ( var obj in payload.Objects )
      {
        obj.DatabaseId = response.Objects[ k++ ];
        Context.ObjectCache[ obj.Hash ] = obj;
      }

      Client.Stream.Layers = payload.Layers.ToList();
      Client.Stream.Objects = payload.Objects.Select( o => o.ApplicationId ).ToList();

      Context.NotifySpeckleFrame( "client-metadata-update", StreamId, Client.Stream.ToJson() );
      Context.NotifySpeckleFrame( "client-done-loading", StreamId, "" );

      IsSendingUpdate = false;
      if ( Expired )
      {
        DataSender.Start();
      }
      Expired = false;
    }

    public PayloadStreamUpdate CreateUpdatePayload( )
    {
      PayloadStreamUpdate payload = new PayloadStreamUpdate() { Name = StreamName };

      var pLayers = new List<SpeckleLayer>();
      var pObjects = new List<SpeckleObject>();

      using ( var converter = new RhinoConverter() )
      {
        var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + this.StreamId, "*", false ).OrderBy( obj => obj.Attributes.LayerIndex );

        // Assemble layers and objects
        int lindex = -1, count = 0, orderIndex = 0;
        foreach ( var obj in objs )
        {
          Layer layer = RhinoDoc.ActiveDoc.Layers[ obj.Attributes.LayerIndex ];
          if ( lindex != obj.Attributes.LayerIndex )
          {
            var spkLayer = new SpeckleLayer()
            {
              Name = layer.FullPath,
              Guid = layer.Id.ToString(),
              ObjectCount = 1,
              StartIndex = count,
              OrderIndex = orderIndex++,
              Properties = new SpeckleLayerProperties()
              {
                Color = new SpeckleCore.Color() { A = 1, Hex = System.Drawing.ColorTranslator.ToHtml( layer.Color ) },
              }
            };
            pLayers.Add( spkLayer );
            lindex = obj.Attributes.LayerIndex;
          }
          else
          {
            var spkl = pLayers.FirstOrDefault( pl => pl.Name == layer.FullPath );
            spkl.ObjectCount++;
          }

          pObjects.Add( converter.ToSpeckle( obj.Geometry ) );
          pObjects[ pObjects.Count - 1 ].ApplicationId = obj.Id.ToString();

          count++;
        }
      }

      foreach ( var layer in pLayers )
      {
        layer.Topology = "0-" + layer.ObjectCount + " ";
      }

      payload.Layers = pLayers;
      // Go through cache
      payload.Objects = pObjects.Select( obj =>
       {
         if ( Context.ObjectCache.ContainsKey( obj.Hash ) )
           return new SpeckleObjectPlaceholder() { Hash = obj.Hash, DatabaseId = Context.ObjectCache[ obj.Hash ].DatabaseId, ApplicationId = obj.ApplicationId };
         return obj;
       } );

      // Add some base properties
      var baseProps = new Dictionary<string, object>();
      baseProps[ "units" ] = RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      baseProps[ "tolerance" ] = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      baseProps[ "angleTolerance" ] = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      payload.BaseProperties = baseProps;

      return payload;
    }

    public async void SendStaggeredUpdate( bool force = false )
    {

      if ( Paused && !force )
      {
        Context.NotifySpeckleFrame( "client-expired", StreamId, "" );
        return;
      }

      if ( IsSendingUpdate )
      {
        Expired = true;
        return;
      }

      IsSendingUpdate = true;

      var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + this.StreamId, "*", false ).OrderBy( obj => obj.Attributes.LayerIndex );

      List<SpeckleLayer> pLayers = new List<SpeckleLayer>();
      List<SpeckleObject> convertedObjects = new List<SpeckleObject>();
      List<PayloadMultipleObjects> objectUpdatePayloads = new List<PayloadMultipleObjects>();

      RhinoConverter converter = new RhinoConverter();

      long totalBucketSize = 0;
      long currentBucketSize = 0;
      List<SpeckleObject> currentBucketObjects = new List<SpeckleObject>();
      List<SpeckleObject> allObjects = new List<SpeckleObject>();

      int lindex = -1, count = 0, orderIndex = 0;
      foreach ( RhinoObject obj in objs )
      {

        // layer list creation
        Layer layer = RhinoDoc.ActiveDoc.Layers[ obj.Attributes.LayerIndex ];
        if ( lindex != obj.Attributes.LayerIndex )
        {
          var spkLayer = new SpeckleLayer()
          {
            Name = layer.FullPath,
            Guid = layer.Id.ToString(),
            ObjectCount = 1,
            StartIndex = count,
            OrderIndex = orderIndex++,
            Properties = new SpeckleLayerProperties() { Color = new SpeckleCore.Color() { A = 1, Hex = System.Drawing.ColorTranslator.ToHtml( layer.Color ) }, }
          };

          pLayers.Add( spkLayer );
          lindex = obj.Attributes.LayerIndex;
        }
        else
        {
          var spkl = pLayers.FirstOrDefault( pl => pl.Name == layer.FullPath );
          spkl.ObjectCount++;
        }

        // object conversion
        var convertedObject = converter.ToSpeckle( obj.Geometry );
        convertedObject.ApplicationId = obj.Id.ToString();
        allObjects.Add( convertedObject );

        // todo: check cache!!! and see what the response from the server is when sending placeholders
        // in the ObjectCreateBulkAsyncRoute

        if ( Context.ObjectCache.ContainsKey( convertedObject.Hash ) )
        {
          convertedObject = new SpeckleObjectPlaceholder() { Hash = convertedObject.Hash, DatabaseId = Context.ObjectCache[ convertedObject.Hash ].DatabaseId, ApplicationId = Context.ObjectCache[ convertedObject.Hash ].ApplicationId };
        }

        // size checking & bulk object creation payloads creation
        long size = RhinoConverter.getBytes( convertedObject ).Length;
        currentBucketSize += size;
        totalBucketSize += size;
        currentBucketObjects.Add( convertedObject );

        if ( currentBucketSize > 1e6 ) // restrict max to ~1mb
        {
          Debug.WriteLine( "Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count );
          objectUpdatePayloads.Add( new PayloadMultipleObjects() { Objects = currentBucketObjects.ToArray() } );
          currentBucketObjects = new List<SpeckleObject>();
          currentBucketSize = 0;
        }
      }

      // last bucket
      if ( currentBucketObjects.Count > 0 )
        objectUpdatePayloads.Add( new PayloadMultipleObjects() { Objects = currentBucketObjects.ToArray() } );

      Debug.WriteLine( "Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000 );

      // create bulk object creation tasks
      Task<ResponsePostObjects>[ ] updateTasks = new Task<ResponsePostObjects>[ objectUpdatePayloads.Count ]; int k = 0;
      foreach ( var payload in objectUpdatePayloads )
      {
        updateTasks[ k++ ] = Client.ObjectCreateBulkAsync( payload );
      }

      if ( objectUpdatePayloads.Count > 100 )
      {
        // means we're around fooking bazillion mb of an upload. FAIL FAIL FAIL
        Context.NotifySpeckleFrame( "client-error", StreamId, "This is a humongous update, in the range of ~100mb. For now, create more streams instead of just one massive one! Updates will be faster and snappier, and you can combine them back together at the other end easier." );
        IsSendingUpdate = false;
        return;
      }

      for ( int i = 0; i < updateTasks.Length; i ++ )
      {
        Debug.WriteLine( "Sending update payload # " + i + " out of " + updateTasks.Length);
        Context.NotifySpeckleFrame( "client-progress-message", StreamId, String.Format( "Sending payload {0} out of {1}", i, updateTasks.Length ));
        await updateTasks[ i ];
      }



      // finalise layer creation
      foreach ( var layer in pLayers )
        layer.Topology = "0-" + layer.ObjectCount + " ";

      // create placeholders for stream update payload
      List<SpeckleObjectPlaceholder> placeholders = new List<SpeckleObjectPlaceholder>();
      int m = 0;
      foreach ( Task<ResponsePostObjects> myTask in updateTasks )
        foreach ( string dbId in myTask.Result.Objects ) placeholders.Add( new SpeckleObjectPlaceholder() { DatabaseId = dbId, ApplicationId = allObjects[ m++ ].ApplicationId } );

      // create stream update payload
      PayloadStreamUpdate streamUpdatePayload = new PayloadStreamUpdate();
      streamUpdatePayload.Layers = pLayers;
      streamUpdatePayload.Objects = placeholders;

      // set some base properties (will be overwritten)
      var baseProps = new Dictionary<string, object>();
      baseProps[ "units" ] = RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      baseProps[ "tolerance" ] = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      baseProps[ "angleTolerance" ] = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      streamUpdatePayload.BaseProperties = baseProps;

      // push it to the server yo!
      var response = await Client.StreamUpdateAsync( streamUpdatePayload, Client.Stream.StreamId );

      // put the objects in the cache 
      int l = 0;

      foreach ( var obj in streamUpdatePayload.Objects )
      {
        obj.DatabaseId = response.Objects[ l ];
        Context.ObjectCache[ allObjects[ l ].Hash ] = placeholders[ l ];
        l++;
      }

      // emit  events, etc.
      Client.Stream.Layers = streamUpdatePayload.Layers.ToList();
      Client.Stream.Objects = streamUpdatePayload.Objects.Select( o => o.ApplicationId ).ToList();

      Context.NotifySpeckleFrame( "client-metadata-update", StreamId, Client.Stream.ToJson() );
      Context.NotifySpeckleFrame( "client-done-loading", StreamId, "" );

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
      Display.Geometry = new List<GeometryBase>();
      if ( !status )
      {
        Display.HoverRange = new Interval( 0, 0 );
        RhinoDoc.ActiveDoc.Views.Redraw();
        return;
      }

      int myLIndex = RhinoDoc.ActiveDoc.Layers.Find( new Guid( layerId ), true );

      var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString( "spk_" + this.StreamId, "*", false ).OrderBy( obj => obj.Attributes.LayerIndex );

      foreach ( var obj in objs )
      {
        if ( obj.Attributes.LayerIndex == myLIndex ) Display.Geometry.Add( obj.Geometry );
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

      Context.NotifySpeckleFrame( "client-add", StreamId, JsonConvert.SerializeObject( new { stream = Client.Stream, client = Client } ) );
      Context.UserClients.Add( this );
    }

    protected RhinoSender( SerializationInfo info, StreamingContext context )
    {
      JsonConvert.DefaultSettings = ( ) => new JsonSerializerSettings()
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
      };


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

      Type = ( SenderType ) info.GetInt16( "type" ); // check for exceptions
      TrackedObjects = info.GetValue( "trackedobjects", typeof( List<string> ) ) as List<string>;
      TrackedLayers = info.GetValue( "trackedlayers", typeof( List<string> ) ) as List<string>;
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

        info.AddValue( "type", Type );
        info.AddValue( "trackedobjects", TrackedObjects );
        info.AddValue( "trackedlayers", TrackedLayers );
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleUiBase;

namespace SpeckleRhino.UIBindings
{

  public class ReceiverWrapper
  {
    public SpeckleDisplayConduit Display = new SpeckleDisplayConduit();
  }

  internal partial class RhinoUiBindings : SpeckleUIBindings
  {

    public Dictionary<string, SpeckleDisplayConduit> DCRS = new Dictionary<string, SpeckleDisplayConduit>();

    // TODO: add overrdiable method to base ui
    public void TogglePreview( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var DC = DCRS[ (string) client.clientId ];
      DC.Enabled = (bool) client.preview;
      RhinoDoc.ActiveDoc.Views.Redraw();
    }

    public override void AddReceiver( string args )
    {
      var receiver = JsonConvert.DeserializeObject<dynamic>( args );
      var index = Clients.FindIndex( cl => cl.clientId == receiver.clientId );
      if( index == -1 )
        Clients.Add( receiver );

      if( !DCRS.ContainsKey( (string) receiver.clientId ) )
        DCRS[ (string) receiver.clientId ] = new SpeckleDisplayConduit();

      SaveClients();
      Task.Run( new Action( () => GetReceiverStream( args ) ) );
    }

    public override void BakeReceiver( string args )
    {
      var receiver = JsonConvert.DeserializeObject<dynamic>( args );
      if( !DCRS.ContainsKey( (string) receiver.clientId ) )
      {
        NotifyUi( "update-client", JsonConvert.SerializeObject( new
        {
          errors = "Failed to find receiver's geometry in local state."
        } ) );
        return;
      }

      var DC = DCRS[ (string) receiver.clientId ];

      var Stream = LocalState.FirstOrDefault( s => s.StreamId == (string) receiver.streamId );
      string parent = String.Format( "{0} | {1}", Stream.Name, Stream.StreamId );

      // Parent layer creation
      var parentId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath( parent, -1 );
      if( parentId == -1 )
      {
        var parentLayer = new Rhino.DocObjects.Layer()
        {
          Color = System.Drawing.Color.Black,
          Name = parent
        };
        parentId = Rhino.RhinoDoc.ActiveDoc.Layers.Add( parentLayer );
      }
      else
      {
        foreach( var layer in Rhino.RhinoDoc.ActiveDoc.Layers[ parentId ].GetChildren() )
        {
          Rhino.RhinoDoc.ActiveDoc.Layers.Purge( layer.Index, false );
        }
      }

      foreach( var spkLayer in Stream.Layers )
      {
        var layerId = RhinoDoc.ActiveDoc.Layers.FindByFullPath( parent + "::" + spkLayer.Name, -1 );

        var index = -1;

        if( spkLayer.Name.Contains( "::" ) )
        {
          var spkLayerPath = spkLayer.Name.Split( new string[ ] { "::" }, StringSplitOptions.None );

          var parentLayerId = Guid.Empty;

          foreach( var layerPath in spkLayerPath )
          {

            if( parentLayerId == Guid.Empty )
              parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[ parentId ].Id;

            var layer = new Rhino.DocObjects.Layer()
            {
              Name = layerPath,
              ParentLayerId = parentLayerId,
              Color = GetColorFromLayer( spkLayer ),
              IsVisible = true
            };

            var parentLayerName = Rhino.RhinoDoc.ActiveDoc.Layers.First( l => l.Id == parentLayerId ).FullPath;

            var layerExist = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath( parentLayerName + "::" + layer.Name, -1 );


            if( layerExist == -1 )
            {
              index = Rhino.RhinoDoc.ActiveDoc.Layers.Add( layer );
              parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[ index ].Id;
            }
            else
            {
              parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[ layerExist ].Id;
            }

          }
        }
        else
        {

          var layer = new Rhino.DocObjects.Layer()
          {
            Name = spkLayer.Name,
            Id = Guid.Parse( spkLayer.Guid ),
            ParentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[ parentId ].Id,
            Color = GetColorFromLayer( spkLayer ),
            IsVisible = true
          };

          index = Rhino.RhinoDoc.ActiveDoc.Layers.Add( layer );
        }

        for( int i = (int) spkLayer.StartIndex; i < spkLayer.StartIndex + spkLayer.ObjectCount; i++ )
        {
          if( DC.Geometry.Count > i && DC.Geometry[ i ] != null && !DC.Geometry[ i ].IsDocumentControlled )
          {
            Rhino.RhinoDoc.ActiveDoc.Objects.Add( DC.Geometry[ i ], new ObjectAttributes() { LayerIndex = index } );
          }
        }
      }
    }

    public void GetReceiverStream( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( (string) client.account.RestApi ) { AuthToken = (string) client.account.Token };
      apiClient.ClientType = "Rhino";

      string errors = "";

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = true,
        loadingBlurb = "Getting stream from server..."
      } ) );

      var previousStream = LocalState.FirstOrDefault( s => s.StreamId == (string) client.streamId );
      if( previousStream == null )
      {
        previousStream = new SpeckleStream { StreamId = (string) client.streamId, Objects = new List<SpeckleObject>() };
        LocalState.Add( previousStream );
      }

      var stream = apiClient.StreamGetAsync( (string) client.streamId, "" ).Result.Resource;

      LocalContext.GetCachedObjects( stream.Objects, (string) client.account.RestApi );
      var payload = stream.Objects.Where( o => o.Type == "Placeholder" ).Select( obj => obj._id ).ToArray();

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = true,
        loadingBlurb = "Getting objects " + payload.Length
      } ) );

      var objects = apiClient.ObjectGetBulkAsync( payload, "" ).Result.Resources;

      foreach( var obj in objects )
      {
        stream.Objects[ stream.Objects.FindIndex( o => o._id == obj._id ) ] = obj;
      }

      var DC = DCRS[ (string) client.clientId ];
      DC.Geometry = new List<Rhino.Geometry.GeometryBase>();
      int i = 0;
      foreach( var obj in stream.Objects )
      {
        try
        {
          DC.Geometry.Add( (GeometryBase) Converter.Deserialise( obj ) );
        }
        catch( Exception e )
        {
          errors += "Failed to convert " + obj.Type + " at index " + i;
        }
        i++;
      }

      errors += "";

      DC.Enabled = true;

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = false,
        isLoadingIndeterminate = true,
        loadingBlurb = string.Format( "Done." ),
        errors,
        errorMsg = errors != "" ? "There are some errors ᕦ(ò_óˇ)ᕤ" : ""
      } ) );

      LocalState.Remove( previousStream );
      LocalState.Add( stream );

      RhinoDoc.ActiveDoc.Views.Redraw();
    }

    public System.Drawing.Color GetColorFromLayer( SpeckleCore.Layer layer )
    {
      System.Drawing.Color layerColor = System.Drawing.ColorTranslator.FromHtml( "#AEECFD" );
      try
      {
        if( layer != null && layer.Properties != null )
          layerColor = System.Drawing.ColorTranslator.FromHtml( layer.Properties.Color.Hex );
      }
      catch
      {
        System.Diagnostics.Debug.WriteLine( "Layer '{0}' had no assigned color", layer.Name );
      }
      return layerColor;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleUiBase;

namespace SpeckleRhino.UIBindings
{
  internal partial class RhinoUiBindings : SpeckleUIBindings
  {
    public override void AddReceiver( string args )
    {
      var receiver = JsonConvert.DeserializeObject<dynamic>( args );
      Clients.Add( receiver );
      SaveClients();
    }

    public override void BakeReceiver( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( (string) client.account.RestApi ) { AuthToken = (string) client.account.Token };
      apiClient.ClientType = "Rhino";

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

      var objects = apiClient.ObjectGetBulkAsync( payload, "" ).Result.Resources;
      foreach( var obj in objects )
      {
        stream.Objects[ stream.Objects.FindIndex( o => o._id == obj._id ) ] = obj;
      }

      throw new NotImplementedException();
    }
  }
}

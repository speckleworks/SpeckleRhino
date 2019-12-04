using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino;
using SpeckleUiBase;

namespace SpeckleRhino.UIBindings
{
  internal partial class RhinoUiBindings : SpeckleUIBindings
  {
    public override void AddSender( string args )
    {
      var sender = JsonConvert.DeserializeObject<dynamic>( args );
      var index = Clients.FindIndex( cl => cl.clientId == sender.clientId );
      if( index == -1 )
        Clients.Add( sender );

      SaveClients();
    }

    public override void UpdateSender( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var index = Clients.FindIndex( cl => (string) cl._id == (string) client._id );
      Clients[ index ] = client;
      SaveClients();
    }

    public override void AddSelectionToSender( string args )
    {
      var selectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects( false, false ).Select( obj => obj.Id.ToString() ).ToList();
      NotifyUi( "update-selection", JsonConvert.SerializeObject( new
      {
        selectedObjects
      } ) );
    }

    public override void AddObjectsToSender( string args )
    {
      // NOTE: not used anymore due to new selection filtering logic
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromSender( string args )
    {
      // NOTE: not used anymore due to new selection filtering logic
      throw new NotImplementedException();
    }

    public override void RemoveSelectionFromSender( string args )
    {
      // NOTE: not used anymore due to new selection filtering logic
      throw new NotImplementedException();
    }

    public override void PushSender( string args )
    {
      // TODO
      throw new NotImplementedException();
    }

  }
}

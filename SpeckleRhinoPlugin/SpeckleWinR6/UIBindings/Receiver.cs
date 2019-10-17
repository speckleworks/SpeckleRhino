using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
      throw new NotImplementedException();
    }
  }
}

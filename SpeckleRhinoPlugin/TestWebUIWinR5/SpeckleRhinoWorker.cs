using Newtonsoft.Json;
using SpeckleCore;
using SpeckleRhinoConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleRhino
{
    /// <summary>
    /// Starting to scaffold.
    /// </summary>
    public class RhinoReceiver
    {
        private Interop Parent { get; set; }
        public SpeckleApiClient Client { get; set; }
        public bool Paused { get; set; } = false;

        public string StreamId
        {
            get
            {
                return this.Client.Stream.StreamId;
            }
        }

        public RhinoReceiver(string _payload, Interop _parent)
        {
            Parent = _parent;
            dynamic payload = JsonConvert.DeserializeObject(_payload);

            Client = new SpeckleApiClient((string)payload.account.restApi, new RhinoConverter(), true);

            Client.OnReady += Client_OnReady;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;
        }

        public virtual void Client_OnWsMessage(object source, SpeckleEventArgs e)
        {
            Parent.NotifySpeckleFrame("test", StreamId, "helloWorldSerialisedBlobs");

            if (Paused)
            {
                Parent.NotifySpeckleFrame("client-expired", StreamId, "");
                return;
            }

            switch ((string)e.EventObject.args.eventType)
            {
                case "update-global":
                    //UpdateGlobal();
                    break;
                case "update-meta":
                    //UpdateMeta();
                    break;
                case "update-object":
                    break;
                default:
                    //CustomMessageHandler((string)e.EventObject.args.eventType, e);
                    break;
            }
        }

        public virtual void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            throw new NotImplementedException();
        }

        public virtual void Client_OnReady(object source, SpeckleEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

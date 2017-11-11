using Newtonsoft.Json;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleRhinoConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleRhino
{
    public interface ISpeckleRhinoClient : IDisposable
    {
        SpeckleCore.ClientRole GetRole();
        string GetClientId();
    }

    /// <summary>
    /// TODO
    /// </summary>
    public class RhinoSender : ISpeckleRhinoClient
    {
        public SpeckleCore.ClientRole GetRole()
        {
            return ClientRole.Sender;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public string GetClientId()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class that holds a rhino receiver client warpped around the
    /// SpeckleApiClient.
    /// </summary>
    public class RhinoReceiver : ISpeckleRhinoClient
    {
        private Interop Context { get; set; }

        public SpeckleApiClient Client { get; set; }

        public List<SpeckleObject> Objects { get; set; }

        public SpeckleDisplayConduit Display;

        public string StreamId { get; private set; }

        public bool Paused { get; set; } = false;

        public RhinoReceiver(string _payload, Interop _parent)
        {
            Context = _parent;
            dynamic payload = JsonConvert.DeserializeObject(_payload);

            StreamId = (string)payload.streamId;

            Client = new SpeckleApiClient((string)payload.account.restApi, new RhinoConverter(), true);

            Client.OnReady += Client_OnReady;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;

            Client.IntializeReceiver((string)payload.streamId, "test", "Rhino", "test", (string)payload.account.apiToken);

            Display = new SpeckleDisplayConduit();
            Display.Enabled = true;

            Objects = new List<SpeckleObject>();
        }


        public virtual void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject(e.EventData));
        }

        public virtual void Client_OnReady(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-add", StreamId, JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }));

            Context.UserClients.Add(this);

            UpdateGlobal();
        }

        public virtual void Client_OnWsMessage(object source, SpeckleEventArgs e)
        {
            if (Paused)
            {
                Context.NotifySpeckleFrame("client-expired", StreamId, "");
                return;
            }

            switch ((string)e.EventObject.args.eventType)
            {
                case "update-global":
                    UpdateGlobal();
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

        public void UpdateGlobal()
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Global update received."));

            var streamGetResponse = Client.StreamGet(StreamId);
            if (streamGetResponse.Success == false)
            {
                Context.NotifySpeckleFrame("client-error", StreamId, streamGetResponse.Message);
                Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Failed to retrieve global update."));
            }

            Client.Stream = streamGetResponse.Stream;

            // prepare payload
            PayloadObjectGetBulk payload = new PayloadObjectGetBulk();
            payload.Objects = Client.Stream.Objects.Where(o => !Context.ObjectCache.ContainsKey(o));

            // bug in speckle core, no sync method for this :(
            Client.ObjectGetBulkAsync("omit=displayValue", payload).ContinueWith(tres =>
            {
                if (tres.Result.Success == false)
                 Context.NotifySpeckleFrame("client-error", StreamId, streamGetResponse.Message);
                var copy = tres.Result;
                // add to cache
                foreach (var obj in tres.Result.Objects)
                    Context.ObjectCache[obj.DatabaseId] = obj;

                // populate real objects
                Objects.Clear();
                foreach (var objId in Client.Stream.Objects)
                    Objects.Add(Context.ObjectCache[objId]);

                DisplayContents();
            });

        }

        public void DisplayContents()
        {
            RhinoConverter rhinoConverter = new RhinoConverter();
            Display.Geometry = new List<GeometryBase>();

            foreach(SpeckleObject myObject in Objects)
            {
                switch(myObject.Type)
                {
                    case "Mesh":
                    case "Brep":
                    case "Curve":
                        Display.Geometry.Add((GeometryBase) rhinoConverter.ToNative(myObject));
                        break;
                }
            }
            
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void Dispose()
        {
            Client.Dispose();
            Display.Enabled = false;
        }

        public string GetClientId()
        {
            return Client.ClientId;
        }

        public ClientRole GetRole()
        {
            return ClientRole.Receiver;
        }
    }
}

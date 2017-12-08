using Newtonsoft.Json;
using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleRhinoConverter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
namespace SpeckleRhino
{
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

        System.Timers.Timer EventSender;


        public RhinoSender(string _payload, Interop _Context)
        {
            Context = _Context;
            dynamic payload = JsonConvert.DeserializeObject(_payload);

            Client = new SpeckleApiClient((string)payload.account.restApi, new RhinoConverter(), true);

            Client.OnError += Client_OnError;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;
            Client.OnReady += Client_OnReady;

            Client.IntializeSender((string)payload.account.apiToken, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid());
        }

        private void Client_OnReady(object source, SpeckleEventArgs e)
        {
            this.StreamId = Client.Stream.StreamId;
        }

        private void Client_OnWsMessage(object source, SpeckleEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Client_OnError(object source, SpeckleEventArgs e)
        {
            throw new NotImplementedException();
        }

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

        public void TogglePaused(bool status)
        {
            this.Paused = status;
        }

        public void ToggleVisibility(bool status)
        {
            this.Visible = status;
        }

        public void ToggleLayerHover(string layerId, bool status)
        {
            throw new NotImplementedException();
        }

        public void ToggleLayerVisibility(string layerId, bool status)
        {
            throw new NotImplementedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }
}

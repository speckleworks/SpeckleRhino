using Newtonsoft.Json;
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

        public dynamic InitPayload;

        System.Timers.Timer DataSender, MetadataSender;


        public List<string> TrackedObjects = new List<string>();


        public RhinoSender(string _payload, Interop _Context, SenderType _Type)
        {
            Context = _Context;
            Type = _Type;

            InitPayload = JsonConvert.DeserializeObject<ExpandoObject>(_payload);

            Client = new SpeckleApiClient((string)InitPayload.account.restApi, new RhinoConverter(), true);

            SetClientEvents();
            SetRhinoEvents();
            SetTimers();

            Client.IntializeSender((string)InitPayload.account.apiToken, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid());
        }

        public void SetRhinoEvents()
        {
            // layer table events: layer name change, layer colour
            // object events: 
            // ModifyAttributes
            // Delete
            // Add
            // Undelete
        }

        public void SetClientEvents()
        {
            Client.OnError += Client_OnError;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;
            Client.OnReady += Client_OnReady;
        }

        public void SetTimers()
        {
            MetadataSender = new System.Timers.Timer(500) { AutoReset = false, Enabled = false };
            MetadataSender.Elapsed += MetadataSender_Elapsed;

            DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
            DataSender.Elapsed += DataSender_Elapsed;
        }

        public void Trigger()
        {
            // By selection:
            // Parse scene objects, get only those with StreamId as property.
            // Do global update with:
            // new stream object list
            // new stream layer list
        }

        private void Client_OnReady(object source, SpeckleEventArgs e)
        {
            // By selection:
            // attach StreamId as prop to all objects in InitPayload.selection
            // create layers
            // convert and send objects / do global update

            // By layer:
            // get all objects from the layers and attach StreamId

            // Finally:
            // Do a full update with:
            // StreamName
            // StreamLayers
            // StreamObjects

            this.StreamId = Client.Stream.StreamId;

            Client.Stream.Name = InitPayload.streamName;

            switch (Type)
            {
                case SenderType.BySelection:
                    foreach (var layer in InitPayload.selection)
                        foreach (string guid in layer.ObjectGuids)
                            RhinoDoc.ActiveDoc.Objects.Find(new Guid(guid)).Attributes.SetUserString("spk_" + StreamId, StreamId);

                    DataSender.Start();
                    break;
                case SenderType.ByLayers:
                    Debug.WriteLine("TODO SenderType.byLayers");
                    break;
            }

            Context.NotifySpeckleFrame("client-add", StreamId, JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }));
            // notifiy speckle frame > add client
            // context.add client

        }

        private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
        {
            PayloadStreamUpdate payload = new PayloadStreamUpdate()
            {
                Name = Client.Stream.Name
            };

            var converter = new RhinoConverter();

            var pLayers = new List<SpeckleLayer>();
            var pObjects = new List<SpeckleObject>();

            var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString("spk_" + this.StreamId, "*", false).OrderBy(obj => obj.Attributes.LayerIndex);

            int lindex = -1, count = 0;
            foreach (var obj in objs)
            {
                if (lindex != obj.Attributes.LayerIndex)
                {
                    Layer layer = RhinoDoc.ActiveDoc.Layers[obj.Attributes.LayerIndex];
                    var spkLayer = new SpeckleLayer()
                    {
                        Name = layer.Name,
                        Guid = layer.Id.ToString(),
                        ObjectCount = 1,
                        StartIndex = count,
                        OrderIndex = obj.Attributes.LayerIndex,
                        Properties = new SpeckleLayerProperties()
                        {
                            Color = new SpeckleCore.Color() { A = 1, Hex = System.Drawing.ColorTranslator.ToHtml(layer.Color) },
                        }
                    };
                    pLayers.Add(spkLayer);
                    lindex = obj.Attributes.LayerIndex;
                }
                else
                {
                    pLayers[obj.Attributes.LayerIndex].ObjectCount++;
                }

                pObjects.Add(converter.ToSpeckle(obj.Geometry));
                pObjects[pObjects.Count - 1].ApplicationId = obj.Id.ToString();

                count++;
            }

            payload.Layers = pLayers;
            payload.Objects = pObjects.Select(obj =>
            {
                if (Context.ObjectCache.ContainsKey(obj.Hash))
                    return new SpeckleObjectPlaceholder() { Hash = obj.Hash, DatabaseId = Context.ObjectCache[obj.Hash].DatabaseId, ApplicationId = obj.ApplicationId };
                return obj;
            });

            var baseProps = new Dictionary<string, object>();
            baseProps["units"] = RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
            baseProps["precision"] = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            payload.BaseProperties = baseProps;

            var response = Client.StreamUpdate(payload, Client.Stream.StreamId);
            var copy2 = response;

            Client.BroadcastMessage(new { eventType = "update-global" });

            int k = 0;
            foreach (var obj in payload.Objects)
            {
                obj.DatabaseId = response.Objects[k++];
                Context.ObjectCache[obj.Hash] = obj;
            }

            Client.Stream.Layers = pLayers;
            Client.Stream.Objects = pObjects.Select(o => o.ApplicationId).ToList();


        }

        private void MetadataSender_Elapsed(object sender, ElapsedEventArgs e)
        {

        }

        private void Client_OnWsMessage(object source, SpeckleEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Client_OnError(object source, SpeckleEventArgs e)
        {
            //throw new NotImplementedException();
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

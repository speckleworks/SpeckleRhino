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

        System.Timers.Timer DataSender, MetadataSender;

        public List<string> TrackedObjects = new List<string>();

        public List<string> TrackedLayers = new List<string>();

        public string StreamName;

        public RhinoSender(string _payload, Interop _Context, SenderType _Type)
        {
            Context = _Context;
            Type = _Type;

            dynamic InitPayload = JsonConvert.DeserializeObject<ExpandoObject>(_payload);

            Client = new SpeckleApiClient((string)InitPayload.account.restApi, new RhinoConverter(), true);

            StreamName = (string)InitPayload.streamName;

            SetClientEvents();
            SetRhinoEvents();
            SetTimers();

            Client.IntializeSender((string)InitPayload.account.apiToken, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid()).ContinueWith(res =>
            {
                StreamId = Client.Stream.StreamId;
                Client.Stream.Name = StreamName;

                Context.NotifySpeckleFrame("client-add", StreamId, JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }));
                Context.UserClients.Add(this);

                InitTrackedObjects(InitPayload);
                SendUpdate(CreateUpdatePayload());

            });

        }

        public void InitTrackedObjects(dynamic payload)
        {
            if (StreamId == null) Debug.WriteLine("WHOOT WE GONNA FAIL");
            switch (Type)
            {
                case SenderType.BySelection:
                    foreach (var layer in payload.selection)
                        foreach (string guid in layer.ObjectGuids)
                        {
                            RhinoDoc.ActiveDoc.Objects.Find(new Guid(guid)).Attributes.SetUserString("spk_" + StreamId, StreamId);
                            TrackedObjects.Add(guid);
                        }
                    break;

                case SenderType.ByLayers:
                    Debug.WriteLine("TODO SenderType.byLayers");
                    break;
            }
        }

        public void AddTrackedObjects() { }

        public void AddTrackedLayers()
        {

        }

        public void SetRhinoEvents()
        {
            RhinoDoc.ModifyObjectAttributes += RhinoDoc_ModifyObjectAttributes;
            RhinoDoc.DeleteRhinoObject += RhinoDoc_DeleteRhinoObject;
            RhinoDoc.AddRhinoObject += RhinoDoc_AddRhinoObject;
            RhinoDoc.UndeleteRhinoObject += RhinoDoc_UndeleteRhinoObject;
            RhinoDoc.LayerTableEvent += RhinoDoc_LayerTableEvent;

            // Note: Replace is followed by a delete and one or more add events
        }

        private void RhinoDoc_LayerTableEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void RhinoDoc_UndeleteRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (DataSender.Enabled) return;
            if (TrackedObjects.Contains(e.ObjectId.ToString()))
            {
                DataSender.Start();
            }
        }

        private void RhinoDoc_AddRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (DataSender.Enabled)
            {
                TrackedObjects.Add(e.ObjectId.ToString());
                return;
            }
            if (e.TheObject.Attributes.GetUserString("spk_" + StreamId) != null)
            {
                DataSender.Start();
                TrackedObjects.Add(e.ObjectId.ToString());
            }
        }

        private void RhinoDoc_DeleteRhinoObject(object sender, RhinoObjectEventArgs e)
        {
            if (DataSender.Enabled)
            {
                TrackedObjects.Remove(e.ObjectId.ToString());
                return;
            }
            if (TrackedObjects.Contains(e.ObjectId.ToString()))
            {
                DataSender.Start();
                TrackedObjects.Remove(e.ObjectId.ToString());
            }
        }

        private void RhinoDoc_ModifyObjectAttributes(object sender, RhinoModifyObjectAttributesEventArgs e)
        {
            if (DataSender.Enabled) return;
            if (TrackedObjects.Contains(e.RhinoObject.Id.ToString()))
            {
                DataSender.Start();
            }
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

        private void Client_OnReady(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Ready Event."));
        }

        private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendUpdate(CreateUpdatePayload());
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Update Sent."));
        }

        private void MetadataSender_Elapsed(object sender, ElapsedEventArgs e)
        {
            // TODO
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Update Sent."));
        }

        private void Client_OnWsMessage(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("WS message received and ignored."));
        }

        private void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject(e.EventData));
        }

        private void Client_OnError(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-error", StreamId, JsonConvert.SerializeObject(e.EventData));
        }

        public void SendUpdate(PayloadStreamUpdate payload)
        {
            Context.NotifySpeckleFrame("client-is-loading", StreamId, "");
            var response = Client.StreamUpdate(payload, Client.Stream.StreamId);
            Client.BroadcastMessage(new { eventType = "update-global" });

            // commit to cache
            int k = 0;
            foreach (var obj in payload.Objects)
            {
                obj.DatabaseId = response.Objects[k++];
                Context.ObjectCache[obj.Hash] = obj;
            }

            Client.Stream.Layers = payload.Layers.ToList();
            Client.Stream.Objects = payload.Objects.Select(o => o.ApplicationId).ToList();

            Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson());
            Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        }

        /// <summary>
        /// This one just looks through the rinho object and pulls in any objects with a matching streamid property. 
        /// </summary>
        /// <returns></returns>
        public PayloadStreamUpdate CreateUpdatePayload()
        {
            PayloadStreamUpdate payload = new PayloadStreamUpdate() { Name = StreamName };

            var pLayers = new List<SpeckleLayer>();
            var pObjects = new List<SpeckleObject>();

            using (var converter = new RhinoConverter())
            {
                var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString("spk_" + this.StreamId, "*", false).OrderBy(obj => obj.Attributes.LayerIndex);

                // Assemble layers and objects
                int lindex = -1, count = 0, orderIndex = 0;
                foreach (var obj in objs)
                {
                    Layer layer = RhinoDoc.ActiveDoc.Layers[obj.Attributes.LayerIndex];
                    if (lindex != obj.Attributes.LayerIndex)
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
                                Color = new SpeckleCore.Color() { A = 1, Hex = System.Drawing.ColorTranslator.ToHtml(layer.Color) },
                            }
                        };
                        pLayers.Add(spkLayer);
                        lindex = obj.Attributes.LayerIndex;
                    }
                    else
                    {
                        var spkl = pLayers.FirstOrDefault(pl => pl.OrderIndex == obj.Attributes.LayerIndex);
                        spkl.ObjectCount++;
                    }

                    pObjects.Add(converter.ToSpeckle(obj.Geometry));
                    pObjects[pObjects.Count - 1].ApplicationId = obj.Id.ToString();

                    count++;
                }
            }

            foreach (var layer in pLayers)
            {
                layer.Topology = "0-" + layer.ObjectCount + " ";
            }

            payload.Layers = pLayers;
            // Go through cache
            payload.Objects = pObjects.Select(obj =>
            {
                if (Context.ObjectCache.ContainsKey(obj.Hash))
                    return new SpeckleObjectPlaceholder() { Hash = obj.Hash, DatabaseId = Context.ObjectCache[obj.Hash].DatabaseId, ApplicationId = obj.ApplicationId };
                return obj;
            });

            // Add some base properties
            var baseProps = new Dictionary<string, object>();
            baseProps["units"] = RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
            baseProps["tolerance"] = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            baseProps["angleTolerance"] = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
            payload.BaseProperties = baseProps;

            return payload;
        }

        public SpeckleCore.ClientRole GetRole()
        {
            return ClientRole.Sender;
        }

        public string GetClientId()
        {
            return Client.ClientId;
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

        public void Dispose()
        {
            //var objs = RhinoDoc.ActiveDoc.Objects.FindByUserString("spk_" + StreamId, "*", false).OrderBy(obj => obj.Attributes.LayerIndex);

            //foreach(var o in objs)
            //{
            //    o.Attributes.SetUserString("spk_" + StreamId, null);
            //}

            Client.Dispose();
        }


        public void CompleteDeserialisation(Interop _Context)
        {
            Context = _Context;

            Context.NotifySpeckleFrame("client-add", StreamId, JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }));
            Context.UserClients.Add(this);
        }

        protected RhinoSender(SerializationInfo info, StreamingContext context)
        {
            byte[] serialisedClient = Convert.FromBase64String((string)info.GetString("client"));

            using (var ms = new MemoryStream())
            {
                ms.Write(serialisedClient, 0, serialisedClient.Length);
                ms.Seek(0, SeekOrigin.Begin);
                Client = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
                StreamId = Client.StreamId;
            }

            SetClientEvents();
            SetRhinoEvents();
            SetTimers();

            Type = (SenderType)info.GetInt16("type"); // check for exceptions
            if (Type == SenderType.BySelection)
                TrackedObjects = info.GetValue("trackedobjects", typeof(List<string>)) as List<string>;
            else
                TrackedLayers = info.GetValue("trackedlayers", typeof(List<string>)) as List<string>;

            var copy = TrackedObjects;



            // Extra TODOS: 
            // get tracked objects & layers based on type
            // do i need to reinit, ie, or how do i check if things were updated in the meantime???
            // or just actually send a fucking update ANYWAYS

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, Client);
                info.AddValue("client", Convert.ToBase64String(ms.ToArray()));
                info.AddValue("paused", Paused);
                info.AddValue("visible", Visible);

                info.AddValue("type", Type);
                info.AddValue("trackedobjects", TrackedObjects);
                info.AddValue("trackedlayers", TrackedLayers);
            }
        }
    }
}

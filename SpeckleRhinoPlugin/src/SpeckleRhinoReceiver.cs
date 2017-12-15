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
    /// Class that holds a rhino receiver client warpped around the
    /// SpeckleApiClient.
    /// </summary>
    [Serializable]
    public class RhinoReceiver : ISpeckleRhinoClient
    {
        public Interop Context { get; set; }

        public SpeckleApiClient Client { get; set; }

        public List<SpeckleObject> Objects { get; set; }

        public SpeckleDisplayConduit Display;

        public string StreamId { get; private set; }

        public bool Paused { get; set; } = false;

        public bool Visible { get; set; } = true;

        public RhinoReceiver(string _payload, Interop _parent)
        {
            Context = _parent;
            dynamic payload = JsonConvert.DeserializeObject(_payload);

            StreamId = (string)payload.streamId;

            Client = new SpeckleApiClient((string)payload.account.restApi, new RhinoConverter(), true);

            Client.OnReady += Client_OnReady;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;
            Client.OnError += Client_OnError;

            Client.IntializeReceiver((string)payload.streamId, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid(), (string)payload.account.apiToken);

            Display = new SpeckleDisplayConduit();
            Display.Enabled = true;

            Objects = new List<SpeckleObject>();
        }

        #region events
        private void Client_OnError(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-error", StreamId, JsonConvert.SerializeObject(e.EventData, Context.SS));
        }

        public virtual void Client_OnLogData(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject(e.EventData, Context.SS));
        }

        public virtual void Client_OnReady(object source, SpeckleEventArgs e)
        {
            Context.NotifySpeckleFrame("client-add", StreamId, JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }, Context.SS));

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
                    UpdateMeta();
                    break;
                case "update-object":
                    break;
                case "update-children":
                    UpdateChildren();
                    break;
                default:
                    Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Unkown event: " + (string)e.EventObject.args.eventType, Context.SS));
                    break;
            }
        }
        #endregion

        #region updates
        public void UpdateMeta()
        {
            Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Metadata update received."));

            var streamGetResponse = Client.StreamGet(StreamId);
            if (streamGetResponse.Success == false)
            {
                Context.NotifySpeckleFrame("client-error", StreamId, streamGetResponse.Message);
                Context.NotifySpeckleFrame("client-log", StreamId, JsonConvert.SerializeObject("Failed to retrieve global update."));
            }

            Client.Stream = streamGetResponse.Stream;

            Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson());

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
            var COPY = Client.Stream;
            Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson());
            Context.NotifySpeckleFrame("client-is-loading", StreamId, "");

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
                Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
            });

        }

        public void UpdateChildren()
        {
            var getStream = Client.StreamGet(StreamId);
            Client.Stream = getStream.Stream;

            Context.NotifySpeckleFrame("client-children", StreamId, Client.Stream.ToJson());
        }
        #endregion

        #region display & helpers
        public void DisplayContents()
        {
            RhinoConverter rhinoConverter = new RhinoConverter();

            Display.Geometry = new List<GeometryBase>();

            int count = 0;
            foreach (SpeckleObject myObject in Objects)
            {
                var gb = rhinoConverter.ToNative(myObject);

                Display.Colors.Add(GetColorFromLayer(GetLayerFromIndex(count)));

                Display.VisibleList.Add(true);

                if (gb is GeometryBase)
                {
                    Display.Geometry.Add(gb as GeometryBase);
                }
                else
                {
                    Display.Geometry.Add(null);
                }

                count++;
            }

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public SpeckleLayer GetLayerFromIndex(int index)
        {
            return Client.Stream.Layers.FirstOrDefault(layer => ((index >= layer.StartIndex) && (index < layer.StartIndex + layer.ObjectCount)));
        }

        public System.Drawing.Color GetColorFromLayer(SpeckleLayer layer)
        {
            System.Drawing.Color layerColor = System.Drawing.ColorTranslator.FromHtml("#AEECFD");
            try
            {
                layerColor = System.Drawing.ColorTranslator.FromHtml(layer.Properties.Color.Hex);
            }
            catch
            {
                Debug.WriteLine("Layer '{0}' had no assigned color", layer.Name);
            }
            return layerColor;
        }

        public string GetClientId()
        {
            return Client.ClientId;
        }

        public ClientRole GetRole()
        {
            return ClientRole.Receiver;
        }
        #endregion

        #region Bake

        public void Bake()
        {
            string parent = String.Format("{1} | {0}", Client.Stream.StreamId, Client.Stream.Name);

            var parentId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent, true);
            if (parentId == -1)
            {
                var parentLayer = new Layer()
                {
                    Color = System.Drawing.Color.Black,
                    Name = parent
                };
                parentId = Rhino.RhinoDoc.ActiveDoc.Layers.Add(parentLayer);
            }
            else
                foreach (var layer in Rhino.RhinoDoc.ActiveDoc.Layers[parentId].GetChildren())
                    Rhino.RhinoDoc.ActiveDoc.Layers.Purge(layer.LayerIndex, false);

            foreach (var sLayer in Client.Stream.Layers)
            {
                var layerId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + sLayer.Name, true);
                if (layerId == -1)
                {
                    var layer = new Layer()
                    {
                        Name = sLayer.Name,
                        Id = Guid.Parse(sLayer.Guid),
                        ParentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[parentId].Id,
                        Color = GetColorFromLayer(sLayer),
                        IsVisible = true
                    };
                    var index = Rhino.RhinoDoc.ActiveDoc.Layers.Add(layer);

                    for (int i = (int)sLayer.StartIndex; i < sLayer.StartIndex + sLayer.ObjectCount; i++)
                    {
                        if (Display.Geometry[i] != null)
                        {
                            Rhino.RhinoDoc.ActiveDoc.Objects.Add(Display.Geometry[i], new ObjectAttributes() { LayerIndex = index });
                        }
                    }
                }
            }

            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void BakeLayer(string layerId)
        {
            SpeckleLayer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);

            // create or get parent
            string parent = String.Format("{1} | {0}", Client.Stream.StreamId, Client.Stream.Name);

            var parentId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent, true);
            if (parentId == -1)
            {
                var parentLayer = new Layer()
                {
                    Color = System.Drawing.Color.Black,
                    Name = parent
                };
                parentId = Rhino.RhinoDoc.ActiveDoc.Layers.Add(parentLayer);
            }
            else
            {
                int prev = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + myLayer.Name, true);
                if (prev != -1)
                    Rhino.RhinoDoc.ActiveDoc.Layers.Purge(prev, true);
            }

            int theLayerId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + myLayer.Name, true);
            if (theLayerId == -1)
            {
                var layer = new Layer()
                {
                    Name = myLayer.Name,
                    Id = Guid.Parse(myLayer.Guid),
                    ParentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[parentId].Id,
                    Color = GetColorFromLayer(myLayer),
                    IsVisible = true
                };
                var index = Rhino.RhinoDoc.ActiveDoc.Layers.Add(layer);
                for (int i = (int)myLayer.StartIndex; i < myLayer.StartIndex + myLayer.ObjectCount; i++)
                {
                    if (Display.Geometry[i] != null)
                    {
                        Rhino.RhinoDoc.ActiveDoc.Objects.Add(Display.Geometry[i], new ObjectAttributes() { LayerIndex = index });
                    }
                }
            }
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        #endregion

        #region Toggles

        public void TogglePaused(bool status)
        {
            this.Paused = status;
        }

        public void ToggleVisibility(bool status)
        {
            this.Display.Enabled = status;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void ToggleLayerHover(string layerId, bool status)
        {
            SpeckleLayer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);
            if (myLayer == null) throw new Exception("Bloopers. Layer not found.");

            if (status)
            {
                Display.HoverRange = new Interval((double)myLayer.StartIndex, (double)(myLayer.StartIndex + myLayer.ObjectCount));
            }
            else
                Display.HoverRange = null;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void ToggleLayerVisibility(string layerId, bool status)
        {
            SpeckleLayer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);
            if (myLayer == null) throw new Exception("Bloopers. Layer not found.");

            for (int i = (int)myLayer.StartIndex; i < myLayer.StartIndex + myLayer.ObjectCount; i++)
                Display.VisibleList[i] = status;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }
        #endregion

        #region serialisation & end of life

        public void Dispose(bool delete = false)
        {
            Client.Dispose(delete);
            Display.Enabled = false;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        public void Dispose()
        {
            Client.Dispose();
            Display.Enabled = false;
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
        }

        protected RhinoReceiver(SerializationInfo info, StreamingContext context)
        {
            Display = new SpeckleDisplayConduit();
            Display.Enabled = true;

            Objects = new List<SpeckleObject>();

            byte[] serialisedClient = Convert.FromBase64String((string)info.GetString("client"));

            using (var ms = new MemoryStream())
            {
                ms.Write(serialisedClient, 0, serialisedClient.Length);
                ms.Seek(0, SeekOrigin.Begin);
                Client = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
                StreamId = Client.StreamId;
            }

            Client.OnReady += Client_OnReady;
            Client.OnLogData += Client_OnLogData;
            Client.OnWsMessage += Client_OnWsMessage;
            Client.OnError += Client_OnError;
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
            }
        }
        #endregion
    }
}

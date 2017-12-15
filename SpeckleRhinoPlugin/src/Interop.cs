using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp.WinForms;
using CefSharp;
using Newtonsoft.Json;
using System.IO;

using SpeckleCore;
using SpeckleRhinoConverter;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using Rhino;
using System.Dynamic;
using Rhino.DocObjects;
using Newtonsoft.Json.Serialization;

namespace SpeckleRhino
{
    // CEF Bound object. 
    // If CEF will be removed, porting to url hacks will be necessary,
    // so let's keep the methods as simple as possible.

    public class Interop
    {
        private static ChromiumWebBrowser Browser;
        private static WinForm mainForm;

        private List<SpeckleAccount> UserAccounts;
        public List<ISpeckleRhinoClient> UserClients;

        public Dictionary<string, SpeckleObject> ObjectCache;

        public bool SpeckleIsReady = false;

        public Interop(ChromiumWebBrowser _originalBrowser, WinForm _mainForm)
        {

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            Browser = _originalBrowser;
            mainForm = _mainForm;

            UserAccounts = new List<SpeckleAccount>();

            UserClients = new List<ISpeckleRhinoClient>();

            ObjectCache = new Dictionary<string, SpeckleObject>();

            ReadUserAccounts();

            RhinoDoc.NewDocument += (sender, e) =>
            {
                Debug.WriteLine("NEW DOC");
                NotifySpeckleFrame("purge-clients", "", "");
                RemoveAllClients();
            };

            RhinoDoc.EndOpenDocument += (sender, e) =>
            {
                // this seems to cover the copy paste issues
                if (e.Merge) return;
                // purge clients from ui
                NotifySpeckleFrame("client-purge", "", "");
                // purge clients from here
                RemoveAllClients();
                // read clients from document strings
                InstantiateFileClients();
            };

            RhinoDoc.BeginSaveDocument += (sender, e) =>
            {
                Debug.WriteLine("BEGIN SAVE DOC");
                SaveFileClients();
            };

            // selection stuff
            RhinoDoc.SelectObjects += (sender, e) =>
            {
                if (SpeckleIsReady)
                    NotifySpeckleFrame("object-selection", "", this.getLayersAndObjectsInfo());
            };

            RhinoDoc.DeselectObjects += (sender, e) =>
            {
                if (SpeckleIsReady)
                    NotifySpeckleFrame("object-selection", "", this.getLayersAndObjectsInfo());
            };

            RhinoDoc.DeselectAllObjects += (sender, e) =>
            {
                if (SpeckleIsReady)
                    NotifySpeckleFrame("object-selection", "", this.getLayersAndObjectsInfo());
            };

            RhinoApp.Idle += (sender, e) =>
            {
                //Debug.WriteLine("App Idle event fired.");
            };
        }

        #region General Utils

        public void ShowDev()
        {
            Browser.ShowDevTools();
        }

        public string GetDocumentName()
        {
            return Rhino.RhinoDoc.ActiveDoc.Name;
        }

        public string GetDocumentGuid()
        {
            return Rhino.RhinoDoc.ActiveDoc.DocumentId.ToString();
        }
        #endregion

        #region Serialisation & Init. 

        /// <summary>
        /// Do not call this from the constructor as you'll get confilcts with 
        /// browser load, etc.
        /// </summary>
        public void AppReady()
        {
            SpeckleIsReady = true;
            InstantiateFileClients();
        }

        public void SaveFileClients()
        {
            RhinoDoc myDoc = RhinoDoc.ActiveDoc;
            foreach (ISpeckleRhinoClient rhinoClient in UserClients)
            {
                using (var ms = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(ms, rhinoClient);
                    string section = rhinoClient.GetRole() == ClientRole.Receiver ? "speckle-client-receivers" : "speckle-client-senders";
                    var client = Convert.ToBase64String(ms.ToArray());
                    var clientId = rhinoClient.GetClientId();
                    RhinoDoc.ActiveDoc.Strings.SetString(section, clientId, client);
                }
            }
        }

        public void InstantiateFileClients()
        {
            string[] receiverKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames("speckle-client-receivers");

            foreach (string rec in receiverKeys)
            {
                byte[] serialisedClient = Convert.FromBase64String(RhinoDoc.ActiveDoc.Strings.GetValue("speckle-client-receivers", rec));
                using (var ms = new MemoryStream())
                {
                    ms.Write(serialisedClient, 0, serialisedClient.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    RhinoReceiver client = (RhinoReceiver)new BinaryFormatter().Deserialize(ms);
                    client.Context = this;
                    // is there maybe a race condition here, where on ready is triggered 
                    // faster than the context get set?
                }
            }

            string[] senderKeys = RhinoDoc.ActiveDoc.Strings.GetEntryNames("speckle-client-senders");

            foreach(string sen in senderKeys)
            {
                byte[] serialisedClient = Convert.FromBase64String(RhinoDoc.ActiveDoc.Strings.GetValue("speckle-client-senders", sen));

                using (var ms = new MemoryStream())
                {
                    ms.Write(serialisedClient, 0, serialisedClient.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    RhinoSender client = (RhinoSender)new BinaryFormatter().Deserialize(ms);
                    client.CompleteDeserialisation(this);
                }
            }
        }
        #endregion

        #region Account Management

        public string GetUserAccounts()
        {
            ReadUserAccounts();
            return JsonConvert.SerializeObject(UserAccounts);
        }

        private void ReadUserAccounts()
        {
            UserAccounts = new List<SpeckleAccount>();
            string strPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            strPath = strPath + @"\SpeckleSettings";

            if (Directory.Exists(strPath) && Directory.EnumerateFiles(strPath, "*.txt").Count() > 0)
                foreach (string file in Directory.EnumerateFiles(strPath, "*.txt"))
                {
                    string content = File.ReadAllText(file);
                    string[] pieces = content.TrimEnd('\r', '\n').Split(',');
                    UserAccounts.Add(new SpeckleAccount() { email = pieces[0], apiToken = pieces[1], serverName = pieces[2], restApi = pieces[3], rootUrl = pieces[4], fileName = file });
                }
        }

        public void AddAccount(string payload)
        {

        }

        public void RemoveAccount(string payload)
        {
            var x = UserAccounts.RemoveAll(account => { return account.fileName == payload; });
            // TODO: Delete file, or move it to special folder
        }
        #endregion

        #region Client Management
        public bool AddReceiverClient(string _payload)
        {
            var myReceiver = new RhinoReceiver(_payload, this);
            return true;
        }

        public bool AddSenderClientFromLayers(string _payload)
        {
            // TODO
            return true;
        }

        public bool AddSenderClientFromSelection(string _payload)
        {
            var mySender = new RhinoSender(_payload, this, SenderType.BySelection);
            return true;
        }

        public bool RemoveClient(string _payload)
        {
            var myClient = UserClients.FirstOrDefault(client => client.GetClientId() == _payload);
            if (myClient == null) return false;

            RhinoDoc.ActiveDoc.Strings.Delete(myClient.GetRole() == ClientRole.Receiver ? "speckle-client-receivers" : "speckle-client-senders", myClient.GetClientId());

            myClient.Dispose(true);

            return UserClients.Remove(myClient);
        }

        public bool RemoveAllClients()
        {
            foreach (var uc in UserClients)
            {
                uc.Dispose();
            }
            UserClients.RemoveAll(c => true);
            return true;
        }

        #endregion

        #region To UI (Generic)
        public void NotifySpeckleFrame(string EventType, string StreamId, string EventInfo)
        {
            var script = string.Format("window.EventBus.$emit('{0}', '{1}', '{2}')", EventType, StreamId, EventInfo);
            Browser.GetMainFrame().EvaluateScriptAsync(script);
        }
        #endregion

        #region From UI (..)

        public void bakeClient(string clientId)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null || myClient is RhinoReceiver)
                ((RhinoReceiver)myClient).Bake();

        }

        public void bakeLayer(string clientId, string layerGuid)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null || myClient is RhinoReceiver)
                ((RhinoReceiver)myClient).BakeLayer(layerGuid);
        }

        public void setClientPause(string clientId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.TogglePaused(status);
        }

        public void setClientVisibility(string clientId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.ToggleVisibility(status);
        }

        public void setClientHover(string clientId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.ToggleVisibility(status);
        }

        public void setLayerVisibility(string clientId, string layerId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.ToggleLayerVisibility(layerId, status);
        }

        public void setLayerHover(string clientId, string layerId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.ToggleLayerHover(layerId, status);
        }

        public void setObjectHover(string clientId, string layerId, bool status)
        {

        }

        public void AddRemoveObjects(string clientId, string _guids, bool remove)
        {
            string[] guids = JsonConvert.DeserializeObject<string[]>(_guids);

            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                try
                {
                    if(!remove)
                    ((RhinoSender)myClient).AddTrackedObjects(guids);
                    else ((RhinoSender)myClient).RemoveTrackedObjects(guids);

                }
                catch { throw new Exception("Force send client was not a sender. whoopsie poopsiee."); }
        }

        public void refreshClient(string clientId)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                try
                {
                    ((RhinoReceiver)myClient).UpdateGlobal();
                }
                catch { throw new Exception("Refresh client was not a receiver. whoopsie poopsiee."); }
        }

        public void forceSend(string clientId)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                try
                {
                    ((RhinoSender)myClient).ForceUpdate();
                }
                catch { throw new Exception("Force send client was not a sender. whoopsie poopsiee."); }
        }

        #endregion

        #region Sender Helpers

        public string getLayersAndObjectsInfo(bool ignoreSelection = false)
        {
            List<RhinoObject> SelectedObjects;
            List<LayerSelection> layerInfoList = new List<LayerSelection>();
            if (!ignoreSelection)
            {
                SelectedObjects = RhinoDoc.ActiveDoc.Objects.GetSelectedObjects(false, false).ToList();
            }
            else
            {
                SelectedObjects = RhinoDoc.ActiveDoc.Objects.ToList();
                foreach(Layer ll in RhinoDoc.ActiveDoc.Layers)
                {
                    layerInfoList.Add(new LayerSelection()
                    {
                        objectCount = 0,
                        layerName = ll.FullPath,
                        color = System.Drawing.ColorTranslator.ToHtml(ll.Color),
                        ObjectGuids = new List<string>(),
                        ObjectTypes = new List<string>()
                    });
                }
            }

            SelectedObjects = SelectedObjects.OrderBy(o => o.Attributes.LayerIndex).ToList();

            foreach (var obj in SelectedObjects)
            {
                var layer = RhinoDoc.ActiveDoc.Layers[obj.Attributes.LayerIndex];
                var myLInfo = layerInfoList.FirstOrDefault(l => l.layerName == layer.FullPath);

                if (myLInfo != null)
                {
                    myLInfo.objectCount++;
                    myLInfo.ObjectGuids.Add(obj.Id.ToString());
                    myLInfo.ObjectTypes.Add(obj.Geometry.GetType().ToString());
                }
                else
                {
                    var myNewLinfo = new LayerSelection()
                    {
                        objectCount = 1,
                        layerName = layer.FullPath,
                        color = System.Drawing.ColorTranslator.ToHtml(layer.Color),
                        ObjectGuids = new List<string>(new string[] { obj.Id.ToString() }),
                        ObjectTypes = new List<string>(new string[] { obj.Geometry.GetType().ToString() })
                    };
                    layerInfoList.Add(myNewLinfo);
                }
            }

            return JsonConvert.SerializeObject(layerInfoList);
        }

        #endregion
    }

    [Serializable]
    public class LayerSelection
    {
        public string layerName;
        public int objectCount;
        public string color;
        public List<string> ObjectGuids;
        public List<string> ObjectTypes;
    }
}

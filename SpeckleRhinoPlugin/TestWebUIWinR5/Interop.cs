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

        public Interop(ChromiumWebBrowser _originalBrowser, WinForm _mainForm)
        {
            Browser = _originalBrowser;
            mainForm = _mainForm;

            UserAccounts = new List<SpeckleAccount>();

            UserClients = new List<ISpeckleRhinoClient>();

            ObjectCache = new Dictionary<string, SpeckleObject>();

            ReadUserAccounts();
            ReadFileClients();
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

        #region Serialisation
        private void ReadFileClients()
        {

        }

        public string GetFileStreams()
        {
            return JsonConvert.SerializeObject(UserClients);
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

        public bool AddSenderClient(string _payload)
        {
            // TODO
            return true;
        }

        public bool RemoveClient(string _payload)
        {
            var myClient = UserClients.FirstOrDefault(client => client.GetClientId() == _payload);
            if (myClient != null)
                myClient.Dispose();

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

        #region From UI (...)

        public void bakeClient(string clientId)
        {

        }

        public void setClientPause( string clientId, bool status)
        {
            var myClient = UserClients.FirstOrDefault(c => c.GetClientId() == clientId);
            if (myClient != null)
                myClient.TogglePaused(status);
        }

        public void setClientVisibility( string clientId, bool status)
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

        }

        public void setLayerHover(string clientId, string layerId, bool status)
        {

        }

        public void setObjectHover(string clientId, string layerId, bool status)
        {

        }

        #endregion
    }
}

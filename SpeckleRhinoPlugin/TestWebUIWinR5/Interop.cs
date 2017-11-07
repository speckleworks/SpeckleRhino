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
        private List<SpeckleApiClient> UserClients;

        public Interop(ChromiumWebBrowser _originalBrowser, WinForm _mainForm)
        {
            Browser = _originalBrowser;
            mainForm = _mainForm;
            UserAccounts = new List<SpeckleAccount>();
            UserClients = new List<SpeckleApiClient>();
            ReadUserAccounts();
            ReadFileClients();
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

        private void ReadFileClients()
        {

        }

        public void ShowDev()
        {
            Browser.ShowDevTools();
        }

        public string GetUserAccounts()
        {
            ReadUserAccounts();
            return JsonConvert.SerializeObject(UserAccounts);
        }

        public string GetFileStreams()
        {
            return JsonConvert.SerializeObject(UserClients);
        }

        public void AddAccount(string payload)
        {

        }

        public void RemoveAccount(string payload)
        {
            var x = UserAccounts.RemoveAll(account => { return account.fileName == payload; });
            var y = x;
        }

        public bool AddReceiverClient(string _payload)
        {
            System.Diagnostics.Debug.WriteLine(_payload);
            dynamic payload = JsonConvert.DeserializeObject(_payload);

            SpeckleApiClient newClient = new SpeckleApiClient((string)payload.account.restApi, new RhinoConverter(), true);

            newClient.OnReady += (sender, e) =>
            {
                NotifySpeckleFrame("client-add", ((SpeckleApiClient)sender).StreamId, JsonConvert.SerializeObject(new { stream = newClient.Stream, client = newClient}));
            };

            newClient.OnLogData += (sender, e) =>
            {
                NotifySpeckleFrame("client-log", ((SpeckleApiClient)sender).StreamId, JsonConvert.SerializeObject(e.EventData));
            };

            newClient.OnWsMessage += (sender, e) =>
            {
                Debug.WriteLine("Got ws message." + ((SpeckleApiClient) sender).StreamId );



                NotifySpeckleFrame("client-ws-message", ((SpeckleApiClient)sender).StreamId, JsonConvert.SerializeObject(e.EventObject));
            };

            newClient.IntializeReceiver((string)payload.streamId, "test", "rhino", "test", (string)payload.account.apiToken).Wait();

            UserClients.Add(newClient);

            return true;
        }

        public bool RemoveReceiverClient(string _payload)
        {
            return true;
        }

        public bool AddSenderClient(string _payload)
        {
            return true;
        }

        public bool RemoveSenderClient(string _payload)
        {
            return true;
        }

        public bool RemoveClient(string _payload)
        {
            var myClient = UserClients.FirstOrDefault(client => client.ClientId == _payload);
            if(myClient!=null)
                myClient.Dispose();

            return UserClients.Remove(myClient);
        }

        public bool RemoveAllClients()
        {
            foreach(var uc in UserClients)
            {
                uc.Dispose();
            }
            UserClients.RemoveAll( c => true);
            return true;
        }

        public void NotifySpeckleFrame(string EventType, string StreamId, string EventInfo)
        {
            var script = string.Format("window.EventBus.$emit('{0}', '{1}', '{2}')", EventType, StreamId, EventInfo);
            Browser.GetMainFrame().EvaluateScriptAsync(script);
        }
    }
}

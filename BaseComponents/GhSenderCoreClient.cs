using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Diagnostics;
using System.Threading;

using Grasshopper.Kernel.Parameters;
using GH_IO.Serialization;

using SpeckleCore;
using SpeckleRhinoConverter;
using SpecklePopup;

using System.Dynamic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using SpeckleGrasshopper.Properties;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace SpeckleGrasshopper
{
    public class GhSenderClient : GH_Component, IGH_VariableParameterComponent
    {
        string streamId, clientId, baseUrl;
        string Log { get; set; }
        bool ready;

        Action expireComponentAction;

        SpeckleApiClient mySender;

        public GhSenderClient()
          : base("Data Sender", "Data Sender",
              "Sends data to Speckle.",
              "Speckle", "I/O")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            try
            {
                if (mySender != null)
                    using (var ms = new MemoryStream())
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(ms, mySender);
                        writer.SetByteArray("speckleclient", ms.ToArray());
                    }
            }
            catch { }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            try
            {
                var serialisedClient = reader.GetByteArray("speckleclient");
                using (var ms = new MemoryStream())
                {
                    ms.Write(serialisedClient, 0, serialisedClient.Length);
                    ms.Seek(0, SeekOrigin.Begin);
                    mySender = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
                    var x = mySender;
                }
            }
            catch { }

            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            ready = false;

            if (mySender == null)
            {
                var myForm = new SpecklePopup.MainWindow();

                var some = new System.Windows.Interop.WindowInteropHelper(myForm);
                some.Owner = Rhino.RhinoApp.MainWindowHandle();

                myForm.ShowDialog();

                if (myForm.restApi != null && myForm.apitoken != null)
                {
                    mySender = new SpeckleApiClient(myForm.restApi, new RhinoConverter());
                    mySender.IntializeSender(myForm.apitoken).ContinueWith(task =>
                    {
                        streamId = task.Result;
                        Rhino.RhinoApp.MainApplicationWindow.Invoke(expireComponentAction);
                    });
                }
            } else { mySender.Converter = new RhinoConverter(); }

            mySender.OnError += OnError;

            mySender.OnWsMessage += OnWsMessage;

            mySender.OnLogData += (sender, e) =>
            {
                this.Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData);
            };

            expireComponentAction = () => this.ExpireSolution(true);

            this.ObjectChanged += (sender, e) => UpdateMetadata();

            foreach (var param in Params.Input) param.ObjectChanged += (sender, e) => UpdateMetadata();

            Grasshopper.Instances.DocumentServer.DocumentRemoved += (sender, e) =>
            {
                if (e.DocumentID == document.DocumentID)
                {
                    var payload = new PayloadClientUpdate();
                    payload.Client = new SpeckleClient();
                    payload.Client.Online = false;

                    mySender.ClientUpdate(payload, mySender.ClientId);
                    mySender.Dispose();
                }
            };
        }

        public virtual void OnError(object source, SpeckleEventArgs e)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventData);
        }

        public virtual void OnWsMessage(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease.");
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (mySender != null) mySender.Dispose();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
                if (mySender != null) mySender.Dispose();

            base.DocumentContextChanged(document, context);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            GH_DocumentObject.Menu_AppendItem(menu, @"Broadcast test message.", (sender, e) =>
            {
                mySender.BroadcastMessage(new { eventType = "get-sliders" } );
                //mySender.SendMessage();
            });
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("out", "out", "Log data.", GH_ParamAccess.item);
            pManager.AddTextParameter("ID", "ID", "The stream's short id.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData(0, Log);
            DA.SetData(1, mySender.StreamId);

            if (!mySender.IsConnected) return;

            mySender.UpdateDataDebonuced(this.NickName, GetData(), GetLayers());
        }

        public void UpdateMetadata()
        {
            mySender.UpdateMetadataDebounced(this.NickName, GetLayers());
        }

        public List<object> GetData()
        {
            List<object> data = new List<dynamic>();
            foreach (IGH_Param myParam in Params.Input)
            {
                foreach (object o in myParam.VolatileData.AllData(false))
                    data.Add(o);
            }
            return data;
        }

        public List<SpeckleLayer> GetLayers()
        {
            List<SpeckleLayer> layers = new List<SpeckleLayer>();
            int startIndex = 0;
            int count = 0;
            foreach (IGH_Param myParam in Params.Input)
            {
                SpeckleLayer myLayer = new SpeckleLayer(
                    myParam.NickName,
                    myParam.InstanceGuid.ToString(),
                    GetParamTopology(myParam),
                    myParam.VolatileDataCount,
                    startIndex,
                    count);

                layers.Add(myLayer);
                startIndex += myParam.VolatileDataCount;
                count++;
            }
            return layers;
        }

        public string GetParamTopology(IGH_Param param)
        {
            string topology = "";
            foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
            {
                topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
            }
            return topology;
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
        {
            //We can only remove from the input
            if (side == GH_ParameterSide.Input && Params.Input.Count > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
        {
            Grasshopper.Kernel.Parameters.Param_GenericObject param = new Param_GenericObject();

            param.Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input);
            param.NickName = param.Name;
            param.Description = "Things to be sent around.";
            param.Optional = true;
            param.Access = GH_ParamAccess.tree;

            param.AttributesChanged += (sender, e) => Debug.WriteLine("Attributes have changed! (of param)");
            param.ObjectChanged += (sender, e) => UpdateMetadata();

            this.UpdateMetadata();

            return param;
        }

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            this.UpdateMetadata();
            return true;
        }

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.sender_2;
                //return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{e66e6873-ddcd-4089-93ff-75ae09f8ada3}"); }
        }
    }
}


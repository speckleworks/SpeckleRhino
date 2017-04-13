using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Diagnostics;
using System.Threading;

using Grasshopper.Kernel.Parameters;
using GH_IO.Serialization;

using SpeckleCommon;
using SpeckleGhRhConverter;
using SpecklePopup;

using System.Dynamic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using SpecklePopup;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper
{
    public class GhSenderClient : GH_Component, IGH_VariableParameterComponent
    {
        string streamId, wsSessionId;
        string serialisedSender;
        bool ready;

        Action expireComponentAction;

        SpeckleSender mySender;

        public GhSenderClient()
          : base("Speckle Sender", "Speckle Sender",
              "Speckle Sender",
              "Speckle", "Speckle")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            if (mySender != null)
                writer.SetString("specklesender", mySender.ToString());

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("specklesender", ref serialisedSender);
            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            ready = false;

            if (serialisedSender != null)
                mySender = new SpeckleSender(serialisedSender, new GhRhConveter(true, true));
            else
            {

                var myForm = new SpecklePopup.MainWindow();

                var some = new System.Windows.Interop.WindowInteropHelper(myForm);
                some.Owner = Rhino.RhinoApp.MainWindowHandle();

                myForm.ShowDialog();

                if (myForm.restApi != null && myForm.apitoken != null)
                    mySender = new SpeckleSender(myForm.restApi, myForm.apitoken, new GhRhConveter(true, true));
            }

            if (mySender == null) return;

            mySender.OnError += OnError;

            mySender.OnReady += OnReady;

            mySender.OnDataSent += OnDataSent;

            mySender.OnMessage += OnVolatileMessage;

            mySender.OnBroadcast += OnBroadcast;

            expireComponentAction = () => this.ExpireSolution(true);

            this.ObjectChanged += (sender, e) => updateMetadata();

            foreach (var param in Params.Input)
                param.ObjectChanged += (sender, e) => updateMetadata();
        }

        public virtual void OnError(object source, SpeckleEventArgs e)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventInfo);
        }

        public virtual void OnReady(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("Sender ready:::" + (string)e.Data.streamId + ":::" + (string)e.Data.wsSessionId);
            this.ready = true;
            this.streamId = e.Data.streamId;
            this.wsSessionId = e.Data.wsSessionId;
        }

        public virtual void OnDataSent(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("Data was sent. Stop the loading bar :) Wait. What loading bar? The one luis wanted! Where is it? I dunno");

            this.Message = "Data sent.";
        }

        public virtual void OnVolatileMessage(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease.");
        }

        public virtual void OnBroadcast(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Sender] Got a volatile broadcast. Extend this class and implement custom protocols at ease.");
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if(mySender!=null) mySender.Dispose();
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
            GH_DocumentObject.Menu_AppendItem(menu, @"Save current state", (sender, e) =>
            {
                mySender.saveToHistory();
            });
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "URL", "Link to the latest uploaded file.", GH_ParamAccess.item);
            pManager.AddTextParameter("ID", "ID", "Stream Id", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (mySender == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to initialise.");
                return;
            }
            DA.SetData(0, mySender.getServer() + @"/streams/" + mySender.getStreamId());
            DA.SetData(1, mySender.getStreamId());

            if (!ready) return;

            updateData();

            this.Message = "Sending Data...";
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
            param.ObjectChanged += (sender, e) => updateMetadata();

            return param;
        }

        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        void IGH_VariableParameterComponent.VariableParameterMaintenance()
        {
        }


        public string getTopology(IGH_Param param)
        {
            string topology = "";
            foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
            {
                topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
            }
            return topology;
        }

        public List<SpeckleLayer> getLayers()
        {
            List<SpeckleLayer> layers = new List<SpeckleLayer>();
            int startIndex = 0;
            int count = 0;
            foreach (IGH_Param myParam in Params.Input)
            {
                SpeckleLayer myLayer = new SpeckleLayer(
                    myParam.NickName,
                    myParam.InstanceGuid.ToString(), 
                    getTopology(myParam),
                    myParam.VolatileDataCount,
                    startIndex,
                    count);

                layers.Add(myLayer);
                startIndex += myParam.VolatileDataCount;
                count++;
            }
            return layers;
        }

        public List<object> getData()
        {
            List<object> data = new List<dynamic>();
            foreach (IGH_Param myParam in Params.Input)
            {
                foreach (object o in myParam.VolatileData.AllData(false))
                    data.Add(o);
            }
            return data;
        }

        public void updateMetadata()
        {
            Debug.WriteLine("Component: UPDATING METADATA");
            mySender.sendMetadataUpdate(getLayers(), this.NickName);
        }

        public void updateData()
        {
            Debug.WriteLine("Component: UPDATING DATA");
            mySender.sendDataUpdate(getData(), getLayers(), this.NickName);
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
            get { return new Guid("{82564680-f008-4f29-bcfc-af782a9237ca}"); }
        }
    }
}

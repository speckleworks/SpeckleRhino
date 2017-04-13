using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleCommon;
using SpeckleGhRhConverter;
using SpecklePopup;


using Grasshopper;
using Grasshopper.Kernel.Data;

namespace SpeckleGrasshopper
{
    public class GhReceiverClient : GH_Component, IGH_VariableParameterComponent
    {
        string apiUrl;
        string token;
        string streamId;
        string serialisedReceiver;
        List<string> senderGuids;

        SpeckleReceiver myReceiver;
        List<SpeckleLayer> layers;
        List<object> objects;

        Action expireComponentAction;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GhReceiverClient()
          : base("Speckle Receiver", "Speckle Receiver",
              "Speckle Receier",
              "Speckle", "Speckle")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            if (myReceiver != null)
                writer.SetString("serialisedReceiver", myReceiver.ToString());
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("serialisedReceiver", ref serialisedReceiver);
            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            senderGuids = new List<string>();

            if (serialisedReceiver != null)
            {
                myReceiver = new SpeckleReceiver(serialisedReceiver, new GhRhConveter(true, true));
                streamId = myReceiver.getStreamId();
                apiUrl = myReceiver.getServer();
                token = myReceiver.getToken();

                registermyReceiverEvents();
            } else
            {
                var myForm = new SpecklePopup.MainWindow();

                var some = new System.Windows.Interop.WindowInteropHelper(myForm);
                some.Owner = Rhino.RhinoApp.MainWindowHandle();

               myForm.ShowDialog();

                if (myForm.restApi != null && myForm.apitoken != null)
                {
                    apiUrl = myForm.restApi;
                    token = myForm.apitoken;
                }
            }

            expireComponentAction = () => this.ExpireSolution(true);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (myReceiver != null)
                myReceiver.Dispose();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                myReceiver.Dispose();
            }
            base.DocumentContextChanged(document, context);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("ID", "ID", "Which speckle stream do you want to connect to?", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string inputId = null;
            DA.GetData(0, ref inputId);
            Debug.WriteLine("StreamId: " + streamId + " Read ID: " + inputId);

            if(apiUrl==null || token == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to init. No server details.");
                return;
            }

            if (inputId == null && streamId == null)
            {
                Debug.WriteLine("No streamId to listen to.");
                return;
            }
            else if ((inputId != streamId) && (inputId != null))
            {
                Debug.WriteLine("changing streamid");
                streamId = inputId;
                if (myReceiver != null) myReceiver.Dispose();

                myReceiver = new SpeckleReceiver(apiUrl, token, streamId, new GhRhConveter(true, true));

                registermyReceiverEvents();
                Message = "";
                return;
            }

            setObjects(DA, objects, layers);
        }

        void registermyReceiverEvents()
        {
            if (myReceiver == null) return;

            myReceiver.OnDataMessage +=OnDataMessage;

            myReceiver.OnError += OnError;

            myReceiver.OnReady += OnReady;

            myReceiver.OnMetadata += OnMetadata;

            myReceiver.OnData += OnData;

            myReceiver.OnHistory += OnHistory;

            myReceiver.OnMessage += OnVolatileMessage;

            myReceiver.OnBroadcast += OnBroadcast;
        }

        private void OnDataMessage(object source, SpeckleEventArgs e)
        {
            this.Message = "Update in progress.";
        }

        #region virtual event handlers

        public virtual void OnError(object source, SpeckleEventArgs e)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventInfo);
        }

        public virtual void OnReady(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Receiver] Got a ready message. Extend this class and implement custom protocols at ease.");

            this.Name = this.NickName = (string)e.Data.name;
            diffStructure(e.Data.layers);
            layers = e.Data.layers;
            objects = e.Data.objects;

            Debug.WriteLine("ready event");
            Rhino.RhinoApp.MainApplicationWindow.Invoke(expireComponentAction);
        }

        public virtual void OnMetadata(object source, SpeckleEventArgs e)
        {
            this.Name = this.NickName = (string)e.Data.name;
            diffStructure(e.Data.layers);
            layers = e.Data.layers;

            Debug.WriteLine("metadata event");
        }

        public virtual void OnData(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("RECEIVER: Received live update event: " + e.EventInfo);
            this.Name = this.NickName = (string)e.Data.name;
            diffStructure(e.Data.layers);
            layers = e.Data.layers;
            objects = e.Data.objects;

            Debug.WriteLine("data event");
            this.Message = "Loaded update.";
            Rhino.RhinoApp.MainApplicationWindow.Invoke(expireComponentAction);
        }

        public virtual void OnHistory(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("Received history update event: " + e.EventInfo);
        }

        public virtual void OnVolatileMessage(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Receiver] Got a volatile message. Extend this class and implement custom protocols at ease.");
        }

        public virtual void OnBroadcast(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Receiver] Got a volatile broadcast. Extend this class and implement custom protocols at ease.");
        }

        #endregion

        #region workhorses

        public void diffStructure(List<SpeckleLayer> newLayers)
        {
            dynamic diffResult = SpeckleLayer.diffLayers(getLayers(), newLayers);

            foreach (SpeckleLayer layer in diffResult.toRemove)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.guid; });

                if (myparam != null)
                    Params.UnregisterOutputParameter(myparam);
            }

            foreach (var layer in diffResult.toAdd)
            {
                Param_GenericObject newParam = getGhParameter(layer);
                Params.RegisterOutputParam(newParam, layer.orderIndex);
            }

            foreach (var layer in diffResult.toUpdate)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.guid; });
                myparam.NickName = layer.name;
            }

            Params.OnParametersChanged();

        }

        public void setObjects(IGH_DataAccess DA, List<object> objects, List<SpeckleLayer> structure)
        {
            if (structure == null) return;
            foreach (SpeckleLayer layer in structure)
            {
                var subset = objects.GetRange(layer.startIndex, layer.objectCount);

                if (layer.topology == "")
                    DA.SetDataList(layer.orderIndex, subset);
                else
                {
                    //HIC SVNT DRACONES
                    var tree = new DataTree<object>();
                    var treeTopo = layer.topology.Split(' ');
                    int subsetCount = 0;
                    foreach (var branch in treeTopo)
                    {
                        if (branch != "")
                        {
                            var branchTopo = branch.Split('-')[0].Split(';');
                            var branchIndexes = new List<int>();
                            foreach (var t in branchTopo) branchIndexes.Add(Convert.ToInt32(t));

                            var elCount = Convert.ToInt32(branch.Split('-')[1]);
                            GH_Path myPath = new GH_Path(branchIndexes.ToArray());

                            for (int i = 0; i < elCount; i++)
                                tree.EnsurePath(myPath).Add(subset[subsetCount + i]);
                            subsetCount += elCount;
                        }
                    }
                    DA.SetDataTree(layer.orderIndex, tree);
                }
            }
        }

        #endregion

        #region Variable Parm

        private Param_GenericObject getGhParameter(SpeckleLayer param)
        {
            Param_GenericObject newParam = new Param_GenericObject();
            newParam.Name = (string)param.guid;
            newParam.NickName = (string)param.name;
            newParam.MutableNickName = false;
            newParam.Access = GH_ParamAccess.tree;
            return newParam;
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, Int32 index)
        {
            return null;
        }
        public void VariableParameterMaintenance()
        {
        }

        #endregion

        #region Layer Helpers

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
            foreach (IGH_Param myParam in Params.Output)
            {
                // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
                SpeckleLayer myLayer = new SpeckleLayer(
                    myParam.NickName,
                    myParam.Name /* aka the orignal guid*/, getTopology(myParam),
                    myParam.VolatileDataCount,
                    startIndex,
                    count);

                layers.Add(myLayer);
                startIndex += myParam.VolatileDataCount;
                count++;
            }
            return layers;
        }

        #endregion

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.receiver_2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{9d04ec58-af99-49cd-9629-1b12ca13d102}"); }
        }
    }
}
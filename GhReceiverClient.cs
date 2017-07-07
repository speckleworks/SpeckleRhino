using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleCommon;
using SpeckleGhRhConverter;


using Grasshopper;
using Grasshopper.Kernel.Data;

namespace SpeckleGrasshopper
{
    public class GhReceiverClient : GH_Component, IGH_VariableParameterComponent
    {
        string ApiUrl;
        string Token;
        string StreamId;
        string SerialisedReceiver;
        List<string> SenderGuids;

        SpeckleReceiver Receiver;
        List<SpeckleLayer> Layers;
        List<object> Objects;

        Action ExpireComponentAction;

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
            if (Receiver != null)
                writer.SetString("serialisedReceiver", Receiver.ToString());
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetString("serialisedReceiver", ref SerialisedReceiver);
            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            SenderGuids = new List<string>();

            if (SerialisedReceiver != null)
            {
                Receiver = new SpeckleReceiver(SerialisedReceiver, new GhRhConveter());
                StreamId = Receiver.GetStreamId();
                ApiUrl = Receiver.GetServer();
                Token = Receiver.GetToken();

                RegistermyReceiverEvents();
            } else
            {
                var myForm = new SpecklePopup.MainWindow();

                var some = new System.Windows.Interop.WindowInteropHelper(myForm)
                {
                    Owner = Rhino.RhinoApp.MainWindowHandle()
                };
                myForm.ShowDialog();

                if (myForm.restApi != null && myForm.apitoken != null)
                {
                    ApiUrl = myForm.restApi;
                    Token = myForm.apitoken;
                }
            }

            ExpireComponentAction = () => this.ExpireSolution(true);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (Receiver != null)
                Receiver.Dispose();
            base.RemovedFromDocument(document);
        }

        public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
        {
            if (context == GH_DocumentContext.Close)
            {
                Receiver.Dispose();
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
            Debug.WriteLine("StreamId: " + StreamId + " Read ID: " + inputId);

            if(ApiUrl==null || Token == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to init. No server details.");
                return;
            }

            if (inputId == null && StreamId == null)
            {
                Debug.WriteLine("No streamId to listen to.");
                return;
            }
            else if ((inputId != StreamId) && (inputId != null))
            {
                Debug.WriteLine("changing streamid");
                StreamId = inputId;
                if (Receiver != null) Receiver.Dispose();

                Receiver = new SpeckleReceiver(ApiUrl, Token, StreamId, new GhRhConveter());

                RegistermyReceiverEvents();
                Message = "";
                return;
            }

            SetObjects(DA, Objects, Layers);
        }

        void RegistermyReceiverEvents()
        {
            if (Receiver == null) return;

            Receiver.OnDataMessage +=OnDataMessage;

            Receiver.OnError += OnError;

            Receiver.OnReady += OnReady;

            Receiver.OnMetadata += OnMetadata;

            Receiver.OnData += OnData;

            Receiver.OnHistory += OnHistory;

            Receiver.OnMessage += OnVolatileMessage;

            Receiver.OnBroadcast += OnBroadcast;
        }

        private void OnDataMessage(object source, SpeckleEventArgs e)
        {
            Message = "Update in progress.";
        }

        #region virtual event handlers

        public virtual void OnError(object source, SpeckleEventArgs e)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventInfo);
        }

        public virtual void OnReady(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("[Gh Receiver] Got a ready message. Extend this class and implement custom protocols at ease.");

            Name = NickName = (string)e.Data.name;
            DiffStructure(e.Data.layers);
            Layers = e.Data.layers;
            Objects = e.Data.objects;

            Debug.WriteLine("ready event");
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }

        public virtual void OnMetadata(object source, SpeckleEventArgs e)
        {
            Name = NickName = (string)e.Data.name;
            DiffStructure(e.Data.layers);
            Layers = e.Data.layers;

            Debug.WriteLine("metadata event");
        }

        public virtual void OnData(object source, SpeckleEventArgs e)
        {
            Debug.WriteLine("RECEIVER: Received live update event: " + e.EventInfo);
            Name = NickName = (string)e.Data.name;
            DiffStructure(e.Data.layers);
            Layers = e.Data.layers;
            Objects = e.Data.objects;

            Debug.WriteLine("data event");
            Message = "Loaded update.";
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
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

        public void DiffStructure(List<SpeckleLayer> newLayers)
        {
            dynamic diffResult = SpeckleLayer.DiffLayers(GetLayers(), newLayers);

            foreach (SpeckleLayer layer in diffResult.toRemove)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.Uuid.ToString(); });

                if (myparam != null)
                    Params.UnregisterOutputParameter(myparam);
            }

            foreach (var layer in diffResult.toAdd)
            {
                Param_GenericObject newParam = GetGhParameter(layer);
                Params.RegisterOutputParam(newParam, layer.orderIndex);
            }

            foreach (var layer in diffResult.toUpdate)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.guid; });
                myparam.NickName = layer.name;
            }

            Params.OnParametersChanged();

        }

        public void SetObjects(IGH_DataAccess DA, List<object> objects, List<SpeckleLayer> structure)
        {
            if (structure == null) return;
            foreach (SpeckleLayer layer in structure)
            {
                var subset = objects.GetRange(layer.StartIndex, layer.ObjectCount);

                if (layer.Topology == "")
                    DA.SetDataList(layer.OrderIndex, subset);
                else
                {
                    //HIC SVNT DRACONES
                    var tree = new DataTree<object>();
                    var treeTopo = layer.Topology.Split(' ');
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
                    DA.SetDataTree(layer.OrderIndex, tree);
                }
            }
        }

        #endregion

        #region Variable Parm

        private Param_GenericObject GetGhParameter(SpeckleLayer param)
        {
            Param_GenericObject newParam = new Param_GenericObject()
            {
                Name = param.Uuid.ToString(),
                NickName = param.Name,
                MutableNickName = false,
                Access = GH_ParamAccess.tree
            };
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

        public string GetTopology(IGH_Param param)
        {
            string topology = "";
            foreach (GH_Path mypath in param.VolatileData.Paths)
            {
                topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
            }
            return topology;
        }

        public List<SpeckleLayer> GetLayers()
        {
            List<SpeckleLayer> layers = new List<SpeckleLayer>();
            int startIndex = 0;
            int count = 0;
            foreach (IGH_Param myParam in Params.Output)
            {
                // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
                SpeckleLayer myLayer = new SpeckleLayer(
                    myParam.NickName,
                    Guid.Parse(myParam.Name) /* aka the orignal guid*/, GetTopology(myParam),
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
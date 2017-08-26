using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleCore;
using SpeckleRhinoConverter;
using SpecklePopup;


using Grasshopper;
using Grasshopper.Kernel.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using System.Dynamic;
using System.Windows;
using System.Threading.Tasks;

namespace SpeckleGrasshopper
{
    public class GhReceiverClient : GH_Component, IGH_VariableParameterComponent
    {
        string AuthToken;
        string StreamId;

        SpeckleApiClient myReceiver;
        List<SpeckleLayer> Layers;
        List<SpeckleObject> Objects;

        Action expireComponentAction;

        RhinoConverter Converter;
        
        public GhReceiverClient()
          : base("Data Receiver", "Data Receiver",
              "Receives data from Speckle.",
              "Speckle", "I/O")
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            try
            {
                if (myReceiver != null)
                    using (var ms = new MemoryStream())
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(ms, myReceiver);
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
                    myReceiver = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
                }
            }
            catch { }
            return base.Read(reader);
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            if (myReceiver == null)
            {
                var myForm = new SpecklePopup.MainWindow();

                var some = new System.Windows.Interop.WindowInteropHelper(myForm);
                some.Owner = Rhino.RhinoApp.MainWindowHandle();

                myForm.ShowDialog();

                if (myForm.restApi != null && myForm.apitoken != null)
                {
                    myReceiver = new SpeckleApiClient(myForm.restApi, new RhinoConverter());
                    AuthToken = myForm.apitoken;
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Account selection failed.");
                    return;
                }
            }

            myReceiver.OnReady += (sender, e) =>
            {
                UpdateGlobal();
            };

            myReceiver.OnWsMessage += OnWsMessage;

            myReceiver.OnError += OnError;

            expireComponentAction = () => this.ExpireSolution(true);

            Converter = new RhinoConverter();
        }

        public virtual void OnError(object source, SpeckleEventArgs e)
        {
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventData);
        }

        public virtual void OnWsMessage(object source, SpeckleEventArgs e)
        {
            switch ((string)e.EventObject.args.eventType)
            {
                case "update-global":
                    UpdateGlobal();
                    break;
                case "update-meta":
                    UpdateMeta();
                    break;
                default:
                    CustomMessageHandler((string)e.EventObject.args.eventType, e);
                    break;
            }
        }

        public virtual void UpdateGlobal()
        {
            var getStream = myReceiver.StreamGetAsync(myReceiver.StreamId);
            getStream.Wait();

            NickName = getStream.Result.Stream.Name;
            Layers = getStream.Result.Stream.Layers.ToList();
            Objects = getStream.Result.Stream.Objects.ToList();

            UpdateOutputStructure();
            Rhino.RhinoApp.MainApplicationWindow.Invoke(expireComponentAction);
        }

        public virtual void UpdateMeta()
        {
            var getName = myReceiver.StreamGetNameAsync(StreamId);
            var getLayers = myReceiver.StreamsGetLayersAsync(StreamId);

            Task.WhenAll(new Task[] { getName, getLayers }).Wait();

            NickName = getName.Result.Name;
            Layers = getLayers.Result.Layers.ToList() ;
            UpdateOutputStructure();
        }

        public virtual void CustomMessageHandler(string eventType, SpeckleEventArgs e)
        {
            Debug.WriteLine("Received {0} type message.", eventType);
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
            pManager.AddTextParameter("ID", "ID", "The stream's short id.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string inputId = null;
            DA.GetData(0, ref inputId);

            if (inputId == null && StreamId == null) return;

            else if ((inputId != StreamId) && (inputId != null))
            {
                StreamId = inputId;
                myReceiver.IntializeReceiver(StreamId, AuthToken);
                return;
            }

            if (!myReceiver.IsConnected) return;

            SetObjects(DA);
        }

        public void UpdateOutputStructure()
        {
            List<SpeckleLayer> toRemove, toAdd, toUpdate;
            toRemove = new List<SpeckleLayer>(); toAdd = new List<SpeckleLayer>(); toUpdate = new List<SpeckleLayer>();

            SpeckleLayer.DiffLayerLists(GetLayers(), Layers, ref toRemove, ref toAdd, ref toUpdate);

            foreach (SpeckleLayer layer in toRemove)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.Guid; });

                if (myparam != null)
                    Params.UnregisterOutputParameter(myparam);
            }

            foreach (var layer in toAdd)
            {
                Param_GenericObject newParam = getGhParameter(layer);
                Params.RegisterOutputParam(newParam, (int)layer.OrderIndex);
            }

            foreach (var layer in toUpdate)
            {
                var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.Guid; });
                myparam.NickName = layer.Name;
            }
            Params.OnParametersChanged();
        }

        public void SetObjects(IGH_DataAccess DA)
        {
            if (Layers == null) return;

            RhinoConverter converter = new RhinoConverter();

            var convObjs = converter.ToNative(Objects).ToList();

            foreach (SpeckleLayer layer in Layers)
            {
                var subset = convObjs.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);

                if (layer.Topology == "")
                    DA.SetDataList((int)layer.OrderIndex, subset);
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
                    DA.SetDataTree((int)layer.OrderIndex, tree);
                }
            }
        }

        #region Variable Parm

        private Param_GenericObject getGhParameter(SpeckleLayer param)
        {
            Param_GenericObject newParam = new Param_GenericObject();
            newParam.Name = (string)param.Guid;
            newParam.NickName = (string)param.Name;
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

        public string GetParamTopology(IGH_Param param)
        {
            string topology = "";
            foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
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
                    myParam.Name /* aka the orignal guid*/, GetParamTopology(myParam),
                    myParam.VolatileDataCount,
                    startIndex,
                    count);

                layers.Add(myLayer);
                startIndex += myParam.VolatileDataCount;
                count++;
            }
            return layers;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.receiver_2;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{e35c72a5-9e1c-4d79-8879-a9d6db8006fb}"); }
        }
    }
}
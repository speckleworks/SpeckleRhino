using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;
using System.Windows;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI.Base;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Dynamic;
using System.Windows.Forms;
using Grasshopper.Kernel.Components;
using Grasshopper.Kernel.Types;

namespace SpeckleGrasshopper
{

    public class DefinitionController : GhSenderClient
    {

        public OrderedDictionary JobQueue;
        public string CurrentJobClient = "none";
        public bool solutionPrepared = false;
        IGH_DataAccess _DA = null;

        public DefinitionController()
        {
            this.Category = "Speckle";
            this.SubCategory = "Extensions";
            this.Name = "Definition Controller";
            this.NickName = "Definition Controllerr";
            this.Description = "Another example of an extended Sender component - this one should enable controlling the gh defintion.";

            JobQueue = new OrderedDictionary();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            GH_DocumentObject.Menu_AppendItem(menu, @"Save current configuration as default", (sender, e) =>
            {
                mySender.StreamCustomUpdate(this.NickName, GetLayers(), GetData());
            });
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _DA = DA;
            this.Message = "JobQueue: " + JobQueue.Count;
            if (mySender == null) return;

            DA.SetData(0, Log);
            DA.SetData(1, mySender.StreamId);

            if (!mySender.IsConnected) return;
            if (JobQueue.Count == 0) return;

            if (!solutionPrepared)
            {
                System.Collections.DictionaryEntry t = JobQueue.Cast<DictionaryEntry>().ElementAt(0);
                CurrentJobClient = (string)t.Key;
                PrepareSolution((IEnumerable)t.Value);
                solutionPrepared = true;
                return;
            }
            else
            {
                solutionPrepared = false;
                mySender.StreamCreateAndPopulate(this.NickName, GetLayers(), GetData(), (streamId) =>
                {
                    List<SpeckleInputParam> inputControllers = null;
                    List<SpeckleOutputParam> outputControllers = null;
                    GetDefinitionIO(ref inputControllers, ref outputControllers);

                    Dictionary<string, object> args = new Dictionary<string, object>();
                    args["eventType"] = "computation-result";
                    args["streamId"] = streamId;
                    args["outputRef"] = outputControllers;

                    mySender.SendMessage(CurrentJobClient, args);
                });

                JobQueue.RemoveAt(0);
                this.Message = "JobQueue: " + JobQueue.Count;
                if (JobQueue.Count != 0)
                    Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
            }
        }

        public void PrepareSolution(IEnumerable args)
        {
            var x = args;

            foreach (dynamic param in args)
            {
                IGH_DocumentObject controller = null;
                try
                {
                    controller = Document.Objects.First(doc => doc.InstanceGuid.ToString() == param.guid);
                }
                catch { }

                if (controller != null)
                    switch (param.type)
                    {
                        case "TextPanel":
                            GH_Panel panel = controller as GH_Panel;
                            panel.UserText = (string)param.value;
                            panel.ExpireSolution(false);
                            break;
                        case "Slider":
                            GH_NumberSlider slider = controller as GH_NumberSlider;
                            slider.SetSliderValue(decimal.Parse(param.value.ToString()));
                            break;
                        case "Point":
                            PointController p = controller as PointController;
                            var xxxx = p;
                            p.setParam((double)param.value.X, (double)param.value.Y, (double)param.value.Z);
                            break;
                        case "Toggle":
                            break;
                        default:
                            break;
                    }
                else
                {
                    if(param.type == "MaterialTable")
                    {
                        var MatOut = (GH_Panel) Document.Objects.FirstOrDefault(doc => doc.NickName == "MAT_OUT");
                        if(MatOut!= null)
                        {
                            string mats = "";
                            foreach ( var layer in param.layers )
                            {
                                try { 
                                mats += layer.name + ":" + layer.material + ":" + layer.price + "\n";
                                }
                                catch { }
                            }

                            MatOut.UserText = mats;
                            MatOut.ExpireSolution(true);
                        }
                    }
                }
            }
            // TODO: Expire component
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }

        public override void OnWsMessage(object source, SpeckleEventArgs e)
        {
            base.OnWsMessage(source, e);
            switch (e.EventObject.args.eventType)
            {
                case "get-defintion-io":

                    List<SpeckleInputParam> inputControllers = null;
                    List<SpeckleOutputParam> outputControllers = null;
                    GetDefinitionIO(ref inputControllers, ref outputControllers);

                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["eventType"] = "get-def-io-response";
                    message["controllers"] = inputControllers;
                    message["outputs"] = outputControllers;

                    mySender.SendMessage(e.EventObject.senderId, message);
                    break;
                case "compute-request":
                    var key = (string)e.EventObject.senderId;                   
                    if (JobQueue.Contains((string)e.EventObject.senderId))
                        JobQueue[key] = e.EventObject.args.requestParameters;
                    else
                        JobQueue.Add(key, e.EventObject.args.requestParameters);

                    Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
                    break;
                default:
                    Log += DateTime.Now.ToString("dd:HH:mm:ss") + " Defaulted, could not parse event. \n";
                    break;
            }
        }

        private void GetDefinitionIO(ref List<SpeckleInputParam> inputControllers, ref List<SpeckleOutputParam> outputControllers)
        {
            inputControllers = new List<SpeckleInputParam>();
            outputControllers = new List<SpeckleOutputParam>();

            foreach (var comp in Document.Objects)
            {
                var slider = comp as GH_NumberSlider;
                if (slider != null)
                {
                    if (slider.NickName.Contains("SPK_IN"))
                    {
                        var n = new SpeckleNumberInput();
                        n.Min = (double)slider.Slider.Minimum;
                        n.Max = (double)slider.Slider.Maximum;
                        n.Value = (double)slider.Slider.Value;
                        n.Step = getSliderStep(slider.Slider);
                        n.OrderIndex = Convert.ToInt32(slider.NickName.Split(':')[1]);
                        n.Name = slider.NickName.Split(':')[2];
                        n.Type = "Slider";
                        n.Guid = slider.InstanceGuid.ToString();

                        inputControllers.Add(n);
                    }
                }

                var ptc = comp as PointController;

                if (ptc != null)
                {
                    inputControllers.Add(ptc.getParam());
                }

                var panel = comp as GH_Panel;
                if (panel != null)
                {
                    if (panel.NickName.Contains("SPK_IN"))
                    {
                        var p = new SpeckleTextInput();
                        p.Value = panel.UserText;
                        p.OrderIndex = Convert.ToInt32(panel.NickName.Split(':')[1]);
                        p.Name = panel.NickName.Split(':')[2];
                        p.Type = "TextPanel";
                        p.Guid = panel.InstanceGuid.ToString();

                        inputControllers.Add(p);
                    }
                    else if (panel.NickName.Contains("SPK_OUT_MAIN"))
                    {
                        var p = new SpeckleOutputParam();
                        p.Value = getPanelValue(panel);
                        p.OrderIndex = Convert.ToInt32(panel.NickName.Split(':')[1]);
                        p.Unit = panel.NickName.Split(':')[2];
                        p.Name = panel.NickName.Split(':')[3];
                        p.IsPrincipal = true;
                        outputControllers.Add(p);

                    }
                    else if (panel.NickName.Contains("SPK_OUT_SEC"))
                    {
                        var p = new SpeckleOutputParam();
                        p.Value = getPanelValue(panel);
                        p.OrderIndex = Convert.ToInt32(panel.NickName.Split(':')[1]);
                        p.Unit = panel.NickName.Split(':')[2];
                        p.Name = panel.NickName.Split(':')[3];
                        p.IsPrincipal = false;
                        outputControllers.Add(p);
                    }
                    else if (panel.NickName == "SPK_MAT")
                    {
                        var p = new SpeckleOutputParam()
                        {
                            Value = getPanelValue(panel),
                            Type = "MaterialSheet"
                        };
                        outputControllers.Add(p);
                    }
                }
            }
        }

        private string getPanelValue(GH_Panel panel)
        {
            string value = "";
            foreach (var x in panel.VolatileData.AllData(true))
            {
                value += x.ToString();
            }
            return value;
        }

        private dynamic getSliderStep(GH_SliderBase gH_NumberSlider)
        {
            switch (gH_NumberSlider.Type)
            {
                case GH_SliderAccuracy.Float:
                    return 1 / Math.Pow(10, gH_NumberSlider.DecimalPlaces);
                case GH_SliderAccuracy.Integer:
                    return 1;
                case GH_SliderAccuracy.Even:
                    return 2;
                case GH_SliderAccuracy.Odd:
                    return 2;
                default:
                    return 1 / 10e2;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{F4BCAF24-9F27-4DDE-8879-66075CA67719}"); }
        }
    }

    public class PointController : GH_Component
    {


        public Point3d? OutputPoint = null;
        public SpecklePoint StreamedPoint = null;

        public PointController() : base("PointController", "PCR", "PCR", "Speckle", "Exetensions")
        {
            //this.Category = "Speckle";
            //this.SubCategory = "Extensions";
            //this.Name = "Point Controller";
            //this.NickName = "PCR";
            //this.Description = "Tells the viewer this is a controllable point.";
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {

            pManager.AddGenericParameter("Base Point", "P", "Base point.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Bounds", "B", "Bounds", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Streamed Point", "P", "Streamed point.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (OutputPoint != null)
            {
                DA.SetData(0, OutputPoint);
            }
            else
            {
                try
                {
                    object oldpt = null;
                    DA.GetData(0, ref oldpt);
                    DA.SetData(0, oldpt);
                }
                catch { }
            }

        }

        public void setParam(double X, double Y, double Z)
        {
            OutputPoint = new Point3d(X, Y, Z);

            this.ExpireSolution(false);
        }

        public SpecklePointInput getParam()
        {
            List<object> data = new List<dynamic>();
            foreach (IGH_Param myParam in Params.Input)
            {
                foreach (object o in myParam.VolatileData.AllData(false))
                    data.Add(o);
            }

            Point3d bp = ((GH_Point)data[0]).Value;
            Box bounds = ((GH_Box)data[1]).Value;

            var inp = new SpecklePointInput()
            {
                X = bp.X,
                Y = bp.Y,
                Z = bp.Z,
                MinX = bounds.X.Min,
                MinY = bounds.Y.Min,
                MinZ = bounds.Z.Min,
                MaxX = bounds.X.Max,
                MaxY = bounds.Y.Max,
                MaxZ = bounds.Z.Max,
                Name = this.NickName,
                Guid = this.InstanceGuid.ToString(),
                OrderIndex = 0,
                Type = "Point"
            };

            return inp;
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{F257C92A-6009-40E8-9ECB-E754EECDAC93}"); }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.GenericIconXS;
            }
        }
    }

    public class ExtenededReceiver : GhReceiverClient
    {
        public ExtenededReceiver()
        {
            this.Category = "Speckle";
            this.SubCategory = "Extensions";
            this.Name = "Extended Receiver";
            this.NickName = "Extended Receiver";
            this.Description = "Example of how you can extend a component. It will pop a messagebox on all ws messages - not very useful!";
        }


        public override void OnWsMessage(object source, SpeckleCore.SpeckleEventArgs e)
        {
            base.OnWsMessage(source, e);
            System.Windows.MessageBox.Show(String.Format("Wow, got a custom message: {0}!", e.EventObject.args.eventType));
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
            get { return new Guid("{36728fa7-ab47-40a1-b47c-c072da646eb7}"); }
        }
    }



}
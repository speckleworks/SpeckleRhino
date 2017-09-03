using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;
using System.Windows;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI.Base;

namespace SpeckleGrasshopper
{

    public class DefinitionController : GhSenderClient
    {
        public DefinitionController()
        {
            this.Category = "Speckle";
            this.SubCategory = "Extensions";
            this.Name = "Definition Controller";
            this.NickName = "Definition Controllerr";
            this.Description = "Another example of an extended Sender component - this one should enable controlling the gh defintion.";
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            base.SolveInstance(DA);
        }

        public override void OnWsMessage(object source, SpeckleEventArgs e)
        {
            base.OnWsMessage(source, e);
            switch (e.EventObject.args.eventType)
            {
                case "get-defintion-io":

                    List<SpeckleInputParam> inputControllers = null;
                    GetDefinitionIO(ref inputControllers);

                    Dictionary<string, object> message = new Dictionary<string, object>();
                    message["eventType"] = "get-def-io-response";
                    message["controllers"] = inputControllers;

                    mySender.SendMessage(e.EventObject.senderId, message);
                    break;
                case "compute-request":
                    break;
                default:
                    Log += DateTime.Now.ToString("dd:HH:mm:ss") + " Defaulted, could not parse event. \n";
                    break;
            }

        }

        private void GetDefinitionIO(ref List<SpeckleInputParam> inputControllers)
        {
            List<GH_NumberSlider> sliders = new List<GH_NumberSlider>();
            List<GH_Panel> inPanels = new List<GH_Panel>();
            List<GH_Panel> outPanels = new List<GH_Panel>();

            inputControllers = new List<SpeckleInputParam>();

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

                        inputControllers.Add(n);
                    }
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

                        inputControllers.Add(p);
                    }
                    else if (panel.NickName.Contains("SPK_OUT"))
                    {
                        outPanels.Add(panel);
                    }
                }
            }
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
            MessageBox.Show(String.Format("Wow, got a custom message: {0}!", e.EventObject.args.eventType));
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
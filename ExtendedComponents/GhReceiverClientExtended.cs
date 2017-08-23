using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;

namespace SpeckleGrasshopper
{
    public class ExtenededReceiver : GhReceiverClient
    {
        public ExtenededReceiver()
        {
            this.Category = "Speckle";
            this.SubCategory = "Extensions";
            this.Name = "Extended Receiver";
            this.NickName = "Extended Receiver";
            this.Description = "Example of how you can extend a component.";
        }


        public override void CustomMessageHandler(string eventType, SpeckleCore.SpeckleEventArgs e)
        {
            base.CustomMessageHandler(eventType, e);
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
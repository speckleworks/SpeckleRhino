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
            get { return new Guid("{05e864ac-ed69-4f08-a812-d78d2f2d7a6a}"); }
        }
    }

    public class ExtendedSender : GhSenderClient
    {

    }

}
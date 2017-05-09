using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCommon;

namespace SpeckleGrasshopper
{
    public class ExtenededReceiver : GhReceiverClient
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ExtenededReceiver()
        {
            this.Category = "Speckle";
            this.SubCategory = "Debug";
            this.Name = "Extended Receiver";
            this.NickName = "Extended Receiver";
        }

        public override void OnVolatileMessage(object source, SpeckleEventArgs e)
        {
            base.OnVolatileMessage(source, e);
            System.Diagnostics.Debug.WriteLine("I've Inherited this method and now i can do whatever I want here, like my own extra protocol.");
            System.Diagnostics.Debug.WriteLine((string) e.Data.args);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{05e864ac-ed69-4f08-a812-d78d2f2d7a6a}"); }
        }
    }
}
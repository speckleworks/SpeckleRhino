using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Runtime;
using Grasshopper.Kernel.Types;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace SpeckleGrasshopper
{
    public class GetUserDataComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GetUserDataComponent()
          : base("Get User Data", "GUD",
              "Extracts the user dictionary attached to an object, if any.",
              "Speckle", "User Data Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object to expand user dictionary of.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("User Dictionary", "D", "User Dictionary", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj2 = null;
            DA.GetData(0, ref obj2);
            if (obj2 == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to get object"); return; }

            var theValue = obj2.GetType().GetProperty("Value").GetValue(obj2, null);
            GeometryBase geometry = null;

            geometry = theValue as GeometryBase;


            if (geometry == null)
            {
                DA.SetData(0, null);
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input object not supported.");
                return;
            }

            if (geometry.UserDictionary == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Object has no user dictionary.");
                DA.SetData(0, null);
                return;
            }

            DA.SetData(0, geometry.UserDictionary);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.GetUserData;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{48f08adc-0fdd-41de-a038-a610e310cad9}"); }
        }
    }
}
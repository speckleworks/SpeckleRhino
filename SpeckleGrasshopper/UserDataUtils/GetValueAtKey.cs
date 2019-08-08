//extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Newtonsoft.Json;
using Rhino.Collections;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using System.IO;
using Grasshopper.Kernel.Parameters;

namespace SpeckleGrasshopper
{
    public class GetValueAtKey : GH_Component
    {
        string serialisedUDs;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GetValueAtKey()
          : base("Gets nested value", "GNV",
              "Gets a value from a dictionary by string of concatenated keys. \n For example, 'prop.subprop.subsubprop'.",
              "Speckle", "User Data Utils")
        {
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("User Data", "D", "User Dictionary.", GH_ParamAccess.item);
            pManager.AddTextParameter("Path", "P", "Path of desired property, separated by dots.\nExample:'turtle.smallerTurtle.microTurtle'", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "O", "Output value.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object o1 = null; DA.GetData(0, ref o1);

            ArchivableDictionary dict = ((GH_ObjectWrapper)o1).Value as ArchivableDictionary;
            if (dict == null)
                throw new Exception("No dictionary provided.");

            string path = null; DA.GetData(1, ref path);
            if (path == null)
                throw new Exception("No path provided.");

            object target = null;
            ArchivableDictionary temp = dict;

            var keys = path.Split('.');
            for(int i = 0; i<keys.Length; i++)
            {
                if (i == keys.Length - 1) target = temp[keys[i]];
                else temp = temp[keys[i]] as ArchivableDictionary;
            }

            DA.SetData(0, new GH_ObjectWrapper(target));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.json;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{f954660c-2c7d-4c2e-ab29-582e41d6c8da}"); }
        }
    }
}

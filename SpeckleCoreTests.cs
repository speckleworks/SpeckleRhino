using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleGhRhConverter;


using Grasshopper;
using Grasshopper.Kernel.Data;

using Newtonsoft.Json;
using System.Dynamic;

using SpeckleCore;
using System.Threading.Tasks;

namespace SpeckleGrasshopper
{
    public class SpeckleCoreTest : GH_Component
    {
        SpeckleApiClient myClient;
        Action expireComponentAction;
        string loginResponse = "";
        bool loggedIn = false;

        public SpeckleCoreTest()
          : base("SpeckleCoreTester", "SpeckleCoreTester",
              "SpeckleCORETESTER",
              "Speckle", "Debug")
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{36db0da4-f432-43c2-a954-7d1cde963160}"); }
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            expireComponentAction = () => this.ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Objects to convert.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Conversion Result String", "S", "Conversion result string.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Conversion Result", "R", "Conversion result object.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
           

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
    }
}

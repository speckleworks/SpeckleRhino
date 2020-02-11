//extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Newtonsoft.Json;
using Rhino.Collections;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using SpeckleCore;
using System.IO;
using Grasshopper.Kernel.Parameters;

namespace SpeckleGrasshopper
{
    public class DiffingStreamsDelta : GH_Component
    {
        string serialisedUDs;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public DiffingStreamsDelta()
          : base("Diffing between two streams (delta)", "DELTADIFF",
              "Operates a diffing between two different streams and returns the delta as response",
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
            //pManager.AddGenericParameter("Client", "C", "Client.", GH_ParamAccess.item);
            pManager.AddTextParameter("StreamID", "A", "StreamID.", GH_ParamAccess.item);
            pManager.AddTextParameter("OtherStreamID", "B", "OtherStreamID.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("inA", "Deleted", "Objects only in A.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("inB", "Created", "Objects only in B.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Common", "Common", "Objects common to A and B.", GH_ParamAccess.list);
        }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      SpeckleCore.SpeckleApiClient myClient = null;

      Account account = null;
      try
      {
        account = LocalContext.GetDefaultAccount();
        var RestApi = account.RestApi;
        var myToken = account.Token;
        myClient = new SpeckleApiClient(account.RestApi);
      }
      catch (Exception err)
      {
      }

      string StreamID = null;
      string OtherStreamID = null;

      DA.GetData(0, ref StreamID);
      DA.GetData(1, ref OtherStreamID);

      if (StreamID != null && OtherStreamID != null) {
        var testDiff = myClient.StreamDeltaDiffAsync(StreamID, OtherStreamID).Result.Delta;

        string[] deleted = testDiff.Deleted.ToArray();
        List<object> deletedObjs = SpeckleCore.Converter.Deserialise(myClient.ObjectGetBulkAsync(deleted, "").Result.Resources);
        DA.SetDataList(0, deletedObjs);

        string[] created = testDiff.Created.ToArray();
        List<object> createdObjs = SpeckleCore.Converter.Deserialise(myClient.ObjectGetBulkAsync(created, "").Result.Resources);
        DA.SetDataList(1, createdObjs);

        string[] common = testDiff.Common.ToArray();
        List<object> objsInCommon = SpeckleCore.Converter.Deserialise(myClient.ObjectGetBulkAsync(common, "").Result.Resources);
        DA.SetDataList(2, objsInCommon);
      }

    }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.Convert;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{b5bbc229-9e55-4540-984d-ddb8ab8357cb}"); }
        }
    }
}

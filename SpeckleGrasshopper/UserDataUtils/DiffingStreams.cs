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
    public class DiffingStreams : GH_Component
    {
        string serialisedUDs;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public DiffingStreams()
          : base("Diffing between two streams", "DIFF",
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
            pManager.AddGeometryParameter("inA", "inA", "Objects only in A.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("inB", "inB", "Objects only in B.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Common", "C", "Objects common to A and B.", GH_ParamAccess.list);
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
        var testDiff = myClient.StreamDiffAsync(StreamID, OtherStreamID).Result.Objects;

        string[] inA = testDiff.InA.ToArray();
        List<object> objsInA = SpeckleCore.Converter.Deserialise(myClient.ObjectGetBulkAsync(inA, "").Result.Resources);
        DA.SetDataList(0, objsInA);

        
        string[] inB = testDiff.InB.ToArray();
        List<object> objsInB = SpeckleCore.Converter.Deserialise(myClient.ObjectGetBulkAsync(inB, "").Result.Resources);
        DA.SetDataList(1, objsInB);

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
            get { return new Guid("{c3295896-927b-4900-9722-6f0e8dd87b73}"); }
        }
    }
}

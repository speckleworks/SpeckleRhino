//extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Newtonsoft.Json;
using System.Windows.Forms;
using SpeckleCore;
using System.IO;
using Grasshopper.Kernel.Parameters;
using System.Reflection;

namespace SpeckleGrasshopper
{
  public class StreamApplyDelta : GH_Component
  {
    
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public StreamApplyDelta()
      : base("Applies a delta to a stream", "DELTADIFF",
          "Applies a delta to a stream and returns a response. Make sure that the delta's revisionA matches with the input stream!",
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
      pManager.AddTextParameter("StreamID", "StreamID", "StreamID.", GH_ParamAccess.item);
      pManager.AddGenericParameter("Delta", "Delta", "Delta to apply.", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Success", "Success", "True if the delta was applied onto the input stream.", GH_ParamAccess.item);
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
        myClient.AuthToken = myToken;
      }
      catch (Exception err)
      {
      }

      string StreamID = null;
      SpeckleCore.SpeckleDelta Delta = null;

      DA.GetData(0, ref StreamID);
      DA.GetData(1, ref Delta);
      
      if (StreamID != null && Delta != null)
      {
        // TODO add exception for streams != Delta.RevisionA.id
        var testApplyDelta = myClient.StreamApplyDeltaAsync(StreamID, Delta).Result;

        var settings = new Newtonsoft.Json.JsonSerializerSettings()
        {
          ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
          Formatting = Newtonsoft.Json.Formatting.Indented
        };

        DA.SetData(0, testApplyDelta.Success);
        Message = testApplyDelta.Message;

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
      get { return new Guid("{a314b496-4426-4f92-94b0-8176fe6577bc}"); }
    }
  }
}

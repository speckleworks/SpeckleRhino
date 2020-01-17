using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using SpeckleCore;
using System.Linq;

namespace SpeckleGrasshopper.UserDataUtils
{
  public class SetUserDataSpeckleObjectComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the SetUserDataSpeckleObjectComponent class.
    /// </summary>
    public SetUserDataSpeckleObjectComponent()
      : base("Set User Data Speckle Object", "SUDSO",
          "Sets user data to a Speckle Object.",
          "Speckle", "User Data Utils")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleObjectParameter(), "Speckle Object", "SO", "Speckle object to add data to", GH_ParamAccess.item);
      pManager.AddGenericParameter("User Data", "D", "Data to attach.", GH_ParamAccess.item);
      pManager[1].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleObjectParameter(), "Speckle Object", "SO", "Speckle object to add data to", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      GH_SpeckleObject GHSpeckleObject = null;
      if (!DA.GetData(0, ref GHSpeckleObject))
        return;

      dynamic dictObject = null;
      DA.GetData(1, ref dictObject);

      var copy = GHSpeckleObject.Duplicate() as GH_SpeckleObject;
      var speckleObject = copy.Value;
      
      try
      {
        var dict = ((GH_ObjectWrapper)dictObject).Value as ArchivableDictionary;
        speckleObject.Properties.Add(Params.Input[1].NickName, dict);
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not an Archivable Dictionary, please provide a dictionary");
      }

      DA.SetData(0, speckleObject);
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
      get { return new Guid("dfb65375-5423-4688-845b-0fd8efe7c6af"); }
    }
  }
}

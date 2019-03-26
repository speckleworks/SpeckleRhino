using System;
using System.Collections.Generic;
using SpeckleGrasshopper.Properties;

using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Reflection;
using SpeckleCore;
using System.Windows.Forms;
using System.Collections;

namespace SpeckleGrasshopper
{
  public class CreateSpeckleKitObject : GH_Component
  {
    public CreateSpeckleKitObject( )
      : base( "Create a SpeckleKit Object", "CSKO",
        "Creates a SpeckleKit objects.",
        "Speckle", "SpeckleKits" )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("SpeckleObjectType", "T", "Speckle object type you want to create.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.Register_GenericParam("Result", "R", "Resulting object", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string inputType = "";
      if (!DA.GetData("SpeckleObjectType", ref inputType))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No type specified.");
        return;
      }
      
      // Try to find type in SpeckleInitializer
      Type objType = SpeckleCore.SpeckleInitializer.GetTypes().FirstOrDefault(t => t.Name == inputType);

      if (objType == null)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find SpeckleObject of type " + inputType + ".");
        return;
      }

      var newObject = Activator.CreateInstance(objType);
      DA.SetData("Result", newObject);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.NewObject;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("ff2c4c7f-fe12-4e4c-badb-3fb5408588c7"); }
    }
  }
}

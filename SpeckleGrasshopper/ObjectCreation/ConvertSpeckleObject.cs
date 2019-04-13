using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Reflection;
using SpeckleCore;
using System.Windows.Forms;
using System.Collections;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper
{
  public class ConvertSpeckleObject : GH_Component
  {
    public ConvertSpeckleObject()
      : base("Converts a Speckle Object", "CVSO",
        "Converts a Speckle objects into another.",
        "Speckle", "SpeckleKits")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("SpeckleObjectType", "T", "Speckle object type you want to convert to.", GH_ParamAccess.item);
      pManager.AddGenericParameter("SpeckleObject", "O", "Speckle object you want to convert.", GH_ParamAccess.item);
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

      SpeckleObject inputObject = null;
      if (!DA.GetData("SpeckleObject", ref inputObject))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No input object.");
        return;
      }

      // Try to find type in SpeckleInitializer
      Type objType = SpeckleCore.SpeckleInitializer.GetTypes().FirstOrDefault(t => t.Name == inputType);

      if (objType == null)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find SpeckleObject of type " + inputType + ".");
        return;
      }

      SpeckleObject convertedObject = (SpeckleObject)Activator.CreateInstance(objType);

      // Check to see if one is a subclass of another
      if (!(inputObject.Type.Contains(convertedObject.Type)) && !(convertedObject.Type.Contains(inputObject.Type)))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "SpeckleObject not convertible to type.");
        return;
      }

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = inputObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(inputObject));
      }

      convertedObject.GenerateHash();
      DA.SetData("Result", convertedObject);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.Convert;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("777908f5-c789-4acc-a9a7-3e89ade1acd5"); }
    }
  }
}

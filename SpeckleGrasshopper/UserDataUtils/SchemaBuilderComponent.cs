using System;
using System.Collections.Generic;
using SpeckleCore;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System.Windows.Forms;
using SpeckleCoreGeometryClasses;

namespace SpeckleGrasshopper.UserDataUtils
{
  public class SchemaBuilderComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the SchemaBuilderComponent class.
    /// </summary>
    /// 
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      var foundtypes = SpeckleCore.SpeckleInitializer.GetTypes();
      foreach(Type type in foundtypes)
      {
        GH_DocumentObject.Menu_AppendItem(menu, type.ToString(), (item, e) => 
        {
          InitInputs(type);
        });
      }
    }

    void InitInputs(Type myType)
    {
      Console.WriteLine(myType.ToString());
      this.Message = myType.Name;

      System.Reflection.PropertyInfo[] propInfo = myType.GetProperties();
      for (int i = 0; i < propInfo.Length; i++)
      {
        // get property name and value
        Type propType = propInfo[i].PropertyType;
        Type baseType = propType.BaseType;

        string propName = propInfo[i].Name;
        object propValue = propInfo.GetValue(i);

        // Create new param based on property name
        Param_GenericObject newParam = new Param_GenericObject();
        newParam.Name = propType.BaseType.Name.ToString();
        newParam.NickName = propType.BaseType.Name.ToString();
        newParam.MutableNickName = false;

        // check if input needs to be a list or item access
        bool genericList = IsGenericList(propType);
        if(genericList == true) {
          newParam.Access = GH_ParamAccess.list;
        }
        else
        {
          newParam.Access = GH_ParamAccess.item;
        }


        if (baseType is SpeckleObject)
        {
          Params.RegisterInputParam(newParam);
        }
        

      }
      this.ExpireSolution(true);


      //SpeckleCoreGeometryClasses.SpecklePoint mySpecklePoint = new SpecklePoint()
    }

    public bool IsGenericList(Type myType)
    {
        return (myType.IsGenericType && (myType.GetGenericTypeDefinition() == typeof(List<>)));
    }

    public SchemaBuilderComponent()
      : base("Schema Builder Component", "SBC",
              "Builds Speckle Types through reflecting upon SpeckleCore and SpeckleKits.",
              "Speckle", "User Data Utils")
    {

    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {

    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
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

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("970d1754-b192-405f-a78b-98afb74ee6ca"); }
    }
  }
}

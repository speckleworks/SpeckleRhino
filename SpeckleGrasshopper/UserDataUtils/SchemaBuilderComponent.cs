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
    public bool showAdditionalProps = false;
    public Type selectedType = null;
    /// <summary>
    /// Initializes a new instance of the SchemaBuilderComponent class.
    /// </summary>
    /// 
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      GH_DocumentObject.Menu_AppendItem(menu, "Toggle Additional Properties (Status: " + showAdditionalProps + ")", (item, e) =>
      {
        showAdditionalProps = !showAdditionalProps;

        if(selectedType is null)
        {

        }
        else
        {
          DeleteInputs();
          InitInputs(selectedType, showAdditionalProps);
        }
      });
      

      GH_DocumentObject.Menu_AppendSeparator( menu );
      var foundtypes = SpeckleCore.SpeckleInitializer.GetTypes();


      foreach (Type type in foundtypes)
      {
        GH_DocumentObject.Menu_AppendItem(menu, type.ToString(), (item, e) => 
        {
          selectedType = type;
          DeleteInputs();
          InitInputs(type, showAdditionalProps);
          
        });
      }


    }


    // Delete inputs when object type is updated by the user
    void DeleteInputs()
    {
      List<IGH_Param> mycurrentParams = this.Params.Input;
      for (int i = mycurrentParams.Count - 1; i >= 0; i--)
      {
        Params.UnregisterInputParameter(mycurrentParams[i]);
      }
     this.ExpireSolution(true);
    }



    void InitInputs(Type myType, bool showAdditionalProps)
    {
      Console.WriteLine(myType.ToString());
      this.Message = myType.Name;

      System.Reflection.PropertyInfo[] propInfo = myType.GetProperties();


      List<Param_GenericObject> inputParams = new List<Param_GenericObject>();
      for (int i = 0; i < propInfo.Length; i++)
      {
        // get property name and value
        Type propType = propInfo[i].PropertyType;
        Type baseType = propType.BaseType;

        string propName = propInfo[i].Name;
        object propValue = propInfo.GetValue(i);

        // Create new param based on property name
        Param_GenericObject newInputParam = new Param_GenericObject();
        newInputParam.Name = propName;
        newInputParam.NickName = propName;
        newInputParam.MutableNickName = false;

        // check if input needs to be a list or item access
        bool genericListBool = IsGenericList(propType);
        if(genericListBool == true) {
          newInputParam.Access = GH_ParamAccess.list;
        }
        else
        {
          newInputParam.Access = GH_ParamAccess.item;
        }

        if (showAdditionalProps == false)
        {
          if (baseType.Name == "SpeckleObject")
          {
            inputParams.Add(newInputParam);
            Params.RegisterInputParam(newInputParam);
            //void InitOutput(Type myType, )

          }
        }
        else
        {
          inputParams.Add(newInputParam);
          Params.RegisterInputParam(newInputParam);
        }
        

      }

      DeleteOutput();
      InitOutput(myType, inputParams);

      


    }
    void InitOutput(Type myType, List<Param_GenericObject> myInputParams)
    {
      var outputObject = Activator.CreateInstance(myType);
      // Create new param for output
      Param_GenericObject newOutputParam = new Param_GenericObject();
      newOutputParam.Name = myType.Name;
      newOutputParam.NickName = myType.Name;
      Params.RegisterOutputParam(newOutputParam);
      this.ExpireSolution(true);
    }

    void DeleteOutput()
    {
      List<IGH_Param> mycurrentParams = this.Params.Output;
      for (int i = mycurrentParams.Count - 1; i >= 0; i--)
      {
        Params.UnregisterOutputParameter(mycurrentParams[i]);
      }
      this.ExpireSolution(true);
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

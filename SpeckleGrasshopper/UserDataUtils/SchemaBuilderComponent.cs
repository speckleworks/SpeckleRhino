using System;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;

namespace SpeckleGrasshopper.UserDataUtils
{
  public class SchemaBuilderComponent : GH_Component
  {

    /// <summary>
    /// Initializes a new instance of the SchemaBuilderComponent class.
    /// </summary>
    // global variables
    public bool showAdditionalProps = false;
    public Type selectedType = null;

    //


    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);


      // toggle optional properties
      GH_DocumentObject.Menu_AppendItem(menu, "Toggle Optional Properties (Status: " + showAdditionalProps + ")", (item, e) =>
      {
        showAdditionalProps = !showAdditionalProps;

        if (selectedType is null)
        {
          //graceful exit: don't show any properties because no type has been selected so far.
        }
        else
        {
          if (showAdditionalProps)
          {
            AddAdditionalInputs(selectedType);
          }
          else
          {
            RemoveAdditionalInputs(selectedType);
          }
        }
      });

      GH_DocumentObject.Menu_AppendSeparator(menu);
      var foundtypes = SpeckleCore.SpeckleInitializer.GetTypes();

      foreach (Type type in foundtypes)
      {
        GH_DocumentObject.Menu_AppendItem(menu, type.ToString(), (item, e) =>
        {

          if (selectedType != null && selectedType.Equals(type)) // Same selected type as before
          {
            //graceful exit: don't do anything because type is the same as before.
          }
          else if (selectedType != null && !selectedType.Equals(type)) // Different selected type as before, reinit input!
          {
            AdaptInputs(type, showAdditionalProps);
            selectedType = type;
          } else if (selectedType == null) // Type was null
          {
            
            InitInputsFromScratch(type, showAdditionalProps);
            selectedType = type;
          }
          else
          {
          }

        });
      }

    }

    void InitInputsFromScratch(Type myType, bool showAdditionalProps)
    {
      DeleteInputs();
      Console.WriteLine(myType.ToString());
      this.Message = myType.Name;


      System.Reflection.PropertyInfo[] props = new System.Reflection.PropertyInfo[] { };

      if (showAdditionalProps == false)
        props = myType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
      else
        props = myType.GetProperties();
    


      List<Param_GenericObject> inputParams;
      RegisterInputParamerters(props, out inputParams);

      InitOutput(myType, inputParams);
      this.Params.OnParametersChanged();
      this.ExpireSolution(true);

    }

    // Delete inputs when object type is updated by the user
    void DeleteInputs()
    {
      List<IGH_Param> mycurrentParams = this.Params.Input;
      for (int i = mycurrentParams.Count - 1; i >= 0; i--)
      {
        Params.UnregisterInputParameter(mycurrentParams[i]);
      }

    }

    void AddAdditionalInputs(Type myType)
    {

      // find additional properties
      System.Reflection.PropertyInfo[] addProps = myType.BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      List<Param_GenericObject> inputParams;
      RegisterInputParamerters(addProps, out inputParams);

      this.Params.OnParametersChanged();
      this.ExpireSolution(true);
    }


    void RegisterInputParamerters(System.Reflection.PropertyInfo[] props, out List<Param_GenericObject> inputParams)
    {
      List<Param_GenericObject> _inputParams = new List<Param_GenericObject>();

      for (int i = 0; i < props.Length; i++)
      {
        // get property name and value
        Type propType = props[i].PropertyType;
        Type baseType = propType.BaseType;

        string propName = props[i].Name;
        object propValue = props.GetValue(i);

        // Create new param based on property name
        Param_GenericObject newInputParam = new Param_GenericObject();
        newInputParam.Name = propName;
        newInputParam.NickName = propName;
        newInputParam.MutableNickName = false;
        newInputParam.Description = propName + " as " + propType.Name;

        // check if input needs to be a list or item access
        bool isCollection = typeof(IEnumerable<>).IsAssignableFrom(propType);
        if (isCollection == true)
        {
          newInputParam.Access = GH_ParamAccess.list;
        }
        else
        {
          newInputParam.Access = GH_ParamAccess.item;
        }

        Params.RegisterInputParam(newInputParam);
        _inputParams.Add(newInputParam);

      }
      inputParams = _inputParams;
    }


    void RemoveAdditionalInputs(Type myType)
    {
      // removes additional properties and makes sure the type's inputs remain the same.
      System.Reflection.PropertyInfo[] previouslyAddedProps = myType.BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
      int numberOfAddedProps = previouslyAddedProps.Length;
      List<IGH_Param> myCurrentParams = this.Params.Input;
      int myCurrentParamsCount = myCurrentParams.Count;
      int removeUntil = (myCurrentParamsCount - 1) - (myCurrentParamsCount - numberOfAddedProps);
      for (int i = myCurrentParamsCount - 1; i >= 0; i--)
      {
        if(i >= myCurrentParamsCount - removeUntil - 1)
        {
          Params.UnregisterInputParameter(myCurrentParams[i]);
        }
        else
        {

        }
        
      }
      this.Params.OnParametersChanged();
      this.ExpireSolution(true);
    }


    void AdaptInputs(Type myType, bool showAdditionalProps)
    {
      
      Console.WriteLine(myType.ToString());
      this.Message = myType.Name;

      System.Reflection.PropertyInfo[] props = new System.Reflection.PropertyInfo[] { };

      props = myType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
      
      if (showAdditionalProps == false) // just reinit params
      {
        DeleteInputs();
        List<Param_GenericObject> inputParams = new List<Param_GenericObject>();

        RegisterInputParamerters(props, out inputParams);

        InitOutput(myType, inputParams);
        this.Params.OnParametersChanged();
        this.ExpireSolution(true);
      }
      else
      {
        // unregister previous type props and register new type props
        // keeps additional properties intact for high user expectations
        System.Reflection.PropertyInfo[] previousProps_All = selectedType.GetProperties();
        System.Reflection.PropertyInfo[] previousProps_ClassType = selectedType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

        
        for (int i = (previousProps_All.Length - 1); i >= 0; i--)
        {
          if((previousProps_ClassType.Length) > i)
          {
              this.Params.UnregisterInputParameter(this.Params.Input[i]);
          }
          else
          {
          }
        }

        List<Param_GenericObject> inputParams = new List<Param_GenericObject>();
        for (int i = 0; i < props.Length; i++)
        {
          // get property name and value
          Type propType = props[i].PropertyType;
          Type baseType = propType.BaseType;

          string propName = props[i].Name;
          object propValue = props.GetValue(i);

          // Create new param based on property name
          Param_GenericObject newInputParam = new Param_GenericObject();
          newInputParam.Name = propName;
          newInputParam.NickName = propName;
          newInputParam.MutableNickName = false;
          newInputParam.Description = propName + " as " + propType.Name;

          // check if input needs to be a list or item access
          bool isCollection = typeof(IEnumerable<>).IsAssignableFrom(propType);
          if (isCollection == true)
          {
            newInputParam.Access = GH_ParamAccess.list;
          }
          else
          {
            newInputParam.Access = GH_ParamAccess.item;
          }
          inputParams.Add(newInputParam);
          this.Params.RegisterInputParam(newInputParam,i);
          
        }

        

        InitOutput(myType, inputParams);
        this.Params.OnParametersChanged();
        this.ExpireSolution(true);

      }


      
    }


    void InitOutput(Type myType, List<Param_GenericObject> myInputParams)
    {
      DeleteOutput();
      var outputObject = Activator.CreateInstance(myType);
      List<IGH_Param> currentInputs = Params.Input;
      // Create new param for output
      Param_GenericObject newOutputParam = new Param_GenericObject();
      newOutputParam.Name = myType.Name;
      newOutputParam.NickName = myType.Name;

      Grasshopper.Kernel.Data.GH_Path gH_Path = new Grasshopper.Kernel.Data.GH_Path(0);

      newOutputParam.AddVolatileData(gH_Path, 0, outputObject);

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

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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using GH_IO.Serialization;

namespace SpeckleGrasshopper
{
  public class ConvertSpeckleObject : GH_Component
  {
    private Type SelectedType;

    public ConvertSpeckleObject()
      : base("Converts a Speckle Object", "CVSO",
        "Converts Speckle objects into another.",
        "Speckle", "SpeckleKits")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
      SelectedType = null;
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Input", "Input", "Object you want to convert.", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.Register_GenericParam("Object", "Object", "The created object.");
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (SelectedType is null)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No type chosen.");
        return;
      }

      object inputObject = null;
      if (!DA.GetData(0, ref inputObject))
        return;

      try
      {
        inputObject = inputObject.GetType().GetProperty("Value").GetValue(inputObject);
      }
      catch { }

      if (inputObject == null) return;

      if (!(inputObject.GetType().IsSubclassOf(typeof(SpeckleObject))))
        inputObject = Converter.Serialise(inputObject);

      SpeckleObject convertedObject = (SpeckleObject)Activator.CreateInstance(SelectedType);

      string inputObjectTypeString = (string)inputObject.GetType().GetProperty("Type").GetValue(inputObject);

      // Check to see if one is a subclass of another
      if (!((inputObjectTypeString.Contains(convertedObject.Type))) && !(convertedObject.Type.Contains(inputObjectTypeString)))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "SpeckleObject not convertible to type.");
        return;
      }

      var deepCopyObject = CreateCopy(inputObject);

      foreach (PropertyInfo p in convertedObject.GetType().GetProperties().Where(p => p.CanWrite))
      {
        PropertyInfo inputProperty = deepCopyObject.GetType().GetProperty(p.Name);
        if (inputProperty != null)
          p.SetValue(convertedObject, inputProperty.GetValue(deepCopyObject));
      }

      convertedObject.GenerateHash();
      
      // applicationId generation/setting
      var appId = convertedObject.GetType().GetProperty("ApplicationId").GetValue(convertedObject);
      if (appId == null)
      {
        var myGeneratedAppId = "gh/" + convertedObject.GetType().GetProperty("Hash").GetValue(convertedObject);
        convertedObject.GetType().GetProperty("ApplicationId").SetValue(convertedObject, myGeneratedAppId);
      }

      DA.SetData(0, convertedObject);
    }

    /// <summary>
    /// Creates a deep copy of a SpeckleObject.
    /// </summary>
    /// <param name="inputObject"></param>
    /// <returns></returns>
    public object CreateCopy(object inputObject)
    {
      using (MemoryStream stream = new MemoryStream())
      {
        if (inputObject.GetType().IsSerializable)
        {
          BinaryFormatter formatter = new BinaryFormatter();

          formatter.Serialize(stream, inputObject);

          stream.Position = 0;

          SpeckleObject val = (SpeckleObject)formatter.Deserialize(stream);
          val._id = null;

          return val;
        }

        return null;
      }
    }

    /// <summary>
    /// Populates tooltip menu.
    /// </summary>
    /// <param name="menu"></param>
    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);

      var foundtypes = SpeckleCore.SpeckleInitializer.GetTypes();

      var subMenus = new Dictionary<Assembly, ToolStripDropDownMenu>();
      var types = SpeckleCore.SpeckleInitializer.GetTypes();
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies().Where(ass => types.Any(t => t.Assembly == ass));

      foreach (Assembly assembly in assemblies)
      {
        menu.Items.Add(assembly.GetName().Name);
        var addedMenuItem = menu.Items[menu.Items.Count - 1];

        subMenus[assembly] = (ToolStripDropDownMenu)addedMenuItem.GetType().GetProperty("DropDown").GetValue(addedMenuItem);
      }

      foreach (Type type in types)
        subMenus[type.Assembly].Items.Add(type.Name, null, (sender, e) => SwitchToType(type));
    }

    /// <summary>
    /// Handles the change to the selected type.
    /// </summary>
    /// <param name="myType"></param>
    public void SwitchToType(Type myType)
    {
      if (SelectedType == myType) return;

      Message = myType.Name;
      SelectedType = myType;
      Params.Output[0].NickName = myType.Name;
      Params.OnParametersChanged();
      ExpireSolution(true);
    }

    /// <summary>
    /// Makes sure we deserialise correctly, and reinstate everything there is to reinstate:
    /// - selected type properties
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public override bool Read(GH_IReader reader) 
    {
      bool isInit = reader.GetBoolean("init");
      if (isInit)
      {
        var selectedTypeName = reader.GetString("type");
        var selectedTypeAssembly = reader.GetString("assembly");

        var selectedType = SpeckleCore.SpeckleInitializer.GetTypes().FirstOrDefault(t => t.Name == selectedTypeName && t.AssemblyQualifiedName == selectedTypeAssembly);
        if (selectedType != null)
          SwitchToType(selectedType);
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("Type {0} from the {1} kit was not found. Are you sure you have it installed?", selectedTypeName, selectedTypeAssembly));
        }
      }

      return base.Read(reader);
    }

    /// <summary>
    /// Serialises the current state of the component, making sure we save:
    /// - the current type.
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public override bool Write(GH_IWriter writer)
    {
      if (SelectedType != null)
      {
        writer.SetBoolean("init", true);
        writer.SetString("type", SelectedType.Name);
        writer.SetString("assembly", SelectedType.AssemblyQualifiedName);
      }
      else
        writer.SetBoolean("init", false);

      return base.Write(writer);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.Convert;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("777908f5-c789-4acc-a9a7-3e89ade1acd5"); }
    }
  }
}

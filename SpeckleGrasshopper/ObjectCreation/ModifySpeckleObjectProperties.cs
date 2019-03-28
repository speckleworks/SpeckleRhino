using System;
using System.Collections.Generic;
using SpeckleGrasshopper.Properties;
using System.Linq;
using System.Timers;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Reflection;
using SpeckleCore;
using System.Windows.Forms;
using System.Collections;
using GH_IO.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SpeckleGrasshopper
{
  public class ModifySpeckleObjectProperties : GH_Component
  {
    public Type ObjectType;
    public bool ExposeAllProperties;

    private System.Timers.Timer Debouncer;
    public Action ExpireComponentAction;

    public ModifySpeckleObjectProperties( )
      : base( "Modifies SpeckleObject Properties", "MSOP",
        "Allows properties of a SpeckleObject to be modified.",
        "Speckle", "SpeckleKits" )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
      ExpireComponentAction = () => this.ExpireSolution(true);
      Param_GenericObject newParam = new Param_GenericObject();
      newParam.Name = "Result";
      newParam.NickName = "R";
      newParam.MutableNickName = false;
      newParam.Access = GH_ParamAccess.item;
      Params.RegisterOutputParam( newParam );
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Debouncer = new System.Timers.Timer(2000); Debouncer.AutoReset = false;
      Debouncer.Elapsed += (sender, e) =>
      {
        Rhino.RhinoApp.MainApplicationWindow.Invoke((Action)delegate { this.ExpireSolution(true); });
      };

      foreach (var param in Params.Input)
      {
        param.ObjectChanged += (sender, e) =>
        {
          Debouncer.Start();
        };
      }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("SpeckleObject", "O", "Speckle object you want to modify.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }

    public override bool Write(GH_IWriter writer)
    {
      try
      {
        if (ObjectType != null)
        {
          using (var ms = new MemoryStream())
          {
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, ObjectType);
            var arr = ms.ToArray();
            var arrr = arr;
            writer.SetByteArray("objectype", ms.ToArray());
          }
        }
      }
      catch (Exception err)
      {
        throw err;
      }
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      try
      {
        var objectType = reader.GetByteArray("objectype");
        var copy = objectType;
        using (var ms = new MemoryStream())
        {
          ms.Write(objectType, 0, objectType.Length);
          ms.Seek(0, SeekOrigin.Begin);
          ObjectType = (Type)new BinaryFormatter().Deserialize(ms);
          var x = ObjectType;
        }
        UpdateInputs();
      }
      catch (Exception err)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to reinitialise sender.");
        //throw err;
      }
      return base.Read(reader);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      GH_ObjectWrapper input = null;
      if (!DA.GetData("SpeckleObject", ref input))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid object.");
        return;
      }

      if (input.Value == null)
        return;

      object inputObject = input.Value;

      if (inputObject.GetType() != ObjectType)
      { 
        ObjectType = inputObject.GetType();
        UpdateInputs();
      }

      var modifiedObject = CreateCopy(inputObject);
      
      DA.SetData("Result", UpdateObjectProperties(modifiedObject));
    }
    
    public void UpdateInputs()
    {
      // Unregister input parameter
      while (Params.Input.Count() > 1)
      {
        Params.UnregisterInputParameter(Params.Input.First(i => i.Name != "SpeckleObject"));
      }

      // Add input parameters
      IEnumerable<PropertyInfo> props = ObjectType.GetProperties().Where(p => p.CanWrite);
      
      if (!ExposeAllProperties)
        props = props.Where(p => !typeof(SpeckleObject).GetProperties().Any(s => s.Name == p.Name));

      foreach (PropertyInfo p in props)
      {
        Param_GenericObject newParam = new Param_GenericObject();
        newParam.Name = (string)p.Name;
        newParam.NickName = (string)p.Name;
        newParam.MutableNickName = false;
        newParam.Access = GH_ParamAccess.item;
        newParam.Optional = true;
        newParam.ObjectChanged += (sender, e) => Debouncer.Start();

        Params.RegisterInputParam(newParam);
      }
      Params.OnParametersChanged();
    }

    public object UpdateObjectProperties(object inputObject)
    {
      foreach (IGH_Param param in Params.Input)
      {
        if (param.Name == "SpeckleObject")
          continue;

        foreach (object o in param.VolatileData.AllData(false))
        {
          object value = null;
          try
          {
            value = o.GetType().GetProperty("Value").GetValue(o);
          }
          catch
          {
            continue;
          }

          PropertyInfo prop = ObjectType.GetProperty(param.Name);
          if (prop.PropertyType.IsEnum)
          {
            try
            {
              prop.SetValue(inputObject, Enum.Parse(prop.PropertyType, (string)value));
              continue;
            }
            catch { }

            try
            {
              prop.SetValue(inputObject, (int)value);
              continue;
            }
            catch { }

            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + param.Name + ".");
          }

          else if (value.GetType() != prop.PropertyType)
          {
            try
            {
              prop.SetValue(inputObject, value);
              continue;
            }
            catch { }

            try
            {
              var conv = Newtonsoft.Json.JsonConvert.DeserializeObject((string)value, prop.PropertyType);
              prop.SetValue(inputObject, conv);
              continue;
            }
            catch { }

            try
            {
              var conv = Converter.Serialise(value);
              prop.SetValue(inputObject, conv);
              continue;
            }
            catch { }

            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + param.Name + ".");
          }

          else
          {
            prop.SetValue(inputObject, value);
          }
        }
      }
      inputObject.GetType().GetMethod("GenerateHash").Invoke(inputObject, null);
      return inputObject;
    }

    public object CreateCopy(object inputObject)
    {
      var ret = Activator.CreateInstance(inputObject.GetType());

      foreach (FieldInfo f in inputObject.GetType().GetFields())
        f.SetValue(ret, f.GetValue(inputObject));

      foreach (PropertyInfo p in inputObject.GetType().GetProperties().Where(p => p.CanWrite))
        if (p.Name != "_id")
          p.SetValue(ret, p.GetValue(inputObject));
      
      return ret;
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      
      Menu_AppendItem(menu, "Expose all properties", (sender, e) =>
        {
          ExposeAllProperties = !ExposeAllProperties;
          UpdateInputs();
          Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }, true, ExposeAllProperties);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.SetUserData;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("e6a41f67-a45a-4ba3-9f91-a3e66d12a778"); }
    }
  }
}

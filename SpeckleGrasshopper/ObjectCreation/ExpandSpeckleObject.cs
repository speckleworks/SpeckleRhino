using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SpeckleCore;
using SpeckleGrasshopper.Parameters;
using SpeckleGrasshopper.Utilities;

namespace SpeckleGrasshopper
{
  public class ExpandSpeckleObject : GH_Component, IGH_VariableParameterComponent
  {

    HashSet<string> properties;

    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public ExpandSpeckleObject()
      : base("Expand Dictionary or SpeckleObject", "EUD",
          "Expands a SpeckleObject's properties or a dictionary into its component key value pairs.",
          "Speckle", "Special")
    {
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Debug.WriteLine(this.Params.Output.Count);
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Dictionaries", "D", "Dictionaries or Speckle Objects to expand.", GH_ParamAccess.item); //Ignore Structures
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
      var objs = Params.Input
        .First()
        .VolatileData
        .AllData(true)
        .ToList<object>();

      //Create An output for all the inputs.
      object myObject = null;
      if (!DA.GetData(0, ref myObject))
      {
        return;
      }

      if (DA.Iteration == 0)
      {
        properties = new HashSet<string>();

        if (GetDictionary(myObject, out var data))
        {
          properties = data.Keys.ToHastSet();
        }
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Valid Format");
          return;
        }
      }


      if (OutputMismatch() && DA.Iteration == 0)
      {
        OnPingDocument().ScheduleSolution(5, d =>
        {
          AutoCreateOutputs(false);
        });
      }
      else if (!OutputMismatch())
      {
        //First pass get children
        var data = new Dictionary<string, object>();
        var nicknames = Params.Output.Select(x => x.NickName).ToList();

        if (!GetDictionary(myObject, out data))
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Valid Format");
          return;
        }

        var notMatching = data.Keys.Where(x => !nicknames.Contains(x)).Any();
        if(notMatching)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Item is not matching the rest. Ignoring");
          return;
        }

        //Second pass to deserialise objects
        var paramID = 0; //This is so we can track the id of the Output Parameter.
        var dataItems = new Dictionary<string, object>();
        var dataList = new Dictionary<string, IEnumerable<object>>();
        var dataTree = new Dictionary<string, DataTree<object>>();


        foreach (var item in data)
        {
          if (myObject is SpeckleStream ss)
          {
            dataItems.Add(item.Key, new GH_SpeckleStream(ss));
          }
          if (item.Value is IEnumerable<SpeckleObject> speckleList)
          {
            dataList.Add(item.Key, Converter.Deserialise(speckleList));
          }
          else if (item.Value is SpeckleObject speckleObject)
          {
            dataItems.Add(item.Key, Converter.Deserialise(speckleObject));
          }
          else if (item.Value is IEnumerable<object> objectList)
          {
            dataList.Add(item.Key, objectList);
          }
          else if (item.Value is Dictionary<string, object> dictionaryObject)
          {
            dataItems.Add(item.Key, new GH_ObjectWrapper(dictionaryObject));
          }
          else if (item.Value is Dictionary<string, IEnumerable<object>> dictionaryList)
          {
            var tree = new DataTree<object>();
            foreach (var d in dictionaryList)
            {
              var listWrapper = new List<GH_ObjectWrapper>();
              d.Value.ToList().ForEach(x => listWrapper.Add(new GH_ObjectWrapper(x)));
              if (int.TryParse(d.Key, out var index))
              {
                tree.AddRange(d.Value, DA.ParameterTargetPath(paramID).AppendElement(index));
              }
            }
            dataTree.Add(item.Key, tree);
          }
          else if (item.Value is object _Object)
            dataItems.Add(item.Key, _Object);
          else if (myObject is GH_SpeckleStream gH_SpeckleStream2)
          {
            dataItems.Add(item.Key, gH_SpeckleStream2);
          }


          paramID++;
        }

        //Add The Data Now
        foreach (var item in dataItems)
        {
          if (item.Value is IEnumerable<object> list)
            DA.SetDataList(item.Key, list);
          else
            DA.SetDataList(item.Key, new List<object>() { item.Value });
        }
        foreach (var item in dataList)
        {
          DA.SetDataList(item.Key, item.Value);
        }
        foreach (var item in dataTree)
        {
          int n = 0;
          foreach (var nickname in Params.Output.Select(x => x.NickName))
          {
            if (nickname.Equals(item.Key))
            {
              break;
            }
            n++;
          }
          DA.SetDataTree(n, item.Value);
        }

      }
    }

    void ToDataTree(IEnumerable list, ref DataTree<object> Tree, List<int> path)
    {
      int k = 0;
      int b = 0;
      bool addedRecurse = false;
      foreach (var item in list)
      {
        if ((item is IEnumerable) && !(item is string))
        {
          if (!addedRecurse)
          {
            path.Add(b);
            addedRecurse = true;
          }
          else
            path[path.Count - 1]++;

          ToDataTree(item as IEnumerable, ref Tree, path);
        }
        else
        {
          GH_Path Path = new GH_Path(path.ToArray());
          Tree.Insert(item, Path, k++);
        }
      }
    }

    private bool OutputMismatch()
    {
      var countMatch = properties.Count() == Params.Output.Count;
      if (!countMatch) return true;

      var list = properties.ToList();
      for (int i = 0; i < properties.Count; i++)
      {
        if (!(Params.Output[i].NickName == list[i]))
        {
          return true;
        }
      }

      return false;
    }

    private void AutoCreateOutputs(bool recompute)
    {

      var tokenCount = properties.Count();
      if (tokenCount == 0) return;

      if (OutputMismatch())
      {
        RecordUndoEvent("Creating Outputs");
        if (Params.Output.Count < tokenCount)
        {
          while (Params.Output.Count < tokenCount)
          {
            var new_param = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
            Params.RegisterOutputParam(new_param);
          }
        }
        else if (Params.Output.Count > tokenCount)
        {
          while (Params.Output.Count > tokenCount)
          {
            Params.UnregisterOutputParameter(Params.Output[Params.Output.Count - 1]);
          }
        }
        Params.OnParametersChanged();
        VariableParameterMaintenance();
        ExpireSolution(recompute);
      }
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return false;
    }
    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return false;
    }
    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      return new Param_GenericObject();
    }

    public void VariableParameterMaintenance()
    {
      if (properties == null)
        return;
      var tokens = properties.ToList();
      if (tokens == null)
        return;
      var names = tokens.ToList();
      for (var i = 0; i < Params.Output.Count; i++)
      {
        if (i > names.Count - 1) return;
        var name = names[i];

        Params.Output[i].Name = $"{name}";
        Params.Output[i].NickName = $"{name}";
        Params.Output[i].Description = $"Data from property: {name}";
        Params.Output[i].MutableNickName = false;
        Params.Output[i].Access = GH_ParamAccess.tree;
      }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.ExpandUserData;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("{D69F00DD-8F8C-4CE2-8B51-DAE8362844F7}"); }
    }

    public bool GetDictionary(object myObject, out Dictionary<string, object> data)
    {
      data = new Dictionary<string, object>();
      if (myObject is GH_SpeckleStream gH_SpeckleStream)
      {
        data = gH_SpeckleStream.Value.ToDictionary();
      }
      else if (myObject is SpeckleStream speckleStream)
      {
        data = speckleStream.ToDictionary();
      }
      else if (myObject is SpeckleObject speckleObject)
      {
        data = speckleObject.Properties;
      }
      else if (myObject is GH_SpeckleObject gH_SpeckleObject1)
      {
        data = gH_SpeckleObject1.Value.Properties;
      }
      else if (myObject is GH_ObjectWrapper ow)
      {
        if (ow.Value is Dictionary<string, object> dictObject)
          data = dictObject;
        else if (ow.Value is Dictionary<string, IEnumerable<object>> dictList)
        {
          foreach (var item in dictList)
          {
            data.Add(item.Key, item.Value);
          }
        }
        else if (ow.Value is GH_SpeckleStream os)
        {
          data = os.Value.ToDictionary();
        }
        else if (ow.Value is SpeckleStream ss)
        {
          data = ss.ToDictionary();
        }
        else if (ow.Value is SpeckleObject so)
        {
          data = so.Properties;
        }
        else if (ow.Value is GH_SpeckleObject ghSO)
        {
          data = ghSO.Value.Properties;
        }
      }
      return data.Any();
    }

  }
}

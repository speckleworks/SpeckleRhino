//extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Newtonsoft.Json;
using Rhino.Collections;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using System.IO;
using Grasshopper.Kernel.Parameters;
using SpeckleCore;
using System.Linq;

namespace SpeckleGrasshopper
{
  public class QuerySpeckleObjectComponent : GH_Component, IGH_VariableParameterComponent
  {
    string serialisedUDs;
    Dictionary<string, (GH_ParamAccess, int, object)> properties;
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public QuerySpeckleObjectComponent()
      : base("Querry Speckle Object", "GNV",
          "Gets a value from a dictionary by string of concatenated keys. \n For example, 'prop.subprop.subsubprop'.",
          "Speckle", "Special")
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
      pManager.AddParameter(new SpeckleObjectParameter(), "Speckle Object", "SO", "The Speckle Object you want to query", GH_ParamAccess.item);
      pManager.AddTextParameter("Path", "P", "Path of desired property, separated by dots.\nExample:'turtle.smallerTurtle.microTurtle'", GH_ParamAccess.list);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      //pManager.AddGenericParameter("Output", "O", "Output value.", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      GH_SpeckleObject GHspeckleObject = null;
      if (!DA.GetData(0, ref GHspeckleObject))
        return;

      var dict = GHspeckleObject.Value.Properties;

      var paths = new List<string>();
      if (!DA.GetDataList(1, paths))
        return;

      object target = null;

      //First Pass on Iteration 0
      int o = 0;
      if (DA.Iteration == 0)
      {
        properties = new Dictionary<string, (GH_ParamAccess, int, object)>();
        foreach (var p in paths)
        {
          var temp = dict;
          var keys = p.Split('.');
          for (int i = 0; i < keys.Length; i++)
          {
            if (i == keys.Length - 1)
              target = temp[keys[i]];
            else
            {
              var t = temp[keys[i]];
              if (t is Dictionary<string, object> d)
                temp = d;
              else if (t is SpeckleObject speckleObject)
                temp = speckleObject.Properties;
            }
          }

          if (target is List<object> myList)
          {
            properties.Add(p, (GH_ParamAccess.list, o, myList));
            o++;
          }
          else if (target is object)
          {
            properties.Add(p, (GH_ParamAccess.item, o, target));
            o++;
          }
        }
      }

      if(OutputMismatch() && DA.Iteration == 0)
      {
        OnPingDocument().ScheduleSolution(5, d =>
        {
          AutoCreateOutputs(false);
        });
      }
      else
      {
        foreach (var tuple in properties.Values)
        {
          if (tuple.Item1 == GH_ParamAccess.list)
            DA.SetDataList(tuple.Item2, tuple.Item3 as IEnumerable<object>);
          else if(tuple.Item1 == GH_ParamAccess.item)
            DA.SetData(tuple.Item2, tuple.Item3);
        }
      }

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

    private bool OutputMismatch()
    {
      var countMatch = properties.Count() == Params.Output.Count;
      if (!countMatch) return true;

      foreach (var name in properties)
      {
        if (!Params.Output.Select(p => p.NickName).Any(n => n == name.Key))
        {
          return true;
        }
      }

      return false;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      return new Param_GenericObject();
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public void VariableParameterMaintenance()
    {
      var tokens = properties;
      if (tokens == null) return;
      var names = tokens.Keys.ToList();
      for (var i = 0; i < Params.Output.Count; i++)
      {
        if (i > names.Count - 1) return;
        var name = names[i];
        var type = tokens[name];

        Params.Output[i].Name = $"{name}";
        Params.Output[i].NickName = $"{name}";
        Params.Output[i].Description = $"Data from property: {name}";
        Params.Output[i].MutableNickName = false;
        
        if (type.Item1 == GH_ParamAccess.item)
          Params.Output[i].Access = GH_ParamAccess.item;
        else if(type.Item1 == GH_ParamAccess.list)
          Params.Output[i].Access = GH_ParamAccess.list;
      }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.json;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("{3442BAA5-A3AD-4F0B-AF82-205532170B32}"); }
    }
  }
}

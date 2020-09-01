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
    HashSet<string> properties;
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
      pManager.AddTextParameter("Path", "P", "Path of desired property, separated by dots.\nExample:'turtle.smallerTurtle.microTurtle'", GH_ParamAccess.tree);
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

      //First Pass on Iteration 0
      if (DA.Iteration == 0)
      {
        //Get All the Paths, even if on a tree
        var allData = Params.Input.OfType<Param_String>()
               .First()
               .VolatileData.AllData(true)
               .OfType<GH_String>()
               .Select(s => s.Value);
        if (!allData.Any())
        {
          return;
        }
        properties = new HashSet<string>();
        foreach (var p in allData)
        {
          properties.Add(p);
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
        int o = 0;
        foreach (var p in properties)
        {

          var temp = dict;
          var keys = p.Split('.');
          object target = null;

          for (int i = 0; i < keys.Length; i++)
          {
            if (i == keys.Length - 1)
              if (temp.ContainsKey(keys[i]))
              {
                target = temp[keys[i]];
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter {o + 1} is missing data at [{i}]{keys[i]}");
                break;
              }
            else
            {
              if (temp.ContainsKey(keys[i]))
              {
                var t = temp[keys[i]];
                if (t is Dictionary<string, object> d)
                  temp = d;
                else if (t is SpeckleObject speckleObject)
                  temp = speckleObject.Properties;
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter {o + 1} is missing data at {keys[i]}");
                break;
              }
            }
          }

          if (target is List<object> myList)
          {
            DA.SetDataList(o, myList);
          }
          else if (target is object)
          {
            DA.SetDataList(o, new List<object> { target });
          }
          o++;
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
      var names = tokens.ToList();
      for (var i = 0; i < Params.Output.Count; i++)
      {
        if (i > names.Count - 1) return;
        var name = names[i];

        Params.Output[i].Name = $"{name}";
        Params.Output[i].NickName = $"{name}";
        Params.Output[i].Description = $"Data from property: {name}";
        Params.Output[i].MutableNickName = false;
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

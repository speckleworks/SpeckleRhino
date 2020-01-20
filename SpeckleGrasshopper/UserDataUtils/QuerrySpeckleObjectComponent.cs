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

namespace SpeckleGrasshopper
{
		public class QuerySpeckleObjectComponent : GH_Component
		{
				string serialisedUDs;
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
						pManager.AddTextParameter("Path", "P", "Path of desired property, separated by dots.\nExample:'turtle.smallerTurtle.microTurtle'", GH_ParamAccess.item);
				}

				/// <summary>
				/// Registers all the output parameters for this component.
				/// </summary>
				protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
				{
						pManager.AddGenericParameter("Output", "O", "Output value.", GH_ParamAccess.list);
				}

				/// <summary>
				/// This is the method that actually does the work.
				/// </summary>
				/// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
				protected override void SolveInstance(IGH_DataAccess DA)
				{
						GH_SpeckleObject speckleObject = null;
						if (!DA.GetData(0, ref speckleObject))
							return;

            var dict = speckleObject.Value.Properties;

						if (dict == null)
								throw new Exception("No dictionary provided.");

						string path = null; DA.GetData(1, ref path);
						if (path == null)
								throw new Exception("No path provided.");

						object target = null;
						var temp = dict;

						var keys = path.Split('.');
						for(int i = 0; i<keys.Length; i++)
						{
								if (i == keys.Length - 1) 
                  target = temp[keys[i]];
								else 
                  temp = temp[keys[i]] as Dictionary<string, object>;
						}
            
            if (target is List<object> myList)
              DA.SetDataList(0, myList);
            else if(target is object)
              DA.SetDataList(0, new List<object> { target});

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

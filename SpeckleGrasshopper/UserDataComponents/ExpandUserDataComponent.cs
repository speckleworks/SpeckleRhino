using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Runtime;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;
using System.Linq;

namespace UserDataUtils
{
    public class ExpandUserDataComponent : GH_Component, IGH_VariableParameterComponent
    {
        Dictionary<string, List<object>> global;
        Action expireComponent, setInputsAndExpireComponent;

        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ExpandUserDataComponent()
          : base("Expand User Data", "EUD",
              "Expands user dictionaries into their component keys (if matching).",
              "Speckle", "User Data Utils")
        {
            expireComponent = () =>
            {
                this.ExpireSolution(true);
            };

            setInputsAndExpireComponent = () =>
            {
                for (int i = Params.Output.Count - 1; i >= 0; i--)
                {
                    var myParam = Params.Output[i];
                    if ((!global.Keys.Contains(myParam.Name)) || (!global.Keys.Contains(myParam.NickName)))
                    {
                        Params.UnregisterOutputParameter(myParam, true);
                    }
                }

                Params.OnParametersChanged();
                foreach (var key in global.Keys)
                {
                    var myparam = Params.Output.FirstOrDefault(q => q.Name == key);
                    if (myparam == null)
                    {
                        Param_GenericObject newParam = getGhParameter(key);
                        Params.RegisterOutputParam(newParam);
                    }
                }

                Params.OnParametersChanged();
                //end
                this.ExpireSolution(true);
            };
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
            pManager.AddGenericParameter("Dictionaries", "D", "Dictionaries to expand.", GH_ParamAccess.tree);
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
            List<object> objs = new List<object>();
            objs = Params.Input[0].VolatileData.AllData(true).ToList<object>();
            if (objs.Count == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No dictionaries found.");
                return;
            }

            global = new Dictionary<string, List<object>>();
            var first = true;

            foreach (var obj in objs)
            {
                GH_ObjectWrapper goo = obj as GH_ObjectWrapper;
                if (goo == null)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provided object not a dictionary.");
                    return;
                }
                ArchivableDictionary dict = goo.Value as ArchivableDictionary;
                if (dict != null)
                {
                    foreach (var key in dict.Keys)
                    {
                        if ((first))
                        {
                            global.Add(key, new List<object>());
                            global[key].Add(dict[key]);
                        }
                            
                        else if (!global.Keys.Contains(key))
                        {
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Object dictionaries do not match.");
                            return;
                        } else
                        {
                            global[key].Add(dict[key]);
                        }
                    }
                }
                first = false;
            }

            if (global.Keys.Count == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Empty dictionary.");
                return;
            }

            var changed = false;

            if (Params.Output.Count != global.Keys.Count)
            {
                changed = true;
            }

            Debug.WriteLine("changed:" + changed);

            if (changed)
            {
                Rhino.RhinoApp.MainApplicationWindow.Invoke(setInputsAndExpireComponent);
            }
            else
            {
                int k = 0;
                foreach (var key in global.Keys)
                {
                    Params.Output[k].Name = Params.Output[k].NickName = key;
                    DA.SetDataList(k++, global[key].Select(x => new GH_ObjectWrapper(x)));
                }
            }
        }

        private Param_GenericObject getGhParameter(string key)
        {
            Param_GenericObject newParam = new Param_GenericObject();
            newParam.Name = (string)key;
            newParam.NickName = (string)key;
            newParam.MutableNickName = false;
            newParam.Access = GH_ParamAccess.list;
            return newParam;
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, Int32 index)
        {
            return false;
        }
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, Int32 index)
        {
            return null;
        }

        public void VariableParameterMaintenance()
        {
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
            get { return new Guid("{b678aa1d-393b-4287-aa5a-9d8123cb033e}"); }
        }
    }
}
//using System;
//using System.Collections.Generic;

//using Grasshopper.Kernel;
//using Rhino.Geometry;
//using System.Windows.Forms;
//using System.IO;
//using Rhino.Collections;
//using Grasshopper.Kernel.Types;
//using System.Linq;

//namespace UserDataUtils
//{
//    public class CsvUserData : GH_Component
//    {

//        string csv;
//        /// <summary>
//        /// Initializes a new instance of the CsvUserData class.
//        /// </summary>
//        public CsvUserData()
//          : base("User Data to CSV", "CSVUD",
//              "Spits out csvs for the provied dictionaries.",
//              "Speckle", "User Data Utils")
//        {
//        }

//        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
//        {
//            base.AppendAdditionalMenuItems(menu);
//            GH_DocumentObject.Menu_AppendSeparator(menu);
//            GH_DocumentObject.Menu_AppendItem(menu, @"Save results to file.", (e, sender) =>
//            {
//                SaveFileDialog savefile = new SaveFileDialog();
//                savefile.FileName = "userDictionaries.csv";
//                savefile.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

//                if (savefile.ShowDialog() == DialogResult.OK)
//                {
//                    using (StreamWriter sw = new StreamWriter(savefile.FileName))
//                        sw.WriteLine(csv);
//                }
//            });
//            GH_DocumentObject.Menu_AppendSeparator(menu);
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddGenericParameter("User Data", "D", "User Dictionaries to export as CSV.", GH_ParamAccess.list);
//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddTextParameter("CSV", "C", "CSV output", GH_ParamAccess.item);
//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            List<object> objs = new List<object>();
//            DA.GetDataList(0, objs);
//            if (objs.Count == 0)
//            {
//                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No dictionaries found.");
//                return;
//            }

//            List<ArchivableDictionary> dictList = new List<ArchivableDictionary>();

//            foreach (var obj in objs)
//            {
//                GH_ObjectWrapper goo = obj as GH_ObjectWrapper;
//                if (goo == null)
//                {
//                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provided object not a dictionary.");
//                    return;
//                }
//                ArchivableDictionary dict = goo.Value as ArchivableDictionary;
//                if (dict == null)
//                {
//                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Provided object not a dictionary.");
//                    return;
//                }

//                dictList.Add(dict);
//            }

//            if (dictList.Count == 0)
//            {
//                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No dictionaries found.");
//                return;
//            }

//            HashSet<string> allProps = new HashSet<string>();
//            List<string> rows = new List<string>();

//            // construct headers
//            foreach (var dict in dictList)
//                allProps.UnionWith(getDictProps("", dict));

//            rows.Add(String.Join(",", allProps.ToArray()));

//            foreach (var dict in dictList)
//            {
//                List<string> row = new List<string>();
//                foreach (string prop in allProps)
//                    row.Add(tryGetProperty(prop, dict).ToString());
//                rows.Add(String.Join(",", row));
//            }

//            csv = String.Join("\n", rows);
//            DA.SetData(0, csv);
//        }

//        public List<string> getDictProps(string rootProp, ArchivableDictionary d)
//        {
//            List<string> props = new List<string>();

//            foreach (var key in d.Keys)
//            {
//                if (d[key] is ArchivableDictionary)
//                {
//                    props.AddRange(getDictProps(rootProp + (rootProp == "" ? "" : ".") + key, d[key] as ArchivableDictionary));
//                }
//                else
//                {
//                    props.Add(rootProp + (rootProp == "" ? "" : ".") + key);
//                }
//            }

//            return props;
//        }

//        public object tryGetProperty(string prop, ArchivableDictionary d)
//        {
//            List<string> props = prop.Split('.').ToList();

//            if (props.Count == 0)
//                return "error";

//            if (!d.Keys.Contains(props.ElementAt(0)))
//                return "null";

//            if (props.Count > 1)
//            {
//                var key = props.ElementAt(0);
//                props.RemoveAt(0);
//                return tryGetProperty(String.Join(".", props), d[key] as ArchivableDictionary);
//            }

//            return d[props.ElementAt(0)];
//        }

//        /// <summary>
//        /// Provides an Icon for the component.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                return Properties.Resources.csv;
//            }
//        }

//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("{6ec27144-e337-47b4-af22-d9934feaa365}"); }
//        }
//    }
//}
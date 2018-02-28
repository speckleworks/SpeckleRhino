using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Rhino.Collections;

namespace UserDataUtils
{
    public class CreateUserData : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Initializes a new instance of the CreateUserData class.
        /// </summary>
        public CreateUserData()
          : base("Create Custom User Data", "CUD",
              "Creates a custom user dictionary which you can nest in another dictionary.",
              "Speckle", "User Data Utils")
        {
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            foreach (var param in Params.Input)
            {
                param.ObjectChanged += (sender, e) =>
                {
                    Debug.WriteLine("(CUD:) param changed name.");
                    Rhino.RhinoApp.MainApplicationWindow.Invoke((Action)delegate { this.ExpireSolution(true); });
                };
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("A", "A", "Data to attach to this key.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("User Data", "UD", "The user data as an Archivable Dictionary.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var props = new ArchivableDictionary();

            for (int i = 0; i < Params.Input.Count; i++)
            {
                var key = Params.Input[i].NickName;

                object ghInputProperty = null;
                DA.GetData(i, ref ghInputProperty);

                if (ghInputProperty == null)
                {
                    props.Set(key, "undefined");
                    continue;
                }

                object valueExtract = ghInputProperty.GetType().GetProperty("Value").GetValue(ghInputProperty, null);

                Debug.WriteLine(key + ": " + valueExtract.GetType().ToString());

                GeometryBase geometry = getGeometryBase(valueExtract);

                if (geometry != null)
                {
                    props.Set(key, geometry);
                    continue;
                }

                if (valueExtract is double)
                    props.Set(key, (double)valueExtract);

                if (valueExtract is Int32 || valueExtract is Int64 || valueExtract is Int16 || valueExtract is int)
                    props.Set(key, (int)valueExtract);

                if (valueExtract is string)
                    props.Set(key, (string)valueExtract);

                if (valueExtract is bool)
                    props.Set(key, (bool)valueExtract);

                if (valueExtract is Vector3d)
                    props.Set(key, (Vector3d)valueExtract);

                if (valueExtract is Point3d)
                    props.Set(key, (Point3d)valueExtract);

                if (valueExtract is Line)
                    props.Set(key, (Line)valueExtract);

                if ((valueExtract is Circle))
                    props.Set(key, new ArcCurve((Circle)valueExtract));

                if (valueExtract is Interval)
                    props.Set(key, (Interval)valueExtract);

                if (valueExtract is UVInterval)
                    props.Set(key, "UV Interval not supported.");

                if (valueExtract is Plane)
                    props.Set(key, (Plane)valueExtract);

                if (valueExtract is ArchivableDictionary)
                    props.Set(key, (ArchivableDictionary)valueExtract);
            }

            DA.SetData(0, props);
        }

        public GeometryBase getGeometryBase(object myObject)
        { 
            if (myObject is Rectangle3d) return ((Rectangle3d)myObject).ToNurbsCurve();
            if (myObject is Polyline) return ((Polyline)myObject).ToNurbsCurve();
            if (myObject is Box) return ((Box)myObject).ToBrep();

            return myObject as GeometryBase;
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input) return true;
            return false;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Input) return true;
            return false;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            Grasshopper.Kernel.Parameters.Param_GenericObject param = new Param_GenericObject();

            param.Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input);
            param.NickName = param.Name;
            param.Description = "Property Name";
            param.Optional = true;
            param.Access = GH_ParamAccess.item;

            param.ObjectChanged += (sender, e) =>
            {
                Debug.WriteLine("(CUD:) param changed name.");
                Rhino.RhinoApp.MainApplicationWindow.Invoke((Action)delegate { this.ExpireSolution(true); });
            };

            return param;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Input;
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
                return Properties.Resources.CreateUserData;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d0350df1-fd31-4ae9-9154-815334c0b853}"); }
        }
    }


}
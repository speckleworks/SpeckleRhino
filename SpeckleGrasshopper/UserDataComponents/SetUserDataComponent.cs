using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;

using System.Linq;
using Grasshopper.Kernel.Types;
using Rhino.Runtime;
using Rhino.Collections;

namespace UserDataUtils
{
    public class SetUserDataComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SetUserDataComponent()
          : base("Set User Data", "SUD",
              "Sets user data to an object.",
              "Speckle", "User Data Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object to attach user data to.", GH_ParamAccess.item);
            pManager.AddGenericParameter("User Data", "D", "Data to attach.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object with user data.", GH_ParamAccess.item);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object objectRef = null;
            DA.GetData(0, ref objectRef);

            if (objectRef == null)
                return;

            object valueExtract = objectRef.GetType().GetProperty("Value").GetValue(objectRef, null);

            GeometryBase recipient = getGeometryBase(valueExtract);

            if (recipient == null)
            {
                DA.SetData(0, null);
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input object not supported.");
                return;
            }

            object dictObject = null;
            DA.GetData(1, ref dictObject);

            GH_ObjectWrapper temp = dictObject as GH_ObjectWrapper;
            if (temp == null)
            {
                DA.SetData(0, recipient);
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Dictionary not provided. Object not modified.");
                return;
            }

            ArchivableDictionary dict = ((GH_ObjectWrapper)dictObject).Value as ArchivableDictionary;
            if (dict == null)
            {
                DA.SetData(0, recipient);
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Dictionary not valid. Object not modified.");
                return;
            }

            recipient.UserDictionary.ReplaceContentsWith(dict);

            DA.SetData(0, new GH_ObjectWrapper(recipient));
        }

        public GeometryBase getGeometryBase(object myObject)
        {
            Debug.WriteLine(myObject.GetType().ToString());
            if (myObject is GH_Point) return new Point(((GH_Point)myObject).Value);
            if (myObject is Point3d) return new Point((Point3d)myObject);
            if (myObject is Line) return ((Line)myObject).ToNurbsCurve();
            if (myObject is Circle) return ((Circle)myObject).ToNurbsCurve();
            if (myObject is Arc) return ((Arc)myObject).ToNurbsCurve();
            if (myObject is Rectangle3d) return ((Rectangle3d)myObject).ToNurbsCurve();
            if (myObject is Polyline) return ((Polyline)myObject).ToNurbsCurve();
            if (myObject is Box) return ((Box)myObject).ToBrep();

            return myObject as GeometryBase;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.SetUserData;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{4edbb242-15e2-4b30-89c4-fa800d156250}"); }
        }
    }
}

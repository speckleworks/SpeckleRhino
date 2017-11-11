using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Rhino.Display;

namespace SpeckleRhino
{
    /// <summary>
    /// (c) Luis @fraguada
    /// </summary>
    public class SpeckleDisplayConduit : Rhino.Display.DisplayConduit
    {

        public List<GeometryBase> Geometry { get; set; }

        public List<Color> Colors { get; set; }

        public List<bool> VisibleList { get; set; }

        public SpeckleDisplayConduit()
        {
            Geometry = new List<GeometryBase>();
        }

        public SpeckleDisplayConduit(List<GeometryBase> _Geometry)
        {
            Geometry = _Geometry;
            Colors = new List<Color>();
            VisibleList = new List<bool>();
        }

        public SpeckleDisplayConduit(List<GeometryBase> _Geometry, List<Color> _Colors, List<bool> _VisibleList)
        {
            Geometry = _Geometry;
            Colors = _Colors;
            VisibleList = _VisibleList;
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            Rhino.Geometry.BoundingBox bbox = Rhino.Geometry.BoundingBox.Unset;
            if (null != Geometry)
            {
                foreach (var obj in Geometry)
                    bbox.Union(obj.GetBoundingBox(false));
                e.IncludeBoundingBox(bbox);
            }

        }

        protected override void CalculateBoundingBoxZoomExtents(CalculateBoundingBoxEventArgs e)
        {
            Rhino.Geometry.BoundingBox bbox = Rhino.Geometry.BoundingBox.Unset;
            if (null != Geometry)
            {
                foreach (var obj in Geometry)
                    bbox.Union(obj.GetBoundingBox(false));
                e.IncludeBoundingBox(bbox);
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);
            int count = 0;

            foreach (var obj in Geometry)
            {
                switch (obj.ObjectType)
                {
                    case Rhino.DocObjects.ObjectType.Mesh:
                        Rhino.Display.DisplayMaterial material = new Rhino.Display.DisplayMaterial(Color.Chartreuse, 0.5);
                        e.Display.DrawMeshShaded((obj as Rhino.Geometry.Mesh), material);
                        e.Display.DrawMeshWires((obj as Rhino.Geometry.Mesh), Color.Beige, 1);
                        break;
                }
                count++;
            }
        }
    }
}

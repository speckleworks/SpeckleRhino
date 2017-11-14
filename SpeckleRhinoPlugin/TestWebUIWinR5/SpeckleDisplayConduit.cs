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
                    case Rhino.DocObjects.ObjectType.Point:
                        e.Display.DrawPoint(((Rhino.Geometry.Point)obj).Location, Color.Pink);
                        break;
                    case Rhino.DocObjects.ObjectType.Curve:
                        e.Display.DrawCurve((Curve)obj, Color.Chartreuse);
                        break;
                    case Rhino.DocObjects.ObjectType.Brep:
                        DisplayMaterial bMaterial = new DisplayMaterial(Color.Chartreuse, 0.5);
                        e.Display.DrawBrepShaded((Brep)obj, bMaterial);
                        //e.Display.DrawBrepWires((Brep)obj, Color.DarkGray, 1);
                        break;
                    case Rhino.DocObjects.ObjectType.Mesh:
                        DisplayMaterial mMaterial = new Rhino.Display.DisplayMaterial(Color.Chartreuse, Color.Yellow,Color.White, Color.White, 0.1,0.5);
                        e.Display.DrawMeshShaded((Mesh)obj, mMaterial);
                        //e.Display.DrawMeshWires((Mesh)obj, Color.DarkGray);
                        break;
                    case Rhino.DocObjects.ObjectType.TextDot:
                        //todo
                        break;
                    case Rhino.DocObjects.ObjectType.Annotation:
                        //todo
                        break;
                }
                count++;
            }
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);
            foreach (var obj in Geometry)
            {
                //e.Display.DrawBoxCorners(((GeometryBase)obj).GetBoundingBox(false), Color.Yellow);
            }
        }
    }
}

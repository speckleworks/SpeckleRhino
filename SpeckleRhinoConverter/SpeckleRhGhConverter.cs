using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;

using SpeckleCore;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SpeckleRhinoConverter
{
    /// <summary>
    /// A Rhino conversion utility class. 
    /// </summary>
    public class RhinoConverter : Converter, IDisposable
    {
        /// <summary>
        /// Converts Speckle objects to Rhino objects.
        /// </summary>
        /// <param name="_objects"></param>
        /// <returns></returns>
        public override IEnumerable<object> ToNative(IEnumerable<SpeckleObject> _objects)
        {
            return _objects.Select(o => ToNative(o));
        }

        /// <summary>
        /// Converts Speckle objects to Rhino objects.
        /// </summary>
        /// <param name="_objects"></param>
        /// <returns></returns>
        public override object ToNative(SpeckleObject _object)
        {
            object encodedObject = null;

            switch (_object.Type)
            {
                case "invalid_object":
                    encodedObject = "Invalid object. Source geometry or data could not be converted.";
                    break;
                case "Number":
                    encodedObject = ((SpeckleNumber)_object).Value;
                    break;
                case "Boolean":
                    encodedObject = ((SpeckleBoolean)_object).Value;
                    break;
                case "Interval":
                    encodedObject = ((SpeckleInterval)_object).ToRhino();
                    break;
                case "Interval2d":
                    encodedObject = ((SpeckleInterval2d)_object).ToRhino();
                    break;
                case "String":
                    encodedObject = ((SpeckleString)_object).Value;
                    break;
                case "Point":
                    encodedObject = ((SpecklePoint)_object).ToRhino();
                    break;
                case "Vector":
                    encodedObject = ((SpeckleVector)_object).ToRhino();
                    break;
                case "Plane":
                    encodedObject = ((SpecklePlane)_object).ToRhino();
                    break;
                case "Line":
                    encodedObject = ((SpeckleLine)_object).ToRhino();
                    break;
                case "Circle":
                    encodedObject = ((SpeckleCircle)_object).ToRhino();
                    break;
                case "Arc":
                    encodedObject = ((SpeckleArc)_object).ToRhino();
                    break;
                case "Ellipse":
                    encodedObject = ((SpeckleEllipse)_object).ToRhino();
                    break;
                case "Rectangle":
                    encodedObject = ((SpeckleRectangle)_object).ToRhino();
                    break;
                case "Box":
                    encodedObject = ((SpeckleBox)_object).ToRhino();
                    break;
                case "Polyline":
                    encodedObject = ((SpecklePolyline)_object).ToRhino();
                    break;
                case "Curve":
                    encodedObject = ((SpeckleCurve)_object).ToRhino();
                    break;
                case "Polycurve":
                    encodedObject = ((SpecklePolycurve)_object).ToRhino();
                    break;
                case "Brep":
                    encodedObject = ((SpeckleBrep)_object).ToRhino();
                    break;
                case "Mesh":
                    encodedObject = ((SpeckleMesh)_object).ToRhino();
                    break;
                case "Extrusion":
                    encodedObject = ((SpeckleExtrusion)_object).ToRhino();
                    break;
                case "Annotation":
                    if (double.IsNaN(((SpeckleAnnotation)_object).TextHeight))
                    {
                        encodedObject = ((SpeckleAnnotation)_object).ToRhinoTextDot();
                    }
                    else
                    {
                        encodedObject = ((SpeckleAnnotation)_object).ToRhinoTextEntity();
                    }
                    break;
                case "Abstract":
                    encodedObject = Converter.FromAbstract((SpeckleAbstract)_object);
                    //encodedObject = _object;
                    break;
                default:
                    encodedObject = _object;
                    break;
            };


            if (_object.Properties == null)
                return encodedObject;

            if (encodedObject is GeometryBase)
            {
                var fullObj = encodedObject as GeometryBase;
                fullObj.UserDictionary.ReplaceContentsWith(PropertiesToNative(_object.Properties));
                return fullObj;
            }

            return encodedObject;
        }

        /// <summary>
        /// Converts Rhino objects to Speckle objects.
        /// </summary>
        /// <param name="_objects"></param>
        /// <returns></returns>
        public override IEnumerable<SpeckleObject> ToSpeckle(IEnumerable<object> _objects)
        {
            return _objects.Select(o => ToSpeckle(o));
        }

        /// <summary>
        /// Converts Rhino objects to Speckle objects.
        /// </summary>
        /// <param name="_objects"></param>
        /// <returns></returns>
        public override SpeckleObject ToSpeckle(object _object)
        {
            object myObject = _object;
            try
            {
                var preValue = _object.GetType().GetProperty("Value");
                if (preValue != null)
                {
                    var Value = preValue.GetValue(_object, null);
                    if (Value != null) myObject = Value;
                }
            }
            catch { };

            if (myObject == null) return new SpeckleString("Null object.");

            if (myObject is bool)
                return new SpeckleBoolean((bool)myObject);

            if (myObject is double || myObject is float || myObject is int)
                return new SpeckleNumber((double)myObject);

            if (myObject is string)
                return new SpeckleString((string)myObject);

            if (myObject is Interval)
                return ((Interval)myObject).ToSpeckle();

            if (myObject is UVInterval)
                return ((UVInterval)myObject).ToSpeckle();

            //double case points: user data persists on Rhino.Geometry.Points but not on Point3d xdlol
            if (myObject is Rhino.Geometry.Point)
                return ((Rhino.Geometry.Point)myObject).ToSpeckle();
            if (myObject is Point3d)
                return ((Point3d)myObject).ToSpeckle();


            if (myObject is Vector3d)
                return ((Vector3d)myObject).ToSpeckle();

            if (myObject is Plane)
                return ((Plane)myObject).ToSpeckle();

            if (myObject is Curve)
            {
                var crvProps = PropertiesToSpeckle(((Curve)myObject).UserDictionary);
                SpeckleObject obj = new SpeckleObject();

                if (((Curve)myObject).IsLinear())
                {

                    obj = (new Line(((Curve)myObject).PointAtStart, ((Curve)myObject).PointAtEnd)).ToSpeckle();

                }
                else if (((Curve)myObject).IsPolyline())
                {

                    Polyline p = null; ((Curve)myObject).TryGetPolyline(out p);
                    obj = p.ToSpeckle();

                }
                else if (((Curve)myObject).IsCircle())
                {

                    Circle c = new Circle();
                    ((Curve)myObject).TryGetCircle(out c);
                    obj = c.ToSpeckle();

                }
                else if (((Curve)myObject).IsArc())
                {

                    Arc a = new Arc(); ((Curve)myObject).TryGetArc(out a);
                    obj = a.ToSpeckle();

                }
                else if (((Curve)myObject).IsEllipse())
                {

                    Ellipse a = new Ellipse(); ((Curve)myObject).TryGetEllipse(out a);
                    obj = a.ToSpeckle();

                }
                else if (myObject is PolyCurve)
                {

                    obj = ((PolyCurve)myObject).ToSpeckle();

                }
                else
                {
                    obj = ((Curve)myObject).ToNurbsCurve().ToSpeckle();
                }

                obj.Properties = crvProps;
                obj.SetFullHash();

                return obj;
            }

            if (myObject is Line)
                return ((Line)myObject).ToSpeckle();

            if (myObject is Circle)
                return ((Circle)myObject).ToSpeckle();

            if (myObject is Arc)
                return ((Arc)myObject).ToSpeckle();

            if (myObject is Ellipse)
                return ((Ellipse)myObject).ToSpeckle();

            if (myObject is Box)
                return ((Box)myObject).ToSpeckle();
            if (myObject is Rectangle3d)
                return ((Rectangle3d)myObject).ToSpeckle();

            if (myObject is Mesh)
                return ((Mesh)myObject).ToSpeckle();

            if (myObject is Brep)
                return ((Brep)myObject).ToSpeckle();
                
            // TODO: check with rhino sender
            if(myObject is Rhino.DocObjects.ExtrusionObject)
            {
                var b = myObject as Rhino.DocObjects.ExtrusionObject;
                return b.ExtrusionGeometry.ToSpeckle();
            }

            if(myObject is Extrusion)
            {
                return ((Extrusion)myObject).ToSpeckle();
            }

            if (myObject is TextEntity)
                return ((TextEntity)myObject).ToSpeckle();

            if (myObject is TextDot)
                return ((TextDot)myObject).ToSpeckle();
            
            // worst case, fail in a very stupid (potentially dangerous) way:
            return Converter.ToAbstract(myObject);
        }


        /// <summary>
        /// Converts a Speckle object's properties to a native Rhino UserDictionary (ArchivableDictionary).
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static ArchivableDictionary PropertiesToNative(Dictionary<string, object> dict)
        {
            ArchivableDictionary myDictionary = new ArchivableDictionary();
            if (dict == null) return myDictionary;

            using (var converter = new RhinoConverter())
            {
                foreach (var key in dict.Keys)
                {
                    if (dict[key] is Dictionary<string, object>)
                    {
                        myDictionary.Set(key, PropertiesToNative((Dictionary<string, object>)dict[key]));
                    }
                    else if (dict[key] is SpeckleObject)
                    {
                        var converted = converter.ToNative((SpeckleObject)dict[key]);

                        if (converted is GeometryBase)
                            myDictionary.Set(key, (GeometryBase)converted);
                        else if (converted is Interval)
                            myDictionary.Set(key, (Interval)converted);
                        else if (converted is Vector3d)
                            myDictionary.Set(key, (Vector3d)converted);
                        else if (converted is Plane)
                            myDictionary.Set(key, (Plane)converted);
                    }
                    else if (dict[key] is int)
                        myDictionary.Set(key, Convert.ToInt32(dict[key]));
                    else if (dict[key] is double)
                        myDictionary.Set(key, (double)dict[key]);
                    else if (dict[key] is bool)
                        myDictionary.Set(key, (bool)dict[key]);
                    else if (dict[key] is string)
                        myDictionary.Set(key, (string)dict[key]);
                }

            }

            return myDictionary;
        }

        /// <summary>
        /// Converts an object's UserDictionary to a f###ing normal Dictionary.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static Dictionary<string, object> PropertiesToSpeckle(ArchivableDictionary dict)
        {
            Dictionary<string, object> myDictionary = new Dictionary<string, object>();

            using (var converter = new RhinoConverter())
            {
                foreach (var key in dict.Keys)
                {
                    if (dict[key] is ArchivableDictionary)
                        myDictionary.Add(key, PropertiesToSpeckle(dict[key] as ArchivableDictionary));
                    else if (dict[key] is string || dict[key] is double || dict[key] is float || dict[key] is int)
                        myDictionary.Add(key, dict[key]);
                    else
                        myDictionary.Add(key, converter.ToSpeckle(dict[key]));
                }
            }
            return myDictionary;
        }

        public void Dispose()
        {

        }
    }

    /// <summary>
    /// These methods extend both the SpeckleObject types with a .ToRhino() method as well as 
    /// the base RhinoCommon types with a .ToSpeckle() method for easier conversion logic.
    /// </summary>`
    public static class SpeckleTypesExtensions
    {
        // Convenience methods point:
        public static double[] ToArray(this Point3d pt)
        {
            return new double[] { pt.X, pt.Y, pt.Z };
        }

        public static Point3d ToPoint(this double[] arr)
        {
            return new Point3d(arr[0], arr[1], arr[2]);
        }

        // Mass point converter
        public static Point3d[] ToPoints(this double[] arr)
        {
            if (arr.Length % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

            Point3d[] points = new Point3d[arr.Length / 3];
            for (int i = 2, k = 0; i < arr.Length; i += 3)
                points[k++] = new Point3d(arr[i - 2], arr[i - 1], arr[i]);

            return points;
        }

        public static double[] ToFlatArray(this IEnumerable<Point3d> points)
        {
            return points.SelectMany(pt => pt.ToArray()).ToArray();
        }

        // Convenience methods vector:
        public static double[] ToArray(this Vector3d vc)
        {
            return new double[] { vc.X, vc.Y, vc.Z };
        }

        public static Vector3d ToVector(this double[] arr)
        {
            return new Vector3d(arr[0], arr[1], arr[2]);
        }

        // The real deals below: 

        // Points
        public static SpecklePoint ToSpeckle(this Point3d pt)
        {
            return new SpecklePoint(pt.X, pt.Y, pt.Z);
        }

        public static Rhino.Geometry.Point ToRhino(this SpecklePoint pt)
        {
            var myPoint = new Rhino.Geometry.Point(new Point3d(pt.Value[0], pt.Value[1], pt.Value[2]));
            return myPoint;
        }

        public static SpecklePoint ToSpeckle(this Rhino.Geometry.Point pt)
        {
            return new SpecklePoint(pt.Location.X, pt.Location.Y, pt.Location.Z, properties: RhinoConverter.PropertiesToSpeckle(pt.UserDictionary));
        }

        // Vectors
        public static SpeckleVector ToSpeckle(this Vector3d pt)
        {
            return new SpeckleVector(pt.X, pt.Y, pt.Z);
        }

        public static Vector3d ToRhino(this SpeckleVector pt)
        {
            return new Vector3d(pt.Value[0], pt.Value[1], pt.Value[2]);
        }

        // Interval
        public static SpeckleInterval ToSpeckle(this Interval interval)
        {
            return new SpeckleInterval(interval.T0, interval.T1);
        }

        public static Interval ToRhino(this SpeckleInterval interval)
        {
            return new Interval((double)interval.Start, (double)interval.End); ;
        }

        // Interval2d
        public static SpeckleInterval2d ToSpeckle(this UVInterval interval)
        {
            return new SpeckleInterval2d(interval.U.ToSpeckle(), interval.V.ToSpeckle());
        }

        public static UVInterval ToRhino(this SpeckleInterval2d interval)
        {
            return new UVInterval(interval.U.ToRhino(), interval.V.ToRhino());
        }

        // Plane
        public static SpecklePlane ToSpeckle(this Plane plane)
        {
            return new SpecklePlane(plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle());
        }

        public static Plane ToRhino(this SpecklePlane plane)
        {
            var returnPlane = new Plane(plane.Origin.ToRhino().Location, plane.Normal.ToRhino());
            returnPlane.XAxis = plane.Xdir.ToRhino();
            returnPlane.YAxis = plane.Ydir.ToRhino();
            return returnPlane;
        }

        // Line
        public static SpeckleLine ToSpeckle(this Line line)
        {
            return new SpeckleLine(line.From.ToSpeckle(), line.To.ToSpeckle());
        }

        public static SpeckleLine ToSpeckle(this LineCurve line)
        {
            return new SpeckleLine(line.PointAtStart.ToSpeckle(), line.PointAtEnd.ToSpeckle(), properties: RhinoConverter.PropertiesToSpeckle(line.UserDictionary));
        }

        public static NurbsCurve ToRhino(this SpeckleLine line)
        {
            return new Line(line.Start.ToRhino().Location, line.End.ToRhino().Location).ToNurbsCurve();
        }

        // Rectangle
        public static SpeckleRectangle ToSpeckle(this Rectangle3d rect)
        {
            return new SpeckleRectangle(rect.Corner(0).ToSpeckle(), rect.Corner(1).ToSpeckle(), rect.Corner(2).ToSpeckle(), rect.Corner(3).ToSpeckle());
        }

        public static NurbsCurve ToRhino(this SpeckleRectangle rect)
        {
            var myPlane = new Plane(rect.A.ToRhino().Location, new Vector3d(rect.B.ToRhino().Location), new Vector3d(rect.C.ToRhino().Location));
            return new Rectangle3d(myPlane, rect.A.ToRhino().Location, rect.B.ToRhino().Location).ToNurbsCurve();
        }

        // Circle
        public static SpeckleCircle ToSpeckle(this Circle circ)
        {
            return new SpeckleCircle(circ.Center.ToSpeckle(), circ.Normal.ToSpeckle(), circ.Radius);
        }

        public static NurbsCurve ToRhino(this SpeckleCircle circ)
        {
            Circle circle = new Circle(new Plane(circ.Center.ToRhino().Location, circ.Normal.ToRhino()), (double)circ.Radius);
            return circle.ToNurbsCurve();
        }

        // Arc
        public static SpeckleArc ToSpeckle(this Arc a)
        {
            SpeckleArc arc = new SpeckleArc(a.Plane.ToSpeckle(), a.Radius, a.StartAngle, a.EndAngle, a.Angle);
            return arc;
        }

        public static NurbsCurve ToRhino(this SpeckleArc a)
        {
            Arc arc = new Arc(a.Plane.ToRhino(), (double)a.Radius, (double)a.AngleRadians);
            return arc.ToNurbsCurve();
        }

        //Ellipse
        public static SpeckleEllipse ToSpeckle(this Ellipse e)
        {
            return new SpeckleEllipse(e.Plane.ToSpeckle(), e.Radius1, e.Radius2);
        }

        public static NurbsCurve ToRhino(this SpeckleEllipse e)
        {
            Ellipse elp = new Ellipse(e.Plane.ToRhino(), (double)e.FirstRadius, (double)e.SecondRadius);
            return elp.ToNurbsCurve();
        }

        // Box
        public static SpeckleBox ToSpeckle(this Box box)
        {
            return new SpeckleBox(box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle());
        }

        public static Box ToRhino(this SpeckleBox box)
        {
            return new Box(box.BasePlane.ToRhino(), box.XSize.ToRhino(), box.YSize.ToRhino(), box.ZSize.ToRhino());
        }

        // Polyline
        public static SpecklePolyline ToSpeckle(this Polyline poly)
        {
            return new SpecklePolyline(poly.ToFlatArray());
        }

        public static NurbsCurve ToRhino(this SpecklePolyline poly)
        {
            return new Polyline(poly.Value.ToPoints()).ToNurbsCurve();
        }

        // TODO: Polycurve

        public static SpecklePolycurve ToSpeckle(this PolyCurve p)
        {
            SpecklePolycurve myPoly = new SpecklePolycurve();

            p.RemoveNesting();
            var segments = p.Explode();

            using (var rhc = new RhinoConverter())
            {
                myPoly.Segments = segments.Select(s => { return rhc.ToSpeckle(s); }).ToArray();
            }

            return myPoly;
        }

        public static NurbsCurve ToRhino(this SpecklePolycurve p)
        {

            PolyCurve myPolyc = new PolyCurve();
            foreach (var segment in p.Segments)
            {
                if (segment.Type == "Curve")
                    myPolyc.Append(((SpeckleCurve)segment).ToRhino());

                if (segment.Type == "Line")
                    myPolyc.Append(((SpeckleLine)segment).ToRhino());

                if (segment.Type == "Arc")
                    myPolyc.Append(((SpeckleArc)segment).ToRhino());

                if (segment.Type == "Polyline")
                    myPolyc.Append(((SpecklePolyline)segment).ToRhino().ToNurbsCurve());
            }
            return myPolyc.ToNurbsCurve();
        }

        // Curve
        public static SpeckleCurve ToSpeckle(this NurbsCurve curve)
        {
            Polyline poly;
            curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

            var x = new SpeckleCurve(poly.ToSpeckle(), properties: RhinoConverter.PropertiesToSpeckle(curve.UserDictionary));

            x.Weights = curve.Points.Select(ctp => ctp.Weight).ToArray();
            x.Points = curve.Points.Select(ctp => ctp.Location).ToFlatArray();
            x.Knots = curve.Knots.ToArray();
            x.Degree = curve.Degree;
            x.Periodic = curve.IsPeriodic;
            x.Rational = curve.IsRational;
            x.Domain = curve.Domain.ToSpeckle();

            return x;
        }

        public static NurbsCurve ToRhino(this SpeckleCurve curve)
        {
            var ptsList = curve.Points.ToPoints();

            // Bug/feature in Rhino sdk: creating a periodic curve adds two extra stupid points? 
            var myCurve = NurbsCurve.Create(curve.Periodic, curve.Degree, new Point3d[curve.Periodic ? ptsList.Length - 2 : ptsList.Length]);
            myCurve.Domain = curve.Domain.ToRhino();

            for (int i = 0; i < ptsList.Length; i++)
                myCurve.Points.SetPoint(i, ptsList[i].X, ptsList[i].Y, ptsList[i].Z, curve.Weights[i]);

            for (int i = 0; i < curve.Knots.Length; i++)
                myCurve.Knots[i] = curve.Knots[i];

            return myCurve;
        }

        // Meshes
        public static SpeckleMesh ToSpeckle(this Mesh mesh)
        {
            var verts = mesh.Vertices.Select(pt => (Point3d)pt).ToFlatArray();

            var Faces = mesh.Faces.SelectMany(face =>
            {
                if (face.IsQuad) return new int[] { 1, face.A, face.B, face.C, face.D };
                return new int[] { 0, face.A, face.B, face.C };
            }).ToArray();

            var Colors = mesh.VertexColors.Select(cl => cl.ToArgb()).ToArray();
            return new SpeckleMesh(verts, Faces, Colors, properties: RhinoConverter.PropertiesToSpeckle(mesh.UserDictionary));
        }

        public static Mesh ToRhino(this SpeckleMesh mesh)
        {
            Mesh m = new Mesh();
            m.Vertices.AddVertices(mesh.Vertices.ToPoints());

            int i = 0;

            while (i < mesh.Faces.Length)
            {
                if (mesh.Faces[i] == 0)
                { // triangle
                    m.Faces.AddFace(new MeshFace(mesh.Faces[i + 1], mesh.Faces[i + 2], mesh.Faces[i + 3]));
                    i += 4;
                }
                else
                { // quad
                    m.Faces.AddFace(new MeshFace(mesh.Faces[i + 1], mesh.Faces[i + 2], mesh.Faces[i + 3], mesh.Faces[i + 4]));
                    i += 5;
                }
            }

            m.VertexColors.AppendColors(mesh.Colors.Select(c => System.Drawing.Color.FromArgb((int)c)).ToArray());
            return m;
        }

        // Breps
        public static SpeckleBrep ToSpeckle(this Brep brep)
        {
            var joinedMesh = new Mesh();
            Mesh.CreateFromBrep(brep).All(meshPart => { joinedMesh.Append(meshPart); return true; });

            return new SpeckleBrep(displayValue: joinedMesh.ToSpeckle(), base64: Converter.getBase64(brep), provenance: "ON", properties: RhinoConverter.PropertiesToSpeckle(brep.UserDictionary));
        }

        public static Brep ToRhino(this SpeckleBrep brep)
        {
            if (brep.Provenance == "ON")
                return (Brep)Converter.getObjFromString(brep.Base64);
            else
                throw new Exception("Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other.");
        }

        public static SpeckleExtrusion ToSpeckle(this Rhino.Geometry.Extrusion extrusion)
        {
            var myExtrusion = new SpeckleExtrusion(extrusion.Profile3d(0, 0).ToNurbsCurve().ToSpeckle(), extrusion.PathStart.DistanceTo(extrusion.PathEnd), extrusion.IsCappedAtBottom);
            return myExtrusion;
        }

        public static Rhino.Geometry.Extrusion ToRhino( this SpeckleExtrusion extrusion)
        {
            var myExtrusion = Extrusion.Create(extrusion.Profile.ToRhino(), extrusion.Length, extrusion.Capped);
            return myExtrusion;
        }

        // Texts & Annotations
        public static SpeckleAnnotation ToSpeckle(this TextEntity textentity)
        {
            Rhino.DocObjects.Tables.FontTable fontTable = Rhino.RhinoDoc.ActiveDoc.Fonts;
            Rhino.DocObjects.Font font = fontTable[textentity.FontIndex];

            return new SpeckleAnnotation(textentity.Text, textentity.TextHeight, font.FaceName, font.Bold, font.Italic, textentity.Plane.ToSpeckle(), textentity.Plane.Origin.ToSpeckle(), properties: RhinoConverter.PropertiesToSpeckle(textentity.UserDictionary));
        }

        public static SpeckleAnnotation ToSpeckle(this TextDot textdot)
        {
            Rhino.Geometry.Plane plane = Plane.Unset;
            double textHeight = double.NaN;
            string faceName = "";
            bool bold = new bool();
            bool italic = new bool();
            return new SpeckleAnnotation(textdot.Text, textHeight, faceName, bold, italic, plane.ToSpeckle(), textdot.Point.ToSpeckle(), properties: RhinoConverter.PropertiesToSpeckle(textdot.UserDictionary));
        }


        public static TextDot ToRhinoTextDot(this SpeckleAnnotation textdot)
        {
            return new TextDot(textdot.Text, textdot.Location.ToRhino().Location);
        }

        public static TextEntity ToRhinoTextEntity(this SpeckleAnnotation textentity)
        {
            Rhino.DocObjects.Tables.FontTable fontTable = Rhino.RhinoDoc.ActiveDoc.Fonts;


            TextEntity textEntity = new TextEntity();
            textEntity.Text = textentity.Text;
            textEntity.Plane = textentity.Plane.ToRhino();
            textEntity.TextHeight = textentity.TextHeight;
            int fontIndex = fontTable.FindOrCreate(textentity.FaceName, textentity.Bold, textentity.Italic);
            textEntity.FontIndex = fontIndex;
            return textEntity;
        }
    }
}

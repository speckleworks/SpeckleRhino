using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;
using SpeckleCommon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleGhRhConverter
{
    /// <summary>
    /// Handles Grasshopper objects: GH_Brep, GH_Mesh, etc. All non-gh specific converting functions are found in the RhinoGeometryConverter class. 
    /// </summary>
    public class GhRhConveter : SpeckleConverter
    {

        public GhRhConveter() : base()
        {
        }

        // sync method
        public override List<SpeckleObject> convert(IEnumerable<object> objects)
        {
            List<SpeckleObject> convertedObjects = new List<SpeckleObject>();
            foreach (object o in objects)
            {
                var myObj = fromGhRhObject(o);
                convertedObjects.Add(myObj);
            }

            return convertedObjects;
        }

        // async callback
        public override void convertAsync(IEnumerable<object> objects, Action<List<SpeckleObject>> callback)
        {
            callback(this.convert(objects));
        }


        public override List<SpeckleObjectProperties> getObjectProperties(IEnumerable<object> objects)
        {
            var propertiesList = new List<SpeckleObjectProperties>();
            var simpleProps = new List<ArchivableDictionary>();
            int k = 0;
            foreach (object o in objects)
            {
                CommonObject myObj = null;

                GH_Brep brep = o as GH_Brep;
                if (brep != null)
                    myObj = brep.Value;

                GH_Surface srf = o as GH_Surface;
                if (srf != null)
                    myObj = srf.Value;

                GH_Mesh mesh = o as GH_Mesh;
                if (mesh != null)
                    myObj = mesh.Value;

                GH_Curve crv = o as GH_Curve;
                if (crv != null)
                    myObj = crv.Value;

                if (myObj != null)
                    if (myObj.UserDictionary.Keys.Length > 0)
                        propertiesList.Add(new SpeckleObjectProperties(k, myObj.UserDictionary));

                k++;
            }
            return propertiesList;
        }
        
        // encodes
        public override object encodeObject(dynamic obj, dynamic objectProperties = null)
        {
            string type = (string)obj.type;

            object encodedObject = null;

            string serialised = JsonConvert.SerializeObject(obj);

            switch (type)
            {
                case "Number":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleNumber>(serialised).value;
                    break;
                case "Boolean":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleBoolean>(serialised).value;
                    break;
                case "Interval":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleInterval>(serialised).ToRhino();
                    break;
                case "Interval2d":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleInterval2d>(serialised).ToRhino();
                    break;
                case "String":
                    encodedObject = (string) obj.value;
                    break;
                case "Point":
                    encodedObject = JsonConvert.DeserializeObject<SpecklePoint>(serialised).ToRhino();
                    break;
                case "Vector":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleVector>(serialised).ToRhino();
                    break;
                case "Plane":
                    encodedObject = JsonConvert.DeserializeObject<SpecklePlane>(serialised).ToRhino();
                    break;
                case "Line":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleLine>(serialised).ToRhino();
                    break;
                case "Circle":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleCircle>(serialised).ToRhino();
                        break;
                case "Rectangle":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleRectangle>(serialised).ToRhino();
                    break;
                case "Box":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleBox>(serialised).ToRhino();
                    break;
                case "Polyline":
                    encodedObject = JsonConvert.DeserializeObject<SpecklePolyline>(serialised).ToRhino();
                    break;
                case "Curve":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleCurve>(serialised).ToRhino();
                    break;
                case "Brep":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleBrep>(serialised).ToRhino();
                    break;
                case "Mesh":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleMesh>(serialised).ToRhino();
                    break;
                default:
                    encodedObject = obj;
                    break;
            };

            if (objectProperties == null) return encodedObject;

            // try and cast
            CommonObject myObj = null;

            // polyline special cases yay
            if (type == "Polyline")
                myObj = ((Polyline)encodedObject).ToNurbsCurve();
            else
                myObj = encodedObject as CommonObject;

            // if still null get outta here
            if (myObj == null) return encodedObject;

            myObj.UserDictionary.Clear();

            var dict = getArchivableDict((ExpandoObject)objectProperties.properties);
            myObj.UserDictionary.ReplaceContentsWith(dict);

            return myObj;
        }



        private static ArchivableDictionary getArchivableDict(ExpandoObject dict)
        {
            var myDictionary = new ArchivableDictionary();
            foreach (var kvp in dict)
            {
                if (kvp.Value is ExpandoObject) myDictionary.Set(kvp.Key, getArchivableDict(kvp.Value as ExpandoObject));
                else
                {
                    try
                    {
                        myDictionary.Set((string)kvp.Key, Convert.ToInt32(kvp.Value));
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set((string)kvp.Key, (double)kvp.Value);
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set((string)kvp.Key, (bool)kvp.Value);
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set((string)kvp.Key, (string)kvp.Value);
                    }
                    catch { }
                }
            }
            return myDictionary;
        }

        /// <summary>
        /// Determines object type and calls the appropriate conversion call. 
        /// </summary>
        /// <param name="o">Object to convert.</param>
        /// <param name="getEncoded">If set to true, will return a base64 encoded value of more complex objects (ie, breps).</param>
        /// <returns></returns>
        public static SpeckleObject fromGhRhObject(object o)
        {
            GH_Number num = o as GH_Number;
            if (num != null)
                return SpeckleConverter.fromNumber(num.Value);
            if (o is double || o is int)
                return SpeckleConverter.fromNumber((double)o);

            GH_Boolean bul = o as GH_Boolean;
            if (bul != null)
                return SpeckleConverter.fromBoolean(bul.Value);
            if (o is Boolean)
                return GhRhConveter.fromBoolean((bool)o);

            GH_String str = o as GH_String;
            if (str != null)
                return SpeckleConverter.fromString(str.Value);
            if (o is string)
                return SpeckleConverter.fromString((string)o);

            GH_Interval int1d = o as GH_Interval;
            if (int1d != null)
                return int1d.Value.ToSpeckle();
            if (o is Interval)
                return ((Interval)o).ToSpeckle();

            GH_Interval2D int2d = o as GH_Interval2D;
            if (int2d != null)
                return int2d.Value.ToSpeckle();
            if (o is UVInterval)
                return ((UVInterval)o).ToSpeckle();

            GH_Point point = o as GH_Point;
            if (point != null)
                return point.Value.ToSpeckle();
            if (o is Point3d)
                return ((Point3d)o).ToSpeckle();
            if (o is Rhino.Geometry.Point) //wtf???
                return (((Rhino.Geometry.Point)o).Location).ToSpeckle();

            GH_Vector vector = o as GH_Vector;
            if (vector != null)
                return vector.Value.ToSpeckle();
            if (o is Vector3d)
                return ((Vector3d)o).ToSpeckle();

            GH_Plane plane = o as GH_Plane;
            if (plane != null)
                return plane.Value.ToSpeckle();
            if (o is Plane)
                return ((Plane)o).ToSpeckle();

            GH_Line line = o as GH_Line;
            if (line != null)
                return line.Value.ToSpeckle();
            if (o is Line)
                return ((Line)o).ToSpeckle();

            GH_Circle circle = o as GH_Circle;
            if (circle != null)
                return circle.Value.ToSpeckle();
            if (o is Circle)
                return ((Circle)o).ToSpeckle();

            GH_Rectangle rectangle = o as GH_Rectangle;
            if (rectangle != null)
                return rectangle.Value.ToSpeckle();
            if (o is Rectangle3d)
                return ((Rectangle3d)o).ToSpeckle();

            GH_Box box = o as GH_Box;
            if (box != null)
                return box.Value.ToSpeckle();
            if (o is Box)
                return ((Box)o).ToSpeckle();

            GH_Curve curve = o as GH_Curve;
            if (curve != null)
            {
                Polyline poly;
                if (curve.Value.TryGetPolyline(out poly))
                    return poly.ToSpeckle();
                return curve.Value.ToSpeckle();
            }

            if (o is Polyline)
                return ((Polyline)o).ToSpeckle();
            if (o is Curve)
                return ((Curve)o).ToSpeckle();

            GH_Surface surface = o as GH_Surface;
            if (surface != null)
                return surface.Value.ToSpeckle();

            GH_Brep brep = o as GH_Brep;
            if (brep != null)
                return brep.Value.ToSpeckle();
            if (o is Brep)
                return((Brep)o).ToSpeckle();

            GH_Mesh mesh = o as GH_Mesh;
            if (mesh != null)
                return mesh.Value.ToSpeckle();
            if (o is Mesh)
                return ((Mesh)o).ToSpeckle();

            return null;
        }

        #region last things

        public override dynamic description()
        {
            return new
            {
                type = "RhGhConverter"
            };
        }

        #endregion
    }


    /// <summary>
    /// These methods extend both the SpeckleObject types with a .ToRhino() method as well as 
    /// the base RhinoCommon types with a .ToSpeckle() method for easier conversion logic.
    /// </summary>
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
                points[k++] = new Point3d(arr[i-2], arr[i - 1], arr[i]);

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

        public static Point3d ToRhino(this SpecklePoint pt)
        {
            return new Point3d(pt.value[0], pt.value[1], pt.value[2]);
        }

        // Vectors
        public static SpeckleVector ToSpeckle(this Vector3d pt)
        {
            return new SpeckleVector(pt.X, pt.Y, pt.Z);
        }

        public static Vector3d ToRhino(this SpeckleVector pt)
        {
            return new Vector3d(pt.value[0], pt.value[1], pt.value[2]);
        }

        // Interval
        public static SpeckleInterval ToSpeckle(this Interval interval)
        {
            return new SpeckleInterval(interval.T0, interval.T1);
        }

        public static Interval ToRhino(this SpeckleInterval interval)
        {
            return new Interval(interval.start, interval.end);
        }

        // Interval2d
        public static SpeckleInterval2d ToSpeckle(this UVInterval interval)
        {
            return new SpeckleInterval2d(interval.U.ToSpeckle(), interval.V.ToSpeckle());
        }

        public static UVInterval ToRhino(this SpeckleInterval2d interval)
        {
            return new UVInterval(interval.u.ToRhino(), interval.v.ToRhino());
        }

        // Plane
        public static SpecklePlane ToSpeckle(this Plane plane)
        {
            return new SpecklePlane(plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle());
        }
        public static Plane ToRhino(this SpecklePlane plane)
        {
            return new Plane(plane.origin.ToRhino(), plane.normal.ToRhino());
        }

        // Line
        public static SpeckleLine ToSpeckle(this Line line)
        {
            return new SpeckleLine(line.From.ToSpeckle(), line.To.ToSpeckle());
        }

        public static Line ToRhino(this SpeckleLine line)
        {
            return new Line(line.start.ToRhino(), line.end.ToRhino());
        }

        // Rectangle
        public static SpeckleRectangle ToSpeckle(this Rectangle3d rect)
        {
            return new SpeckleRectangle(rect.Corner(0).ToSpeckle(), rect.Corner(1).ToSpeckle(), rect.Corner(2).ToSpeckle(), rect.Corner(3).ToSpeckle());
        }
        public static Rectangle3d ToRhino(this SpeckleRectangle rect)
        {
            var myPlane = new Plane(rect.a.ToRhino(), rect.b.ToRhino(), rect.c.ToRhino());
            return new Rectangle3d(myPlane, rect.a.ToRhino(), rect.c.ToRhino());
        }

        // Circle
        public static SpeckleCircle ToSpeckle(this Circle circ)
        {
            return new SpeckleCircle(circ.Center.ToSpeckle(), circ.Normal.ToSpeckle(), circ.Radius);
        }

        public static Circle ToRhino(this SpeckleCircle circ)
        {
            return new Circle(new Plane(circ.center.ToRhino(), circ.normal.ToRhino()), circ.radius);
        }

        // Box
        public static SpeckleBox ToSpeckle(this Box box)
        {
            return new SpeckleBox(box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle());
        }

        public static Box ToRhino(this SpeckleBox box)
        {
            return new Box(box.basePlane.ToRhino(), box.xSize.ToRhino(), box.ySize.ToRhino(), box.zSize.ToRhino());
        }

        // Polyline
        public static SpecklePolyline ToSpeckle(this Polyline poly)
        {
            return new SpecklePolyline(poly.ToFlatArray());
        }

        public static Polyline ToRhino(this SpecklePolyline poly)
        {
            return new Polyline(poly.value.ToPoints());
        }

        // Curve
        public static SpeckleCurve ToSpeckle( this Curve curve )
        {
            Polyline poly;
            curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

            return new SpeckleCurve(poly.ToSpeckle(), SpeckleConverter.getBase64(curve), "ON");
        }

        public static Curve ToRhino(this SpeckleCurve curve)
        {
            if (curve.provenance == "ON")
                return (Curve)SpeckleConverter.getObjFromString(curve.base64);
            else
                throw new Exception("Unknown curve provenance: " + curve.provenance + ". Don't know how to convert from one to the other.");
        }

        // Meshes
        public static SpeckleMesh ToSpeckle( this Mesh mesh)
        {
            var verts = mesh.Vertices.Select(pt => (Point3d)pt).ToFlatArray();

            var faces = mesh.Faces.SelectMany(face => {
                if (face.IsQuad) return new int[] { 1, face.A, face.B, face.C, face.D };
                return new int[] { 0, face.A, face.B, face.C };
            }).ToArray();

            var colors = mesh.VertexColors.Select(cl => cl.ToArgb()).ToArray();
            return new SpeckleMesh(verts, faces, colors);
        }

        public static Mesh ToRhino(this SpeckleMesh mesh)
        {
            Mesh m = new Mesh();
            m.Vertices.AddVertices(mesh.vertices.ToPoints());

            int i = 0;

            while(i < mesh.faces.Length)
            {
                if(mesh.faces[i] == 0)
                { // triangle
                    m.Faces.AddFace(new MeshFace(mesh.faces[i+1], mesh.faces[i+2], mesh.faces[i+3]));
                    i += 4;
                }else
                { // quad
                    m.Faces.AddFace(new MeshFace(mesh.faces[i + 1], mesh.faces[i + 2], mesh.faces[i + 3], mesh.faces[i+4]));
                    i += 5;
                }
            }

            m.VertexColors.AppendColors(mesh.colors.Select(c => Color.FromArgb(c) ).ToArray());
            return m;
        }

        // Breps
        public static SpeckleBrep ToSpeckle(this Brep brep)
        {
            var joinedMesh = new Mesh();
            Mesh.CreateFromBrep(brep).All(meshPart => { joinedMesh.Append(meshPart); return true; });

            return new SpeckleBrep(joinedMesh.ToSpeckle(), SpeckleConverter.getBase64(brep), "ON");
        }

        public static Brep ToRhino(this SpeckleBrep brep)
        {
            if (brep.provenance == "ON")
                return (Brep)SpeckleConverter.getObjFromString(brep.base64);
            else
                throw new Exception("Unknown brep provenance: " + brep.provenance + ". Don't know how to convert from one to the other.");
        }
    }
}

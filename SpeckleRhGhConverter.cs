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
        public override List<SpeckleObject> Convert(IEnumerable<object> objects)
        {
            List<SpeckleObject> convertedObjects = new List<SpeckleObject>();
            foreach (object o in objects)
            {
                var myObj = FromGhRhObject(o);
                convertedObjects.Add(myObj);
            }

            return convertedObjects;
        }

        // async callback
        public override void ConvertAsync(IEnumerable<object> objects, Action<List<SpeckleObject>> callback)
        {
            callback(Convert(objects));
        }


        public override List<SpeckleObjectProperties> GetObjectProperties(IEnumerable<object> objects)
        {
            var propertiesList = new List<SpeckleObjectProperties>();
            var simpleProps = new List<ArchivableDictionary>();
            int k = 0;
            foreach (object o in objects)
            {
                CommonObject myObj = null;

                if (o is GH_Brep brep)
                    myObj = brep.Value;

                if (o is GH_Surface srf)
                    myObj = srf.Value;

                if (o is GH_Mesh mesh)
                    myObj = mesh.Value;

                if (o is GH_Curve crv)
                    myObj = crv.Value;

                if (myObj != null)
                    if (myObj.UserDictionary.Keys.Length > 0)
                        propertiesList.Add(new SpeckleObjectProperties(k, myObj.UserDictionary));

                k++;
            }
            return propertiesList;
        }
        
        // encodes
        public override object EncodeObject(dynamic obj, dynamic objectProperties = null)
        {
            string type = (string)obj.type;

            object encodedObject = null;

            string serialised = JsonConvert.SerializeObject(obj);

            switch (type)
            {
                case "invalid_object":
                    encodedObject = "Invalid object. Source geometry or data could not be converted.";
                    break;
                case "Number":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleNumber>(serialised).Value;
                    break;
                case "Boolean":
                    encodedObject = JsonConvert.DeserializeObject<SpeckleBoolean>(serialised).Value;
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
                        myDictionary.Set(kvp.Key, System.Convert.ToInt32(kvp.Value));
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set(kvp.Key, (double)kvp.Value);
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set(kvp.Key, (bool)kvp.Value);
                    }
                    catch { }

                    try
                    {
                        myDictionary.Set(kvp.Key, (string)kvp.Value);
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
        public static SpeckleObject FromGhRhObject(object o)
        {
            if (o is GH_Number num)
                return SpeckleConverter.FromNumber(num.Value);
            if (o is double || o is int)
                return SpeckleConverter.FromNumber((double)o);

            if (o is GH_Boolean bul)
                return SpeckleConverter.FromBoolean(bul.Value);
            if (o is Boolean)
                return GhRhConveter.FromBoolean((bool)o);

            if (o is GH_String str)
                return SpeckleConverter.FromString(str.Value);
            if (o is string)
                return SpeckleConverter.FromString((string)o);

            if (o is GH_Interval int1d)
                return int1d.Value.ToSpeckle();
            if (o is Interval)
                return ((Interval)o).ToSpeckle();

            if (o is GH_Interval2D int2d)
                return int2d.Value.ToSpeckle();
            if (o is UVInterval)
                return ((UVInterval)o).ToSpeckle();

            if (o is GH_Point point)
                return point.Value.ToSpeckle();
            if (o is Point3d)
                return ((Point3d)o).ToSpeckle();
            if (o is Rhino.Geometry.Point) //wtf???
                return (((Rhino.Geometry.Point)o).Location).ToSpeckle();

            if (o is GH_Vector vector)
                return vector.Value.ToSpeckle();
            if (o is Vector3d)
                return ((Vector3d)o).ToSpeckle();

            if (o is GH_Plane plane)
                return plane.Value.ToSpeckle();
            if (o is Plane)
                return ((Plane)o).ToSpeckle();

            if (o is GH_Line line)
                return line.Value.ToSpeckle();
            if (o is Line)
                return ((Line)o).ToSpeckle();

            if (o is GH_Circle circle)
                return circle.Value.ToSpeckle();
            if (o is Circle)
                return ((Circle)o).ToSpeckle();

            if (o is GH_Rectangle rectangle)
                return rectangle.Value.ToSpeckle();
            if (o is Rectangle3d)
                return ((Rectangle3d)o).ToSpeckle();

            if (o is GH_Box box)
                return box.Value.ToSpeckle();
            if (o is Box)
                return ((Box)o).ToSpeckle();

            if (o is GH_Curve curve)
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

            if (o is GH_Surface surface)
                return surface.Value.ToSpeckle();

            if (o is GH_Brep brep)
                return brep.Value.ToSpeckle();
            if (o is Brep)
                return((Brep)o).ToSpeckle();

            if (o is GH_Mesh mesh)
                return mesh.Value.ToSpeckle();
            if (o is Mesh)
                return ((Mesh)o).ToSpeckle();

            return null;
        }

        #region last things

        public override dynamic Description()
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
            return new Point3d(pt.Value[0], pt.Value[1], pt.Value[2]);
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
            return new Interval(interval.Start, interval.End);
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
            return new Plane(plane.Origin.ToRhino(), plane.Normal.ToRhino());
        }

        // Line
        public static SpeckleLine ToSpeckle(this Line line)
        {
            return new SpeckleLine(line.From.ToSpeckle(), line.To.ToSpeckle());
        }

        public static Line ToRhino(this SpeckleLine line)
        {
            return new Line(line.Start.ToRhino(), line.End.ToRhino());
        }

        // Rectangle
        public static SpeckleRectangle ToSpeckle(this Rectangle3d rect)
        {
            return new SpeckleRectangle(rect.Corner(0).ToSpeckle(), rect.Corner(1).ToSpeckle(), rect.Corner(2).ToSpeckle(), rect.Corner(3).ToSpeckle());
        }
        public static Rectangle3d ToRhino(this SpeckleRectangle rect)
        {
            var myPlane = new Plane(rect.A.ToRhino(), rect.B.ToRhino(), rect.C.ToRhino());
            return new Rectangle3d(myPlane, rect.A.ToRhino(), rect.C.ToRhino());
        }

        // Circle
        public static SpeckleCircle ToSpeckle(this Circle circ)
        {
            return new SpeckleCircle(circ.Center.ToSpeckle(), circ.Normal.ToSpeckle(), circ.Radius);
        }

        public static Circle ToRhino(this SpeckleCircle circ)
        {
            return new Circle(new Plane(circ.Center.ToRhino(), circ.Normal.ToRhino()), circ.Radius);
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

        public static Polyline ToRhino(this SpecklePolyline poly)
        {
            return new Polyline(poly.Value.ToPoints());
        }

        // Curve
        public static SpeckleCurve ToSpeckle( this Curve curve )
        {
            Polyline poly;
            curve.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly);

            return new SpeckleCurve(poly.ToSpeckle(), SpeckleConverter.GetBase64(curve), "ON");
        }

        public static Curve ToRhino(this SpeckleCurve curve)
        {
            if (curve.Provenance == "ON")
                return (Curve)SpeckleConverter.GetObjFromString(curve.Base64);
            else
                throw new Exception("Unknown curve provenance: " + curve.Provenance + ". Don't know how to convert from one to the other.");
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
            m.Vertices.AddVertices(mesh.Vertices.ToPoints());

            int i = 0;

            while(i < mesh.Faces.Length)
            {
                if(mesh.Faces[i] == 0)
                { // triangle
                    m.Faces.AddFace(new MeshFace(mesh.Faces[i+1], mesh.Faces[i+2], mesh.Faces[i+3]));
                    i += 4;
                }else
                { // quad
                    m.Faces.AddFace(new MeshFace(mesh.Faces[i + 1], mesh.Faces[i + 2], mesh.Faces[i + 3], mesh.Faces[i+4]));
                    i += 5;
                }
            }

            m.VertexColors.AppendColors(mesh.Colors.Select(c => Color.FromArgb(c) ).ToArray());
            return m;
        }

        // Breps
        public static SpeckleBrep ToSpeckle(this Brep brep)
        {
            var joinedMesh = new Mesh();
            Mesh.CreateFromBrep(brep).All(meshPart => { joinedMesh.Append(meshPart); return true; });

            return new SpeckleBrep(joinedMesh.ToSpeckle(), SpeckleConverter.GetBase64(brep), "ON");
        }

        public static Brep ToRhino(this SpeckleBrep brep)
        {
            if (brep.Provenance == "ON")
                return (Brep)SpeckleConverter.GetObjFromString(brep.Base64);
            else
                throw new Exception("Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other.");
        }
    }
}

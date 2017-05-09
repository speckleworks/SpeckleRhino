using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;
using SpeckleCommon;
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

        // encodes object
        public static object encodeObjectFromSpeckle(SpeckleObject obj, dynamic objectProperties = null)
        {
            object encodedObject = null;
            if (obj == null)
                return "Null Object";
            switch (obj.type)
            {
                case "Boolean":
                    encodedObject = ((SpeckleBoolean)obj).value;
                    break;
                case "Number":
                    encodedObject = ((SpeckleNumber)obj).value;
                    break;
                case "String":
                    encodedObject = ((SpeckleString)obj).value;
                    break;
                case "Interval":
                    encodedObject = ((SpeckleInterval)obj).ToRhino();
                    break;
                case "Interval2d":
                    encodedObject = ((SpeckleInterval2d)obj).ToRhino();
                    break;
                case "Point":
                    encodedObject = ((SpecklePoint)obj).ToRhino();
                    break;
                case "Vector":
                    encodedObject = ((SpeckleVector)obj).ToRhino();
                    break;
                case "Plane":
                    encodedObject = ((SpecklePlane)obj).ToRhino();
                    break;
                case "Line":
                    encodedObject = ((SpeckleLine)obj).ToRhino();
                    break;
                case "Rectangle":
                    encodedObject = ((SpeckleRectangle)obj).ToRhino();
                    break;

                default:
                    encodedObject = obj.type;
                    break;
            }

            return encodedObject;
        }
        
        public override object encodeObject(dynamic obj, dynamic objectProperties = null)
        {
            string type = (string)obj.type;
            object encodedObject = null;

            switch (type)
            {
                case "Number":
                    encodedObject = toNumber(obj); break;
                case "Boolean":
                    encodedObject = toBoolean(obj); break;
                case "Interval":
                    encodedObject = toInterval(obj); break;
                case "Interval2d":
                    encodedObject = toInterval2d(obj); break;
                case "String":
                    encodedObject = toString(obj); break;
                case "Point":
                    encodedObject = toPoint(obj); break;
                case "Vector":
                    encodedObject = toVector(obj); break;
                case "Plane":
                    encodedObject = toPlane(obj); break;
                case "Line":
                    encodedObject = toLine(obj); break;
                //case "Arc":
                //    encodedObject = toArc(obj); break;
                //case "Circle":
                //    encodedObject = toCircle(obj); break;
                case "Rectangle":
                    encodedObject = toRectangle(obj); break;
                //case "Box":
                //    encodedObject = toBox(obj); break;
                case "Polyline":
                    encodedObject = toPolyline(obj); break;
                //case "Curve":
                //    encodedObject = toCurve(obj); break;
                //case "Brep":
                //    encodedObject = toBrep(obj); break;
                case "Mesh":
                    encodedObject = toMesh(obj); break;
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
            GH_Interval int1d = o as GH_Interval;
            if (int1d != null)
                return GhRhConveter.fromInterval(int1d.Value);
            if (o is Interval)
                return GhRhConveter.fromInterval((Interval)o);

            GH_Interval2D int2d = o as GH_Interval2D;
            if (int2d != null)
                return GhRhConveter.fromInterval2d(int2d.Value);
            if (o is UVInterval)
                return GhRhConveter.fromInterval2d((UVInterval)o);

            // basic stuff
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

            // simple geometry
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

            //GH_Arc arc = o as GH_Arc;
            //if (arc != null)
            //    return GhRhConveter.fromArc(arc.Value);
            //if (o is Arc)
            //    return GhRhConveter.fromArc((Arc)o);

            //GH_Circle circle = o as GH_Circle;
            //if (circle != null)
            //    return GhRhConveter.fromCircle(circle.Value);
            //if (o is Circle)
            //    return GhRhConveter.fromCircle((Circle)o);

            GH_Rectangle rectangle = o as GH_Rectangle;
            if (rectangle != null)
                return rectangle.Value.ToSpeckle();
            if (o is Rectangle3d)
                return ((Rectangle3d)o).ToSpeckle();

            //GH_Box box = o as GH_Box;
            //if (box != null)
            //    return GhRhConveter.fromBox(box.Value);
            //if (o is Box)
            //    return GhRhConveter.fromBox((Box)o);

            // getting complex 
            GH_Curve curve = o as GH_Curve;
            if (curve != null)
            {
                Polyline poly;
                if (curve.Value.TryGetPolyline(out poly))
                    return GhRhConveter.fromPolyline(poly);
                //return GhRhConveter.fromCurve(curve.Value);
            }

            if (o is Polyline)
                return GhRhConveter.fromPolyline((Polyline)o);
            //if (o is Curve)
            //    return GhRhConveter.fromCurve((Curve)o);

            //GH_Surface surface = o as GH_Surface;
            //if (surface != null)
            //    return GhRhConveter.fromBrep(surface.Value);

            //GH_Brep brep = o as GH_Brep;
            //if (brep != null)
            //    return GhRhConveter.fromBrep(brep.Value);
            //if (o is Brep)
            //    return GhRhConveter.fromBrep((Brep)o);

            GH_Mesh mesh = o as GH_Mesh;
            if (mesh != null)
                return GhRhConveter.fromMesh(mesh.Value);
            if (o is Mesh)
                return GhRhConveter.fromMesh((Mesh)o);

            return new SpeckleObject() { type = "Undefined" };
        }

        #region Rhino Geometry Converters

        public static SpeckleInterval fromInterval(Interval u)
        {
            return new SpeckleInterval(u.Min, u.Max);
        }

        public static Interval toInterval(dynamic obj)
        {
            return new Interval(obj.value[0], obj.value[1]);
        }

        public static SpeckleInterval2d fromInterval2d(UVInterval i)
        {
            return new SpeckleInterval2d(i.U.ToSpeckle(), i.V.ToSpeckle());
        }

        public static UVInterval toInterval2d(dynamic obj)
        {
            return new UVInterval(new Interval(obj.value[0], obj.value[1]), new Interval(obj.value[2], obj.value[3]));
        }

        public static SpecklePoint fromPoint(Point3d o)
        {
            return new SpecklePoint(o.X, o.Y, o.Z);
        }

        public static Point3d toPoint(dynamic o)
        {
            return new Point3d(o.value[0], o.value[1], o.value[2]);
        }

        public static SpeckleVector fromVector(Vector3d o)
        {
            return new SpeckleVector(o.X, o.Y, o.Z);
        }

        public static Vector3d toVector(dynamic o)
        {
            return new Vector3d(o.value[0], o.value[1], o.value[2]);
        }

        public static SpecklePlane fromPlane(Plane p)
        {
            return new SpecklePlane(fromPoint(p.Origin), fromVector(p.Normal), fromVector(p.XAxis), fromVector(p.YAxis));
        }

        public static Plane toPlane(dynamic o)
        {
            return new Plane(toPoint(o.value.origin), toVector(o.value.xdir), toVector(o.value.ydir));
        }

        public static SpeckleLine fromLine(Line o)
        {
            return new SpeckleLine(fromPoint(o.From), fromPoint(o.To));
        }

        public static Line toLine(dynamic o)
        {
            return new Line(fromPoint(o.value.start), fromPoint(o.value.end));
        }

        //public static SpeckleRectangle fromRectangle(Rectangle3d rect)
        //{

        //    //return new SpeckleRectangle(fromPoint(rect.Corner(0)), fromPoint(rect.Corner(1)),
        //        fromPoint(rect.Corner(2)), fromPoint(rect.Corner(3)));
        //}

        public static Rectangle3d toRectangle(dynamic o)
        {
            return new Rectangle3d(new Plane(toPoint(o.value.a), toPoint(o.value.b), toPoint(o.value.c)), toPoint(o.value.a), toPoint(o.value.b));
        }

        public static SpeckleObject fromPolyline(Polyline poly)
        {
            var arr = new List<double>();
            foreach (var pt in poly)
                arr.AddRange(pt.ToArray());

            return new SpecklePolyline(arr.ToArray());
        }

        public static Polyline toPolyline(dynamic o)
        {
            var pts = new List<Point3d>();

            for(int i = 0; i < o.value.Count; i+=3)
                pts.Add(new Point3d(o.value[i], o.value[i + 1], o.value[i + 2]));

            return new Polyline(pts);
        }

        //public static SpeckleObject fromCurve(Curve o, bool getEncoded, bool getAbstract)
        //{
        //    var encodedObj = "RH:" + SpeckleConverter.getBase64(o);

        //    var polyCurve = o.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true);
        //    Polyline poly; polyCurve.TryGetPolyline(out poly);

        //    SpeckleObject obj = new SpeckleObject();
        //    obj.value = new ExpandoObject();
        //    obj.properties = new ExpandoObject();

        //    obj.type = "Curve";
        //    obj.hash = "Curve." + SpeckleConverter.getHash(encodedObj);
        //    obj.value = fromPolyline(poly);
        //    obj.encodedValue = getEncoded ? encodedObj : "";

        //    return obj;
        //}

        //public static Curve toCurve(dynamic o)
        //{
        //    var base64Arr = o.encodedValue.Split(':');
        //    if (base64Arr[0] != "RH") throw new Exception("Conversion Error: Can't convert from " + base64Arr[0] + " to RH. Object hash: " + o.hash);
        //    return getObjFromString(base64Arr[1]) as Curve;
        //}

        public static SpeckleObject fromMesh(Mesh o)
        {
            var encodedObj = "RH:" + SpeckleConverter.getBase64(o);

            SpeckleObject obj = new SpeckleObject();

            var vertices = o.Vertices.Select(pt => ((Point3d)pt).ToArray()).SelectMany(d => d).ToArray() ;


            return new SpeckleMesh(vertices, new double[] { 0, 2 }, new int[] { 1 });
            //obj.value = new ExpandoObject();
            //obj.properties = new ExpandoObject();


            //obj.type = "Mesh";
            //obj.hash = "Mesh." + SpeckleConverter.getHash(encodedObj);

            //obj.value.vertices = o.Vertices.Select(pt => fromPoint(pt));
            //obj.value.faces = o.Faces;
            //obj.value.colors = o.VertexColors.Select(c => c.ToArgb());

            //return obj;
        }

        public static dynamic toMesh(dynamic o)
        {
            return o;
        }

        //public static Mesh toMesh(dynamic o)
        //{
        //    Mesh mesh = new Mesh();
        //    foreach (var pt in o.value.vertices) mesh.Vertices.Add(toPoint(pt));
        //    foreach (var cl in o.value.colors) mesh.VertexColors.Add(Color.FromArgb((int)cl));
        //    foreach (var fa in o.value.faces)
        //        if (fa.C == fa.D) mesh.Faces.AddFace(new MeshFace((int)fa.A, (int)fa.B, (int)fa.C));
        //        else mesh.Faces.AddFace(new MeshFace((int)fa.A, (int)fa.B, (int)fa.C, (int)fa.D));

        //    return mesh;
        //}

        //public static SpeckleObject fromBrep(Brep o, bool getEncoded, bool getAbstract)
        //{
        //    var encodedObj = "RH:" + SpeckleConverter.getBase64(o);
        //    var ms = getMeshFromBrep(o);

        //    SpeckleObject obj = new SpeckleObject();
        //    obj.value = new ExpandoObject();
        //    obj.properties = new ExpandoObject();

        //    obj.type = "Brep";
        //    obj.hash = "Brep." + SpeckleConverter.getHash(encodedObj);
        //    obj.encodedValue = getEncoded ? encodedObj : "";
        //    obj.value.vertices = ms.Vertices;
        //    obj.value.faces = ms.Faces;
        //    obj.value.colors = ms.VertexColors;

        //    return obj;
        //}

        //public static Brep toBrep(dynamic o)
        //{
        //    var base64Arr = o.encodedValue.Split(':');
        //    if (base64Arr[0] != "RH") throw new Exception("Conversion Error: Can't convert from " + base64Arr[0] + " to RH. Object hash: " + o.hash);

        //    return getObjFromString(base64Arr[1]) as Brep;
        //}

        ///// <summary>
        ///// Little utility function
        ///// </summary>
        ///// <param name="b"></param>
        ///// <returns>A joined mesh of all the brep's faces</returns>
        //private static Mesh getMeshFromBrep(Brep b)
        //{
        //    Mesh[] meshes = Mesh.CreateFromBrep(b);
        //    Mesh joinedMesh = new Mesh();
        //    foreach (Mesh m in meshes) joinedMesh.Append(m);
        //    return joinedMesh;
        //}

        //public static SpeckleObject fromArc(Arc arc)
        //{
        //    SpeckleObject obj = new SpeckleObject();
        //    obj.value = new ExpandoObject();
        //    obj.properties = new ExpandoObject();

        //    obj.type = "Arc";
        //    obj.hash = "Arc." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(arc));
        //    obj.value.center = fromPoint(arc.Center);
        //    obj.value.plane = fromPlane(arc.Plane);
        //    obj.value.startAngle = arc.StartAngle;
        //    obj.value.endAngle = arc.EndAngle;
        //    obj.value.startPoint = fromPoint(arc.StartPoint);
        //    obj.value.midPoint = fromPoint(arc.MidPoint);
        //    obj.value.endPoint = fromPoint(arc.EndPoint);

        //    return obj;
        //}

        //public static Arc toArc(dynamic o)
        //{
        //    return new Arc(toPoint(o.value.startPoint), toPoint(o.value.midPoint), toPoint(o.value.endPoint));
        //}

        //public static SpeckleObject fromCircle(Circle circle)
        //{
        //    SpeckleObject obj = new SpeckleObject();
        //    obj.value = new ExpandoObject();

        //    obj.type = "Circle";
        //    obj.hash = "Circle." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(circle));

        //    obj.value.plane = fromPlane(circle.Plane);
        //    obj.value.center = fromPoint(circle.Center);
        //    obj.value.normal = fromVector(circle.Plane.Normal);
        //    obj.value.radius = circle.Radius;

        //    return obj;
        //}

        //public static Circle toCircle(dynamic o)
        //{
        //    return new Circle(toPlane(o.value.plane), o.value.radius);
        //}

        //public static SpeckleObject fromBox(Box box)
        //{
        //    SpeckleObject obj = new SpeckleObject();
        //    obj.value = new ExpandoObject();

        //    obj.type = "Box";
        //    obj.hash = "Box." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(box));
        //    obj.value.center = fromPoint(box.Center);
        //    obj.value.normal = fromVector(box.Plane.Normal); // use fromVector
        //    obj.value.plane = fromPlane(box.Plane); // to use fromPlane
        //    obj.value.X = fromInterval(box.X);
        //    obj.value.Y = fromInterval(box.Y);
        //    obj.value.Z = fromInterval(box.Z);

        //    return obj;
        //}

        //public static Box toBox(dynamic o)
        //{
        //    return new Box(toPlane(o.value.plane), toInterval(o.value.X), toInterval(o.value.Y), toInterval(o.value.Z));
        //}


        #endregion

        #region last things

        public override dynamic description()
        {
            return new
            {
                type = "grasshopper"
            };
        }

        #endregion
    }


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

        // Polyline
        public static SpecklePolyline ToSpeckle(this Polyline poly)
        {
            return new SpecklePolyline(poly.ToFlatArray());
        }
    }
}

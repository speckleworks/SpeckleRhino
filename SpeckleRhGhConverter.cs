using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;
using SpeckleClient;
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
        /// <summary>
        /// Keeps track of objects sent in this session, and sends only the ones unsent so far. 
        /// Reduces some server load and payload size, which makes for more world peace and love in general.
        /// <para>I should consider moving this cache to the client.</para>
        /// </summary>
        HashSet<string> sent = new HashSet<string>();
        HashSet<string> temporaryCahce = new HashSet<string>();

        public GhRhConveter(bool _encodeObjectsToSpeckle, bool _encodeObjectsToNative) : base(_encodeObjectsToSpeckle, _encodeObjectsToNative)
        {
        }

        public override void commitCache()
        {
            foreach (var s in temporaryCahce)
                sent.Add(s);
            temporaryCahce = new HashSet<string>();
        }

        // sync method
        public override List<SpeckleObject> convert(IEnumerable<object> objects)
        {
            List<SpeckleObject> convertedObjects = new List<SpeckleObject>();
            foreach (object o in objects)
            {
                var myObj = fromGhRhObject(o, encodeObjectsToNative, encodeObjectsToSpeckle);

                if (nonHashedTypes.Contains((string)myObj.type))
                {
                    convertedObjects.Add(myObj);
                }
                else
                {
                    var isInCache = sent.Contains((string)myObj.hash);

                    if (!isInCache)
                    {
                        convertedObjects.Add(myObj);
                        temporaryCahce.Add(myObj.hash);
                    }
                    else
                    {
                        var temp = new SpeckleObject();
                        temp.hash = myObj.hash;
                        temp.type = myObj.type;
                        convertedObjects.Add(temp);
                    }
                }
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
        public override object encodeObject(dynamic obj, dynamic objectProperties = null)
        {
            string type = (string)obj.type;
            object encodedObject = null;

            switch (type)
            {
                case "Number":
                    encodedObject = GhRhConveter.toNumber(obj); break;
                case "Boolean":
                    encodedObject = GhRhConveter.toBoolean(obj); break;
                case "String":
                    encodedObject = GhRhConveter.toString(obj); break;
                case "Point":
                    encodedObject = GhRhConveter.toPoint(obj); break;
                case "Vector":
                    encodedObject = GhRhConveter.toVector(obj); break;
                case "Plane":
                    encodedObject = GhRhConveter.toPlane(obj); break;
                case "Line":
                    encodedObject = GhRhConveter.toLine(obj); break;
                case "Arc":
                    encodedObject = GhRhConveter.toArc(obj); break;
                case "Circle":
                    encodedObject = GhRhConveter.toCircle(obj); break;
                case "Rectangle":
                    encodedObject = GhRhConveter.toRectangle(obj); break;
                case "Box":
                    encodedObject = GhRhConveter.toBox(obj); break;
                case "Polyline":
                    encodedObject = GhRhConveter.toPolyline(obj); break;
                case "Curve":
                    encodedObject = GhRhConveter.toCurve(obj); break;
                case "Brep":
                    encodedObject = GhRhConveter.toBrep(obj); break;
                case "Mesh":
                    encodedObject = GhRhConveter.toMesh(obj); break;
                // gh types:
                case "Interval":
                    encodedObject = GhRhConveter.toInterval(obj); break;
                case "Interval2d":
                    encodedObject = GhRhConveter.toInterval2d(obj); break;
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
        private static SpeckleObject fromGhRhObject(object o, bool getEncoded = false, bool getAbstract = true)
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
                return GhRhConveter.fromPoint(point.Value);
            if (o is Point3d)
                return GhRhConveter.fromPoint((Point3d)o);
            // added because when we assign user data to points they get converted to points.
            // the above comment does makes sense. check the sdk.
            if (o is Rhino.Geometry.Point)
                return GhRhConveter.fromPoint(((Rhino.Geometry.Point)o).Location);

            GH_Vector vector = o as GH_Vector;
            if (vector != null)
                return GhRhConveter.fromVector(vector.Value);
            if (o is Vector3d)
                return GhRhConveter.fromVector((Vector3d)o);

            GH_Plane plane = o as GH_Plane;
            if (plane != null)
                return GhRhConveter.fromPlane(plane.Value);
            if (o is Plane)
                return GhRhConveter.fromPlane((Plane)o);

            GH_Line line = o as GH_Line;
            if (line != null)
                return GhRhConveter.fromLine(line.Value);
            if (o is Line)
                return GhRhConveter.fromLine((Line)o);

            GH_Arc arc = o as GH_Arc;
            if (arc != null)
                return GhRhConveter.fromArc(arc.Value);
            if (o is Arc)
                return GhRhConveter.fromArc((Arc)o);

            GH_Circle circle = o as GH_Circle;
            if (circle != null)
                return GhRhConveter.fromCircle(circle.Value);
            if (o is Circle)
                return GhRhConveter.fromCircle((Circle)o);

            GH_Rectangle rectangle = o as GH_Rectangle;
            if (rectangle != null)
                return GhRhConveter.fromRectangle(rectangle.Value);
            if (o is Rectangle3d)
                return GhRhConveter.fromRectangle((Rectangle3d)o);

            GH_Box box = o as GH_Box;
            if (box != null)
                return GhRhConveter.fromBox(box.Value);
            if (o is Box)
                return GhRhConveter.fromBox((Box)o);

            // getting complex 
            GH_Curve curve = o as GH_Curve;
            if (curve != null)
            {
                Polyline poly;
                if (curve.Value.TryGetPolyline(out poly))
                    return GhRhConveter.fromPolyline(poly);
                return GhRhConveter.fromCurve(curve.Value, getEncoded, getAbstract);
            }

            if (o is Polyline)
                return GhRhConveter.fromPolyline((Polyline)o);
            if (o is Curve)
                return GhRhConveter.fromCurve((Curve)o, getEncoded, getAbstract);

            GH_Surface surface = o as GH_Surface;
            if (surface != null)
                return GhRhConveter.fromBrep(surface.Value, getEncoded, getAbstract);

            GH_Brep brep = o as GH_Brep;
            if (brep != null)
                return GhRhConveter.fromBrep(brep.Value, getEncoded, getAbstract);

            if (o is Brep)
                return GhRhConveter.fromBrep((Brep)o, getEncoded, getAbstract);

            GH_Mesh mesh = o as GH_Mesh;
            if (mesh != null)
                return GhRhConveter.fromMesh(mesh.Value);
            if (o is Mesh)
                return GhRhConveter.fromMesh((Mesh)o);

            return new SpeckleObject() { hash = "404", value = "Type not supported", type = "404" };
        }

        #region Rhino Geometry Converters

        public static SpeckleObject fromPoint(Point3d o)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();

            obj.type = "Point";
            obj.hash = "NoHash";
            obj.value.x = o.X;
            obj.value.y = o.Y;
            obj.value.z = o.Z;
            return obj;
        }

        public static Point3d toPoint(dynamic o)
        {
            return new Point3d(o.value.x, o.value.y, o.value.z);
        }

        public static SpeckleObject fromVector(Vector3d o)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();

            obj.type = "Vector";
            obj.hash = "NoHash";
            obj.value.x = o.X;
            obj.value.y = o.Y;
            obj.value.z = o.Z;

            return obj;
        }

        public static Vector3d toVector(dynamic o)
        {
            return new Vector3d(o.value.x, o.value.y, o.value.z);
        }

        public static SpeckleObject fromPlane(Plane p)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();

            obj.type = "Plane";
            obj.hash = "Plane." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(p));
            obj.value.origin = fromPoint(p.Origin);
            obj.value.xdir = fromVector(p.XAxis);
            obj.value.ydir = fromVector(p.YAxis);
            obj.value.normal = fromVector(p.Normal);

            return obj;
        }

        public static Plane toPlane(dynamic o)
        {
            return new Plane(toPoint(o.value.origin), toVector(o.value.xdir), toVector(o.value.ydir));
        }

        public static SpeckleObject fromLine(Line o)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Line";
            obj.hash = "NoHash";

            obj.value.start = fromPoint(o.From);
            obj.value.end = fromPoint(o.To);

            obj.properties.length = o.Length;
            return obj;
        }

        public static Line toLine(dynamic o)
        {
            return new Line(fromPoint(o.value.start), fromPoint(o.value.end));
        }

        public static SpeckleObject fromPolyline(Polyline poly)
        {
            var encodedObj = "RH:" + SpeckleConverter.getBase64(poly.ToNurbsCurve());
            Rhino.Geometry.AreaMassProperties areaProps = Rhino.Geometry.AreaMassProperties.Compute(poly.ToNurbsCurve());

            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Polyline";
            obj.hash = "Polyline." + SpeckleConverter.getHash(encodedObj);

            obj.value = poly.Select(pt => fromPoint(pt));
            obj.properties.length = poly.Length;
            obj.properties.area = areaProps != null ? areaProps.Area : 0;
            obj.properties.areaCentroid = areaProps != null ? fromPoint(areaProps.Centroid) : null;

            return obj;
        }

        public static Polyline toPolyline(dynamic o)
        {
            var pts = new List<Point3d>();
            foreach (var pt in o.value) pts.Add(toPoint(pt));
            return new Polyline(pts);
        }

        public static SpeckleObject fromCurve(Curve o, bool getEncoded, bool getAbstract)
        {
            var encodedObj = "RH:" + SpeckleConverter.getBase64(o);
            Rhino.Geometry.AreaMassProperties areaProps = Rhino.Geometry.AreaMassProperties.Compute(o);
            var polyCurve = o.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true);
            Polyline poly; polyCurve.TryGetPolyline(out poly);

            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Curve";
            obj.hash = "Curve." + SpeckleConverter.getHash(encodedObj);
            obj.value = fromPolyline(poly);
            obj.encodedValue = getEncoded ? encodedObj : "";
            obj.properties.length = o.GetLength();
            obj.properties.area = areaProps != null ? areaProps.Area : 0;
            obj.properties.areaCentroid = areaProps != null ? fromPoint(areaProps.Centroid) : null;

            return obj;
        }

        public static Curve toCurve(dynamic o)
        {
            var base64Arr = o.encodedValue.Split(':');
            if (base64Arr[0] != "RH") throw new Exception("Conversion Error: Can't convert from " + base64Arr[0] + " to RH. Object hash: " + o.hash);
            return getObjFromString(base64Arr[1]) as Curve;
        }

        public static SpeckleObject fromMesh(Mesh o)
        {
            var encodedObj = "RH:" + SpeckleConverter.getBase64(o);
            Rhino.Geometry.VolumeMassProperties volumeProps = Rhino.Geometry.VolumeMassProperties.Compute(o);
            Rhino.Geometry.AreaMassProperties areaProps = Rhino.Geometry.AreaMassProperties.Compute(o);

            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();


            obj.type = "Mesh";
            obj.hash = "Mesh." + SpeckleConverter.getHash(encodedObj);

            obj.value.vertices = o.Vertices.Select(pt => fromPoint(pt));
            obj.value.faces = o.Faces;
            obj.value.colors = o.VertexColors.Select(c => c.ToArgb());
            //o.UserData
            obj.properties.volume = volumeProps.Volume;
            obj.properties.area = areaProps.Area;
            obj.properties.volumeCentroid = fromPoint(volumeProps.Centroid);
            obj.properties.areaCentroid = fromPoint(areaProps.Centroid);

            return obj;


        }

        public static Mesh toMesh(dynamic o)
        {
            Mesh mesh = new Mesh();
            foreach (var pt in o.value.vertices) mesh.Vertices.Add(toPoint(pt));
            foreach (var cl in o.value.colors) mesh.VertexColors.Add(Color.FromArgb((int)cl));
            foreach (var fa in o.value.faces)
                if (fa.C == fa.D) mesh.Faces.AddFace(new MeshFace((int)fa.A, (int) fa.B, (int) fa.C));
                else mesh.Faces.AddFace(new MeshFace((int)fa.A, (int)fa.B, (int)fa.C, (int)fa.D));

            return mesh;
        }

        public static SpeckleObject fromBrep(Brep o, bool getEncoded, bool getAbstract)
        {
            var encodedObj = "RH:" + SpeckleConverter.getBase64(o);
            var ms = getMeshFromBrep(o);

            Rhino.Geometry.VolumeMassProperties volumeProps = Rhino.Geometry.VolumeMassProperties.Compute(o);
            Rhino.Geometry.AreaMassProperties areaProps = Rhino.Geometry.AreaMassProperties.Compute(o);

            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Brep";
            obj.hash = "Brep." + SpeckleConverter.getHash(encodedObj);
            obj.encodedValue = getEncoded ? encodedObj : "";
            obj.value.vertices = ms.Vertices;
            obj.value.faces = ms.Faces;
            obj.value.colors = ms.VertexColors;
            obj.properties.volume = volumeProps.Volume;
            obj.properties.area = areaProps.Area;
            obj.properties.volumeCentroid = fromPoint(volumeProps.Centroid);
            obj.properties.areaCentroid = fromPoint(areaProps.Centroid);

            return obj;
        }

        public static Brep toBrep(dynamic o)
        {
            var base64Arr = o.encodedValue.Split(':');
            if (base64Arr[0] != "RH") throw new Exception("Conversion Error: Can't convert from " + base64Arr[0] + " to RH. Object hash: " + o.hash);

            return getObjFromString(base64Arr[1]) as Brep;
        }

        /// <summary>
        /// Little utility function
        /// </summary>
        /// <param name="b"></param>
        /// <returns>A joined mesh of all the brep's faces</returns>
        private static Mesh getMeshFromBrep(Brep b)
        {
            Mesh[] meshes = Mesh.CreateFromBrep(b);
            Mesh joinedMesh = new Mesh();
            foreach (Mesh m in meshes) joinedMesh.Append(m);
            return joinedMesh;
        }

        public static SpeckleObject fromArc(Arc arc)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Arc";
            obj.hash = "Arc." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(arc));
            obj.value.center = fromPoint(arc.Center);
            obj.value.plane = fromPlane(arc.Plane);
            obj.value.startAngle = arc.StartAngle;
            obj.value.endAngle = arc.EndAngle;
            obj.value.startPoint = fromPoint(arc.StartPoint);
            obj.value.midPoint = fromPoint(arc.MidPoint);
            obj.value.endPoint = fromPoint(arc.EndPoint);
            obj.properties.length = arc.Length;

            return obj;
        }

        public static Arc toArc(dynamic o)
        {
            return new Arc(toPoint(o.value.startPoint), toPoint(o.value.midPoint), toPoint(o.value.endPoint));
        }

        public static SpeckleObject fromCircle(Circle circle)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Circle";
            obj.hash = "Circle." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(circle));

            obj.value.plane = fromPlane(circle.Plane);
            obj.value.center = fromPoint(circle.Center);
            obj.value.normal = fromVector(circle.Plane.Normal);
            obj.value.radius = circle.Radius;

            obj.properties.length = circle.Circumference;

            return obj;
        }

        public static Circle toCircle(dynamic o)
        {
            return new Circle(toPlane(o.value.plane), o.value.radius);
        }

        public static SpeckleObject fromRectangle(Rectangle3d rect)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Rectangle";
            obj.hash = "Rectangle." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(rect));
            obj.value.A = fromPoint(rect.Corner(0));
            obj.value.B = fromPoint(rect.Corner(1));
            obj.value.C = fromPoint(rect.Corner(2));
            obj.value.D = fromPoint(rect.Corner(3));
            obj.value.plane = fromPlane(rect.Plane);

            obj.properties.length = rect.Circumference;
            obj.properties.area = rect.Area;

            return obj;
        }

        public static Rectangle3d toRectangle(dynamic o)
        {
            return new Rectangle3d(toPlane(o.value.plane), toPoint(o.value.A), toPoint(o.value.C));
        }

        public static SpeckleObject fromBox(Box box)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();
            obj.properties = new ExpandoObject();

            obj.type = "Box";
            obj.hash = "Box." + SpeckleConverter.getHash("RH:" + SpeckleConverter.getBase64(box));
            obj.value.center = fromPoint(box.Center);
            obj.value.normal = fromVector(box.Plane.Normal); // use fromVector
            obj.value.plane = fromPlane(box.Plane); // to use fromPlane
            obj.value.X = fromInterval(box.X);
            obj.value.Y = fromInterval(box.Y);
            obj.value.Z = fromInterval(box.Z);
            obj.properties.area = box.Area;
            obj.properties.volume = box.Volume;

            return obj;
        }

        public static Box toBox(dynamic o)
        {
            return new Box(toPlane(o.value.plane), toInterval(o.value.X), toInterval(o.value.Y), toInterval(o.value.Z));
        }

        public static SpeckleObject fromInterval(Interval u)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();

            obj.type = "Interval";
            obj.hash = "NoHash";
            obj.value.min = u.Min;
            obj.value.max = u.Max;

            return obj;
        }

        public static Interval toInterval(dynamic obj)
        {
            return new Interval(obj.value.min, obj.value.max);
        }

        public static SpeckleObject fromInterval2d(UVInterval i)
        {
            SpeckleObject obj = new SpeckleObject();
            obj.value = new ExpandoObject();

            obj.type = "Interval2d";
            obj.hash = "NoHash";
            obj.value.u = fromInterval(i.U);
            obj.value.v = fromInterval(i.V);
            return obj;
        }

        public static UVInterval toInterval2d(dynamic obj)
        {
            return new UVInterval(toInterval(obj.u), toInterval(obj.v));
        }


        #endregion

        #region last things

        public override dynamic description()
        {
            return new
            {
                type = "grasshopper",
                encodeObjectsToNative = encodeObjectsToNative,
                encodeObjectsToSpeckle = encodeObjectsToSpeckle
            };
        }

        #endregion
    }
}

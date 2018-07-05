using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Runtime;

using SpeckleCore;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace SpeckleRhinoConverter
{
  public class ConverterHack { /*makes sure the assembly is loaded*/  public ConverterHack( ) { } }

  /// <summary>
  /// These methods extend both the SpeckleObject types with a .ToNative() method as well as 
  /// the base RhinoCommon types with a .ToSpeckle() method for easier conversion logic.
  /// </summary>`
  public static class SpeckleRhinoConverter
  {

    public static bool SetBrepDisplayMesh = true;
    public static bool AddMeshTextureCoordinates = false;
    public static bool AddRhinoObjectProperties = false;
    public static bool AddBasicLengthAreaVolumeProperties = false;

    // Dictionaries & ArchivableDictionaries
    public static Dictionary<string, object> ToSpeckle( this ArchivableDictionary dict, Dictionary<int, string> traversed = null, string path = "root", GeometryBase root = null )
    {
      if ( dict.Values.Length == 0 ) return null;
      if ( dict == null ) return null;

      if ( traversed == null )
      {
        traversed = new Dictionary<int, string>();
        traversed.Add( root.GetHashCode(), "root" );
      }

      Dictionary<string, object> myDictionary = new Dictionary<string, object>();

      foreach ( var key in dict.Keys )
      {
        var myObj = dict[ key ];
        if ( traversed.ContainsKey( myObj.GetHashCode() ) )
        {
          myDictionary.Add( key, new SpeckleAbstract() { _type = "ref", _ref = traversed[ myObj.GetHashCode() ] } );
          continue;
        }

        traversed.Add( myObj.GetHashCode(), path + "/" + key );

        if ( dict[ key ] is ArchivableDictionary )
          myDictionary.Add( key, ( ( ArchivableDictionary ) dict[ key ] ).ToSpeckle( traversed, path + "/" + key, root ) );
        else if ( dict[ key ] is string || dict[ key ] is double || dict[ key ] is float || dict[ key ] is int || dict[ key ] is SpeckleObject )
          myDictionary.Add( key, dict[ key ] );
        else if ( dict[ key ] is IEnumerable )
        {
          myDictionary.Add( key, "enums not supported yet." );
        }
        else
        {
          if ( dict[ key ] is GeometryBase )
          {
            GeometryBase obj = dict[ key ] as GeometryBase;
            ArchivableDictionary dictCopy = obj.UserDictionary.Clone();
            obj.UserDictionary.Clear();
            SpeckleObject conv = SpeckleCore.Converter.Serialise( obj );
            conv.Properties = dictCopy.ToSpeckle( traversed, path + "/" + key, root );
            conv.GenerateHash();
            myDictionary.Add( key, conv );
            obj.UserDictionary.ReplaceContentsWith( dictCopy );
          }
          else
          {
            myDictionary.Add( key, SpeckleCore.Converter.Serialise( dict[ key ] ) );
          }
        }
      }
      return myDictionary;
    }

    public static ArchivableDictionary ToNative( this Dictionary<string, object> dict )
    {
      ArchivableDictionary myDictionary = new ArchivableDictionary();
      if ( dict == null ) return myDictionary;

      foreach ( var key in dict.Keys )
      {
        if ( dict[ key ] is Dictionary<string, object> )
        {
          myDictionary.Set( key, ( ( Dictionary<string, object> ) dict[ key ] ).ToNative() );
        }
        else if ( dict[ key ] is SpeckleObject )
        {
          var converted = SpeckleCore.Converter.Deserialise( ( SpeckleObject ) dict[ key ] );

          if ( converted is GeometryBase )
            myDictionary.Set( key, ( GeometryBase ) converted );
          else if ( converted is Interval )
            myDictionary.Set( key, ( Interval ) converted );
          else if ( converted is Vector3d )
            myDictionary.Set( key, ( Vector3d ) converted );
          else if ( converted is Plane )
            myDictionary.Set( key, ( Plane ) converted );
        }
        else if ( dict[ key ] is int )
          myDictionary.Set( key, Convert.ToInt32( dict[ key ] ) );
        else if ( dict[ key ] is double )
          myDictionary.Set( key, ( double ) dict[ key ] );
        else if ( dict[ key ] is bool )
          myDictionary.Set( key, ( bool ) dict[ key ] );
        else if ( dict[ key ] is string )
          myDictionary.Set( key, ( string ) dict[ key ] );
      }
      return myDictionary;
    }

    // Convenience methods point:
    public static double[ ] ToArray( this Point3d pt )
    {
      return new double[ ] { pt.X, pt.Y, pt.Z };
    }

    public static double[ ] ToArray( this Point2d pt )
    {
      return new double[ ] { pt.X, pt.Y };
    }

    public static double[ ] ToArray( this Point2f pt )
    {
      return new double[ ] { pt.X, pt.Y };
    }

    public static Point3d ToPoint( this double[ ] arr )
    {
      return new Point3d( arr[ 0 ], arr[ 1 ], arr[ 2 ] );
    }

    // numbers
    public static SpeckleNumber ToSpeckle( this float num )
    {
      return new SpeckleNumber( num );
    }

    public static SpeckleNumber ToSpeckle( this long num )
    {
      return new SpeckleNumber( num );
    }

    public static SpeckleNumber ToSpeckle( this int num )
    {
      return new SpeckleNumber( num );
    }

    public static SpeckleNumber ToSpeckle( this double num )
    {
      return new SpeckleNumber( num );
    }

    public static double? ToNative( this SpeckleNumber num )
    {
      return num.Value;
    }

    // booleans 
    public static SpeckleBoolean ToSpeckle( this bool b )
    {
      return new SpeckleBoolean( b );
    }

    public static bool? ToNative( this SpeckleBoolean b )
    {
      return b.Value;
    }

    // strings
    public static SpeckleString ToSpeckle( this string b )
    {
      return new SpeckleString( b );
    }

    public static string ToNative( this SpeckleString b )
    {
      return b.Value;
    }

    // Mass point converter
    public static Point3d[ ] ToPoints( this IEnumerable<double> arr )
    {
      if ( arr.Count() % 3 != 0 ) throw new Exception( "Array malformed: length%3 != 0." );

      Point3d[ ] points = new Point3d[ arr.Count() / 3 ];
      var asArray = arr.ToArray();
      for ( int i = 2, k = 0; i < arr.Count(); i += 3 )
        points[ k++ ] = new Point3d( asArray[ i - 2 ], asArray[ i - 1 ], asArray[ i ] );

      return points;
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point3d> points )
    {
      return points.SelectMany( pt => pt.ToArray() ).ToArray();
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point2f> points )
    {
      return points.SelectMany( pt => pt.ToArray() ).ToArray();
    }

    // Convenience methods vector:
    public static double[ ] ToArray( this Vector3d vc )
    {
      return new double[ ] { vc.X, vc.Y, vc.Z };
    }

    public static Vector3d ToVector( this double[ ] arr )
    {
      return new Vector3d( arr[ 0 ], arr[ 1 ], arr[ 2 ] );
    }

    // Points
    // GhCapture?
    public static SpecklePoint ToSpeckle( this Point3d pt )
    {
      return new SpecklePoint( pt.X, pt.Y, pt.Z );
    }
    // Rh Capture?
    public static Rhino.Geometry.Point ToNative( this SpecklePoint pt )
    {
      var myPoint = new Rhino.Geometry.Point( new Point3d( pt.Value[ 0 ], pt.Value[ 1 ], pt.Value[ 2 ] ) );
      myPoint.UserDictionary.ReplaceContentsWith( pt.Properties.ToNative() );
      return myPoint;
    }

    public static SpecklePoint ToSpeckle( this Rhino.Geometry.Point pt )
    {
      return new SpecklePoint( pt.Location.X, pt.Location.Y, pt.Location.Z, properties: pt.UserDictionary.Count != 0 ? pt.UserDictionary.ToSpeckle( root: pt ) : null );
    }

    // Vectors
    public static SpeckleVector ToSpeckle( this Vector3d pt )
    {
      return new SpeckleVector( pt.X, pt.Y, pt.Z );
    }

    public static Vector3d ToNative( this SpeckleVector pt )
    {
      return new Vector3d( pt.Value[ 0 ], pt.Value[ 1 ], pt.Value[ 2 ] );
    }

    // Interval
    public static SpeckleInterval ToSpeckle( this Interval interval )
    {
      return new SpeckleInterval( interval.T0, interval.T1 );
    }

    public static Interval ToNative( this SpeckleInterval interval )
    {
      return new Interval( ( double ) interval.Start, ( double ) interval.End );
    }

    // Interval2d
    public static SpeckleInterval2d ToSpeckle( this UVInterval interval )
    {
      return new SpeckleInterval2d( interval.U.ToSpeckle(), interval.V.ToSpeckle() );
    }

    public static UVInterval ToNative( this SpeckleInterval2d interval )
    {
      return new UVInterval( interval.U.ToNative(), interval.V.ToNative() );
    }

    // Plane
    public static SpecklePlane ToSpeckle( this Plane plane )
    {
      return new SpecklePlane( plane.Origin.ToSpeckle(), plane.Normal.ToSpeckle(), plane.XAxis.ToSpeckle(), plane.YAxis.ToSpeckle() );
    }

    public static Plane ToNative( this SpecklePlane plane )
    {
      var returnPlane = new Plane( plane.Origin.ToNative().Location, plane.Normal.ToNative() );
      returnPlane.XAxis = plane.Xdir.ToNative();
      returnPlane.YAxis = plane.Ydir.ToNative();
      return returnPlane;
    }

    #region LifeSucks

    // Line
    // Gh Line capture
    public static SpeckleLine ToSpeckle( this Line line )
    {
      return new SpeckleLine( ( new Point3d[ ] { line.From, line.To } ).ToFlatArray() );
    }

    // Rh Line capture
    public static SpeckleLine ToSpeckle( this LineCurve line )
    {
      return new SpeckleLine( ( new Point3d[ ] { line.PointAtStart, line.PointAtEnd } ).ToFlatArray(), properties: line.UserDictionary.ToSpeckle( root: line ) ) { Domain = line.Domain.ToSpeckle() };
    }

    // Back again only to LINECURVES because we hate grasshopper and its dealings with rhinocommon
    public static LineCurve ToNative( this SpeckleLine line )
    {
      var pts = line.Value.ToPoints();
      var myLine = new LineCurve( pts[ 0 ], pts[ 1 ] );
      if ( line.Domain != null )
        myLine.Domain = line.Domain.ToNative();
      myLine.UserDictionary.ReplaceContentsWith( line.Properties.ToNative() );
      return myLine;
    }

    // Rectangles now and forever forward will become polylines
    public static SpecklePolyline ToSpeckle( this Rectangle3d rect )
    {
      return new SpecklePolyline( ( new Point3d[ ] { rect.Corner( 0 ), rect.Corner( 1 ), rect.Corner( 2 ), rect.Corner( 3 ) } ).ToFlatArray() ) { Closed = true };
    }

    // Circle
    // Gh Capture
    public static SpeckleCircle ToSpeckle( this Circle circ )
    {
      var circle = new SpeckleCircle( circ.Plane.ToSpeckle(), circ.Radius );
      return circle;
    }

    public static ArcCurve ToNative( this SpeckleCircle circ )
    {
      Circle circle = new Circle( circ.Plane.ToNative(), ( double ) circ.Radius );

      var myCircle = new ArcCurve( circle );
      if ( circ.Domain != null )
        myCircle.Domain = circ.Domain.ToNative();
      myCircle.UserDictionary.ReplaceContentsWith( circ.Properties.ToNative() );

      return myCircle;
    }

    // Arc
    // Rh Capture can be a circle OR an arc
    public static SpeckleObject ToSpeckle( this ArcCurve a )
    {
      if ( a.IsClosed )
      {
        Circle preCircle;
        a.TryGetCircle( out preCircle );
        SpeckleCircle myCircle = preCircle.ToSpeckle();
        myCircle.Domain = a.Domain.ToSpeckle();
        myCircle.Properties = a.UserDictionary.ToSpeckle( root: a );
        myCircle.GenerateHash();
        return myCircle;
      }
      else
      {
        Arc preArc;
        a.TryGetArc( out preArc );
        SpeckleArc myArc = preArc.ToSpeckle();
        myArc.Domain = a.Domain.ToSpeckle();
        myArc.Properties = a.UserDictionary.ToSpeckle( root: a );
        myArc.GenerateHash();
        return myArc;
      }
    }

    // Gh Capture
    public static SpeckleArc ToSpeckle( this Arc a )
    {
      SpeckleArc arc = new SpeckleArc( a.Plane.ToSpeckle(), a.Radius, a.StartAngle, a.EndAngle, a.Angle );
      return arc;
    }

    public static ArcCurve ToNative( this SpeckleArc a )
    {
      Arc arc = new Arc( a.Plane.ToNative(), ( double ) a.Radius, ( double ) a.AngleRadians );
      arc.StartAngle = ( double ) a.StartAngle;
      arc.EndAngle = ( double ) a.EndAngle;
      var myArc = new ArcCurve( arc );
      if ( a.Domain != null )
        myArc.Domain = a.Domain.ToNative();
      myArc.UserDictionary.ReplaceContentsWith( a.Properties.ToNative() );
      return myArc;
    }

    //Ellipse
    public static SpeckleEllipse ToSpeckle( this Ellipse e )
    {
      return new SpeckleEllipse( e.Plane.ToSpeckle(), e.Radius1, e.Radius2 );
    }

    public static NurbsCurve ToNative( this SpeckleEllipse e )
    {
      Ellipse elp = new Ellipse( e.Plane.ToNative(), ( double ) e.FirstRadius, ( double ) e.SecondRadius );


      var myEllp = NurbsCurve.CreateFromEllipse( elp );
      var shit = myEllp.IsEllipse( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance );

      if ( e.Domain != null )
        myEllp.Domain = e.Domain.ToNative();
      myEllp.UserDictionary.ReplaceContentsWith( e.Properties.ToNative() );

      return myEllp;
    }

    // Polyline

    // Gh Capture
    public static SpeckleObject ToSpeckle( this Polyline poly )
    {
      if ( poly.Count == 2 )
        return new SpeckleLine( poly.ToFlatArray() );

      var myPoly = new SpecklePolyline( poly.ToFlatArray() );
      myPoly.Closed = poly.IsClosed;

      if ( myPoly.Closed )
        myPoly.Value.RemoveRange( myPoly.Value.Count - 3, 3 );

      return myPoly;
    }

    // Rh Capture
    public static SpeckleObject ToSpeckle( this PolylineCurve poly )
    {
      Polyline polyline;

      if ( poly.TryGetPolyline( out polyline ) )
      {
        if ( polyline.Count == 2 )
          return new SpeckleLine( polyline.ToFlatArray(), null, poly.UserDictionary.ToSpeckle( root: poly ) );

        var myPoly = new SpecklePolyline( polyline.ToFlatArray() );
        myPoly.Closed = polyline.IsClosed;

        if ( myPoly.Closed )
          myPoly.Value.RemoveRange( myPoly.Value.Count - 3, 3 );

        myPoly.Domain = poly.Domain.ToSpeckle();
        myPoly.Properties = poly.UserDictionary.ToSpeckle( root: poly );
        return myPoly;
      }
      return null;
    }

    // Deserialise
    public static PolylineCurve ToNative( this SpecklePolyline poly )
    {
      var points = poly.Value.ToPoints().ToList();
      if ( poly.Closed ) points.Add( points[ 0 ] );

      var myPoly = new PolylineCurve( points );
      if ( poly.Domain != null )
        myPoly.Domain = poly.Domain.ToNative();
      myPoly.UserDictionary.ReplaceContentsWith( poly.Properties.ToNative() );
      return myPoly;
    }

    // Polycurve
    // Rh Capture/Gh Capture
    public static SpecklePolycurve ToSpeckle( this PolyCurve p )
    {
      SpecklePolycurve myPoly = new SpecklePolycurve();
      myPoly.Closed = p.IsClosed;
      myPoly.Domain = p.Domain.ToSpeckle();

      var segments = new List<Curve>();
      CurveSegments( segments, p, true );

      //myPoly.Segments = segments.Select( s => { return ( ( NurbsCurve ) s ).ToSpeckle(); } ).ToList();
      myPoly.Segments = segments.Select( s => { return SpeckleCore.Converter.Serialise( s ); } ).ToList();

      myPoly.Properties = p.UserDictionary.ToSpeckle( root: p );
      myPoly.GenerateHash();

      return myPoly;
    }



    public static PolyCurve ToNative( this SpecklePolycurve p )
    {
      PolyCurve myPolyc = new PolyCurve();
      foreach ( var segment in p.Segments )
      {
        switch ( segment )
        {
          case SpeckleCore.SpeckleCurve crv:
            myPolyc.Append( crv.ToNative() );
            break;
          case SpeckleCore.SpeckleLine crv:
            myPolyc.Append( crv.ToNative() );
            break;
          case SpeckleCore.SpeckleArc crv:
            myPolyc.Append( crv.ToNative() );
            break;
          case SpeckleCore.SpecklePolyline crv:
            myPolyc.Append( crv.ToNative() );
            break;
        }
      }
      myPolyc.UserDictionary.ReplaceContentsWith( p.Properties.ToNative() );
      if ( p.Domain != null )
        myPolyc.Domain = p.Domain.ToNative();
      return myPolyc;
    }

    // Curve
    public static SpeckleObject ToSpeckle( this NurbsCurve curve )
    {
      var properties = curve.UserDictionary.ToSpeckle( root: curve );

      if ( curve.IsArc( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) )
      {
        Arc getObj; curve.TryGetArc( out getObj );
        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsCircle( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) )
      {
        Circle getObj; curve.TryGetCircle( out getObj );
        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsEllipse( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) )
      {
        Ellipse getObj; curve.TryGetEllipse( out getObj );
        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      if ( curve.IsLinear( Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance ) || curve.IsPolyline() ) // defaults to polyline
      {
        Polyline getObj; curve.TryGetPolyline( out getObj );
        SpeckleObject myObject = getObj.ToSpeckle(); myObject.Properties = properties; myObject.GenerateHash();
        return myObject;
      }

      Polyline poly;
      curve.ToPolyline( 0, 1, 0, 0, 0, 0.1, 0, 0, true ).TryGetPolyline( out poly );

      SpecklePolyline displayValue;

      if ( poly.Count == 2 )
      {
        displayValue = new SpecklePolyline();
        displayValue.Value = new List<double> { poly[ 0 ].X, poly[ 0 ].Y, poly[ 0 ].Z, poly[ 1 ].X, poly[ 1 ].Y, poly[ 1 ].Z };
        displayValue.GenerateHash();
      }
      else
      {
        displayValue = poly.ToSpeckle() as SpecklePolyline;
      }

      SpeckleCurve myCurve = new SpeckleCurve( displayValue );
      NurbsCurve nurbsCurve = curve.ToNurbsCurve();

      myCurve.Weights = nurbsCurve.Points.Select( ctp => ctp.Weight ).ToList();
      myCurve.Points = nurbsCurve.Points.Select( ctp => ctp.Location ).ToFlatArray().ToList();
      myCurve.Knots = nurbsCurve.Knots.ToList();
      myCurve.Degree = nurbsCurve.Degree;
      myCurve.Periodic = nurbsCurve.IsPeriodic;
      myCurve.Rational = nurbsCurve.IsRational;
      myCurve.Domain = nurbsCurve.Domain.ToSpeckle();
      myCurve.Closed = nurbsCurve.IsClosed;

      myCurve.Properties = properties;
      myCurve.GenerateHash();

      return myCurve;
    }

    public static NurbsCurve ToNative( this SpeckleCurve curve )
    {
      var ptsList = curve.Points.ToPoints();

      if ( !curve.Periodic )
      {
        // Bug/feature in Rhino sdk: creating a periodic curve adds two extra stupid points? 
        var myCurve = NurbsCurve.Create( curve.Periodic, curve.Degree, ptsList );
        myCurve.Domain = curve.Domain.ToNative();

        // set weights
        for ( int i = 0; i < ptsList.Length; i++ )
          myCurve.Points.SetPoint( i, ptsList[ i ].X * curve.Weights[ i ], ptsList[ i ].Y * curve.Weights[ i ], ptsList[ i ].Z * curve.Weights[ i ], curve.Weights[ i ] );

        // set knots
        for ( int i = 0; i < curve.Knots.Count; i++ )
          myCurve.Knots[ i ] = curve.Knots[ i ];

        myCurve.UserDictionary.ReplaceContentsWith( curve.Properties.ToNative() );
        return myCurve;
      }
      else
      {
        var thePts = ptsList.Take( ptsList.Length - 3 ).ToArray();
        var myCurve = NurbsCurve.Create( curve.Periodic, curve.Degree, thePts );

        // set weights
        for ( int i = 0; i < ptsList.Length; i++ )
          myCurve.Points.SetPoint( i, ptsList[ i ].X * curve.Weights[ i ], ptsList[ i ].Y * curve.Weights[ i ], ptsList[ i ].Z * curve.Weights[ i ], curve.Weights[ i ] );

        // set knots
        for ( int i = 0; i < curve.Knots.Count; i++ )
          myCurve.Knots[ i ] = curve.Knots[ i ];

        myCurve.Domain = curve.Domain.ToNative();
        return myCurve;
      }
    }

    #endregion


    // Box
    public static SpeckleBox ToSpeckle( this Box box )
    {
      return new SpeckleBox( box.Plane.ToSpeckle(), box.X.ToSpeckle(), box.Y.ToSpeckle(), box.Z.ToSpeckle() );
    }

    public static Box ToNative( this SpeckleBox box )
    {
      return new Box( box.BasePlane.ToNative(), box.XSize.ToNative(), box.YSize.ToNative(), box.ZSize.ToNative() );
    }

    // Meshes
    public static SpeckleMesh ToSpeckle( this Mesh mesh )
    {
      var verts = mesh.Vertices.Select( pt => ( Point3d ) pt ).ToFlatArray();

      //var tex_coords = mesh.TextureCoordinates.Select( pt => pt ).ToFlatArray();

      var Faces = mesh.Faces.SelectMany( face =>
       {
         if ( face.IsQuad ) return new int[ ] { 1, face.A, face.B, face.C, face.D };
         return new int[ ] { 0, face.A, face.B, face.C };
       } ).ToArray();

      var Colors = mesh.VertexColors.Select( cl => cl.ToArgb() ).ToArray();
      double[ ] textureCoords;

      if ( SpeckleRhinoConverter.AddMeshTextureCoordinates )
      {
        textureCoords = mesh.TextureCoordinates.Select( pt => pt ).ToFlatArray();
        return new SpeckleMesh( verts, Faces, Colors, textureCoords, properties: mesh.UserDictionary.ToSpeckle( root: mesh ) );
      }

      return new SpeckleMesh( verts, Faces, Colors, null, properties: mesh.UserDictionary.ToSpeckle( root: mesh ) );
    }

    public static Mesh ToNative( this SpeckleMesh mesh )
    {
      Mesh m = new Mesh();
      m.Vertices.AddVertices( mesh.Vertices.ToPoints() );

      int i = 0;

      while ( i < mesh.Faces.Count )
      {
        if ( mesh.Faces[ i ] == 0 )
        { // triangle
          m.Faces.AddFace( new MeshFace( mesh.Faces[ i + 1 ], mesh.Faces[ i + 2 ], mesh.Faces[ i + 3 ] ) );
          i += 4;
        }
        else
        { // quad
          m.Faces.AddFace( new MeshFace( mesh.Faces[ i + 1 ], mesh.Faces[ i + 2 ], mesh.Faces[ i + 3 ], mesh.Faces[ i + 4 ] ) );
          i += 5;
        }
      }

      m.VertexColors.AppendColors( mesh.Colors.Select( c => System.Drawing.Color.FromArgb( ( int ) c ) ).ToArray() );

      if ( mesh.TextureCoordinates != null )
        for ( int j = 0; j < mesh.TextureCoordinates.Count; j += 2 )
        {
          m.TextureCoordinates.Add( mesh.TextureCoordinates[ j ], mesh.TextureCoordinates[ j + 1 ] );
        }

      m.UserDictionary.ReplaceContentsWith( mesh.Properties.ToNative() );
      return m;
    }

    // Breps
    public static SpeckleBrep ToSpeckle( this Brep brep )
    {
      var joinedMesh = new Mesh();
      if ( SpeckleRhinoConverter.SetBrepDisplayMesh )
      {
        MeshingParameters mySettings;
#if R6
        mySettings = new MeshingParameters( 0 );
#else
        mySettings = MeshingParameters.Coarse;

        mySettings.SimplePlanes = true;
        mySettings.RelativeTolerance = 0;
        mySettings.GridAspectRatio = 6;
        mySettings.GridAngle = Math.PI;
        mySettings.GridAspectRatio = 0;
        mySettings.SimplePlanes = true;
#endif

        Mesh.CreateFromBrep( brep, mySettings ).All( meshPart => { joinedMesh.Append( meshPart ); return true; } );
      }

      return new SpeckleBrep( displayValue: SpeckleRhinoConverter.SetBrepDisplayMesh ? joinedMesh.ToSpeckle() : null, rawData: JsonConvert.SerializeObject( brep ), provenance: "Rhino", properties: brep.UserDictionary.ToSpeckle( root: brep ) );
    }

    public static Brep ToNative( this SpeckleBrep brep )
    {
      try
      {
        if ( brep.Provenance == "Rhino" )
        {
          var myBrep = JsonConvert.DeserializeObject<Brep>( ( string ) brep.RawData );
          myBrep.UserDictionary.ReplaceContentsWith( brep.Properties.ToNative() );
          return myBrep;
        }
        throw new Exception( "Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other." );
      }
      catch
      {
        System.Diagnostics.Debug.WriteLine( "Failed to deserialise brep" );
        return null;
      }
    }

    // Extrusions
    public static SpeckleExtrusion ToSpeckle( this Rhino.Geometry.Extrusion extrusion )
    {
      //extrusion.PathTangent
      var myExtrusion = new SpeckleExtrusion( ( ( NurbsCurve ) extrusion.Profile3d( 0, 0 ) ).ToSpeckle(), extrusion.PathStart.DistanceTo( extrusion.PathEnd ), extrusion.IsCappedAtBottom );

      myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
      myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();
      myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();
      myExtrusion.ProfileTransformation = extrusion.GetProfileTransformation( 0.0 );

      var Profiles = new List<SpeckleObject>();
      for ( int i = 0; i < extrusion.ProfileCount; i++ )
        Profiles.Add( ( ( NurbsCurve ) extrusion.Profile3d( i, 0 ) ).ToSpeckle() );

      myExtrusion.Profiles = Profiles;
      myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle( root: extrusion );
      myExtrusion.GenerateHash();
      return myExtrusion;
    }

    public static Rhino.Geometry.Extrusion ToNative( this SpeckleExtrusion extrusion )
    {
      Curve profile = null;

      switch ( extrusion.Profile )
      {
        case SpeckleCore.SpeckleCurve curve:
          profile = curve.ToNative();
          break;
        case SpeckleCore.SpecklePolycurve polycurve:
          profile = polycurve.ToNative();
          if ( !profile.IsClosed )
            profile.Reverse();
          break;
        case SpeckleCore.SpecklePolyline polyline:
          profile = polyline.ToNative();
          if ( !profile.IsClosed )
            profile.Reverse();
          break;
        case SpeckleCore.SpeckleArc arc:
          profile = arc.ToNative();
          break;
        case SpeckleCore.SpeckleCircle circle:
          profile = circle.ToNative();
          break;
        case SpeckleCore.SpeckleEllipse ellipse:
          profile = ellipse.ToNative();
          break;
        case SpeckleCore.SpeckleLine line:
          profile = line.ToNative();
          break;
        default:
          profile = null;
          break;
      }

      if ( profile == null ) return null;

      var myExtrusion = Extrusion.Create( profile.ToNurbsCurve(), ( double ) extrusion.Length, ( bool ) extrusion.Capped );

      myExtrusion.UserDictionary.ReplaceContentsWith( extrusion.Properties.ToNative() );
      return myExtrusion;
    }

    // Texts & Annotations
    public static SpeckleAnnotation ToSpeckle( this TextEntity textentity )
    {
      Rhino.DocObjects.Font font = Rhino.RhinoDoc.ActiveDoc.Fonts[ textentity.FontIndex ];

      var myAnnotation = new SpeckleAnnotation();
      myAnnotation.Text = textentity.Text;
      myAnnotation.Plane = textentity.Plane.ToSpeckle();
      myAnnotation.FontName = font.FaceName;
      myAnnotation.TextHeight = textentity.TextHeight;
      myAnnotation.Bold = font.Bold;
      myAnnotation.Italic = font.Italic;
      myAnnotation.GenerateHash();

      return myAnnotation;
    }

    public static SpeckleAnnotation ToSpeckle( this TextDot textdot )
    {
      var myAnnotation = new SpeckleAnnotation();
      myAnnotation.Text = textdot.Text;
      myAnnotation.Location = textdot.Point.ToSpeckle();
      myAnnotation.GenerateHash();

      return myAnnotation;
    }

    public static object ToNative( this SpeckleAnnotation annot )
    {
      if ( annot.Plane != null )
      {
        // TEXT ENTITIY 
        var textEntity = new TextEntity()
        {
          Text = annot.Text,
          Plane = annot.Plane.ToNative(),
          FontIndex = Rhino.RhinoDoc.ActiveDoc.Fonts.FindOrCreate( annot.FontName, ( bool ) annot.Bold, ( bool ) annot.Italic ),
          TextHeight = ( double ) annot.TextHeight
        };
#if R6
        var dimStyleIndex = Rhino.RhinoDoc.ActiveDoc.DimStyles.Add( "Speckle" );
        var dimStyle = new Rhino.DocObjects.DimensionStyle
        {
          TextHeight = ( double ) annot.TextHeight,
          Font = new Rhino.DocObjects.Font( annot.FontName, Rhino.DocObjects.Font.FontWeight.Bold, Rhino.DocObjects.Font.FontStyle.Italic, false, false )
        };
        Rhino.RhinoDoc.ActiveDoc.DimStyles.Modify( dimStyle, dimStyleIndex, true );

        textEntity.DimensionStyleId = Rhino.RhinoDoc.ActiveDoc.DimStyles[ dimStyleIndex ].Id;

#endif

        return textEntity;
      }
      else
      {
        // TEXT DOT!
        var myTextdot = new TextDot( annot.Text, annot.Location.ToNative().Location );
        myTextdot.UserDictionary.ReplaceContentsWith( annot.Properties.ToNative() );
        return myTextdot;
      }
    }

    // Blocks and groups
    // TODO


    // Proper explosion of polycurves:
    // (C) The Rutten David https://www.grasshopper3d.com/forum/topics/explode-closed-planar-curve-using-rhinocommon 
    public static bool CurveSegments( List<Curve> L, Curve crv, bool recursive )
    {
      if ( crv == null ) { return false; }

      PolyCurve polycurve = crv as PolyCurve;
      if ( polycurve != null )
      {
        if ( recursive ) { polycurve.RemoveNesting(); }

        Curve[ ] segments = polycurve.Explode();

        if ( segments == null ) { return false; }
        if ( segments.Length == 0 ) { return false; }

        if ( recursive )
        {
          foreach ( Curve S in segments )
          {
            CurveSegments( L, S, recursive );
          }
        }
        else
        {
          foreach ( Curve S in segments )
          {
            L.Add( S.DuplicateShallow() as Curve );
          }
        }

        return true;
      }

      //Nothing else worked, lets assume it's a nurbs curve and go from there...
      NurbsCurve nurbs = crv.ToNurbsCurve();
      if ( nurbs == null ) { return false; }

      double t0 = nurbs.Domain.Min;
      double t1 = nurbs.Domain.Max;
      double t;

      int LN = L.Count;

      do
      {
        if ( !nurbs.GetNextDiscontinuity( Continuity.C1_locus_continuous, t0, t1, out t ) ) { break; }

        Interval trim = new Interval( t0, t );
        if ( trim.Length < 1e-10 )
        {
          t0 = t;
          continue;
        }

        Curve M = nurbs.DuplicateCurve();
        M = M.Trim( trim );
        if ( M.IsValid ) { L.Add( M ); }

        t0 = t;
      } while ( true );

      if ( L.Count == LN ) { L.Add( nurbs ); }

      return true;
    }
  }
}

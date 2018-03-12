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
  public class ConverterHack { /*makes sure the assembly is loaded*/  public ConverterHack( ) { } }

  /// <summary>
  /// These methods extend both the SpeckleObject types with a .ToNative() method as well as 
  /// the base RhinoCommon types with a .ToSpeckle() method for easier conversion logic.
  /// </summary>`
  public static class SpeckleRhinoConverter
  {

    // TODO: 
    // Extension method for ArchivableDictionary -> Dictionary
    // Extension mehtod Dictionary-> Archivable dictionary

    // Dictionaries & ArchivableDictionaries
    public static Dictionary<string, object> ToSpeckle( this ArchivableDictionary dict )
    {
      if ( dict == null ) return null;
      Dictionary<string, object> myDictionary = new Dictionary<string, object>();

      foreach ( var key in dict.Keys )
      {
        if ( dict[ key ] is ArchivableDictionary )
          myDictionary.Add( key, ( ( ArchivableDictionary ) dict[ key ] ).ToSpeckle() );
        else if ( dict[ key ] is string || dict[ key ] is double || dict[ key ] is float || dict[ key ] is int )
          myDictionary.Add( key, dict[ key ] );
        else
          myDictionary.Add( key, SpeckleCore.Converter.Serialise( dict[ key ] ) );
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

    // Mass point converter
    public static Point3d[ ] ToPoints( this double[ ] arr )
    {
      if ( arr.Length % 3 != 0 ) throw new Exception( "Array malformed: length%3 != 0." );

      Point3d[ ] points = new Point3d[ arr.Length / 3 ];
      for ( int i = 2, k = 0; i < arr.Length; i += 3 )
        points[ k++ ] = new Point3d( arr[ i - 2 ], arr[ i - 1 ], arr[ i ] );

      return points;
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point3d> points )
    {
      return points.SelectMany( pt => pt.ToArray() ).ToArray();
    }

    public static double[ ] ToFlatArray( this IEnumerable<Point2d> points )
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

    // The real deals below: 

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
      return new SpecklePoint( pt.Location.X, pt.Location.Y, pt.Location.Z, properties: pt.UserDictionary.Count != 0 ? pt.UserDictionary.ToSpeckle() : null );
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
      return new Interval( ( double ) interval.Start, ( double ) interval.End ); ;
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
    public static SpecklePolyline ToSpeckle( this Line line )
    {
      return new SpecklePolyline( ( new Point3d[ ] { line.From, line.To } ).ToFlatArray() );
    }

    // Rh Line capture
    public static SpecklePolyline ToSpeckle( this LineCurve line )
    {
      return new SpecklePolyline( new Point3d[ ] { line.PointAtStart, line.PointAtEnd }.ToFlatArray(), properties: line.UserDictionary.ToSpeckle() );
    }

    // Back again
    public static LineCurve ToNative( this SpeckleLine line )
    {
      var myLine = new LineCurve( line.Start.ToNative().Location, line.End.ToNative().Location );
      myLine.UserDictionary.ReplaceContentsWith( line.Properties.ToNative() );
      return myLine;
    }

    // Rectangles now and forever forward will become polylines
    public static SpecklePolyline ToSpeckle( this Rectangle3d rect )
    {
      return new SpecklePolyline( ( new Point3d[ ] { rect.Corner( 0 ), rect.Corner( 1 ), rect.Corner( 2 ), rect.Corner( 3 ), rect.Corner( 0 ) } ).ToFlatArray() );
    }

    // Circle
    // Gh Capture
    public static SpeckleCircle ToSpeckle( this Circle circ )
    {
      return new SpeckleCircle( circ.Center.ToSpeckle(), circ.Normal.ToSpeckle(), circ.Radius );
    }

    public static ArcCurve ToNative( this SpeckleCircle circ )
    {
      Circle circle = new Circle( new Plane( circ.Center.ToNative().Location, circ.Normal.ToNative() ), ( double ) circ.Radius );
      var myCircle = new ArcCurve( circle );
      myCircle.UserDictionary.ReplaceContentsWith( circ.Properties.ToNative() );
      return myCircle;
    }

    // Arc
    // Rh Capture can be a circle OR an arc, because fuck you
    public static SpeckleObject ToSpeckle( this ArcCurve a )
    {
      if ( a.IsClosed )
      {
        Circle preCircle;
        a.TryGetCircle( out preCircle );
        SpeckleCircle myCircle = preCircle.ToSpeckle();
        myCircle.Properties = a.UserDictionary.ToSpeckle();
        return myCircle;
      }
      else
      {
        Arc preArc;
        a.TryGetArc( out preArc );
        SpeckleArc myArc = preArc.ToSpeckle();
        myArc.Properties = a.UserDictionary.ToSpeckle();
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
      var myArc = new ArcCurve( arc );
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
      var myEllp = elp.ToNurbsCurve();
      myEllp.UserDictionary.ReplaceContentsWith( e.Properties.ToNative() );
      return myEllp;
    }

    // Polyline

    // Gh Capture
    public static SpecklePolyline ToSpeckle( this Polyline poly )
    {
      return new SpecklePolyline( poly.ToFlatArray() );
    }

    // Rh Capture
    public static SpecklePolyline ToSpeckle( this PolylineCurve poly )
    {
      Polyline polyline;
      if ( poly.TryGetPolyline( out polyline ) )
      {
        var myPoly = new SpecklePolyline( polyline.ToFlatArray() );
        myPoly.Properties = poly.UserDictionary.ToSpeckle();
        return myPoly;
      }
      return null;
    }

    // Deserialise
    public static PolylineCurve ToNative( this SpecklePolyline poly )
    {
      var myPoly = new PolylineCurve( poly.Value.ToPoints() );
      myPoly.UserDictionary.ReplaceContentsWith( poly.Properties.ToNative() );
      return myPoly;
    }

    // Polycurve
    // Rh Capture/Gh Capture
    public static SpecklePolycurve ToSpeckle( this PolyCurve p )
    {
      SpecklePolycurve myPoly = new SpecklePolycurve();

      p.RemoveNesting();
      var segments = p.Explode();

      myPoly.Segments = segments.Select( s => { return s.ToSpeckle(); } ).ToArray();
      myPoly.Properties = p.UserDictionary.ToSpeckle();
      myPoly.SetHashes( myPoly.Segments.Select( obj => obj.Hash ).ToArray() );
      return myPoly;
    }

    public static PolyCurve ToNative( this SpecklePolycurve p )
    {

      PolyCurve myPolyc = new PolyCurve();
      foreach ( var segment in p.Segments )
      {
        if ( segment.Type == "Curve" )
          myPolyc.Append( ( ( SpeckleCurve ) segment ).ToNative() );

        if ( segment.Type == "Line" )
          myPolyc.Append( ( ( SpeckleLine ) segment ).ToNative() );

        if ( segment.Type == "Arc" )
          myPolyc.Append( ( ( SpeckleArc ) segment ).ToNative() );

        if ( segment.Type == "Polyline" )
          myPolyc.Append( ( ( SpecklePolyline ) segment ).ToNative().ToNurbsCurve() );
      }
      myPolyc.UserDictionary.ReplaceContentsWith( p.Properties.ToNative() );
      return myPolyc;
    }

    public static SpeckleObject ToSpeckle( this Curve curve )
    {
      var properties = curve.UserDictionary.ToSpeckle();

      if ( curve is PolyCurve )
      {
        return ( ( PolyCurve ) curve ).ToSpeckle();
      }

      if ( curve.IsArc() )
      {
        Arc getObj; curve.TryGetArc( out getObj );
        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsCircle() )
      {
        Circle getObj; curve.TryGetCircle( out getObj );
        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsEllipse() )
      {
        Ellipse getObj; curve.TryGetEllipse( out getObj );
        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsLinear() || curve.IsPolyline() ) // defaults to polyline
      {
        Polyline getObj; curve.TryGetPolyline( out getObj );
        SpecklePolyline myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      Polyline poly;
      curve.ToPolyline( 0, 1, 0, 0, 0, 0.1, 0, 0, true ).TryGetPolyline( out poly );

      SpeckleCurve myCurve = new SpeckleCurve( poly.ToSpeckle(), properties: curve.UserDictionary.ToSpeckle() );
      NurbsCurve nurbsCurve = curve.ToNurbsCurve();


      myCurve.Weights = nurbsCurve.Points.Select( ctp => ctp.Weight ).ToArray();
      myCurve.Points = nurbsCurve.Points.Select( ctp => ctp.Location ).ToFlatArray();
      myCurve.Knots = nurbsCurve.Knots.ToArray();
      myCurve.Degree = nurbsCurve.Degree;
      myCurve.Periodic = nurbsCurve.IsPeriodic;
      myCurve.Rational = nurbsCurve.IsRational;
      myCurve.Domain = nurbsCurve.Domain.ToSpeckle();

      myCurve.Properties = properties;
      return myCurve;
    }

    // Curve
    public static SpeckleObject ToSpeckle( this NurbsCurve curve )
    {
      var properties = curve.UserDictionary.ToSpeckle();

      //if ( curve.se )
      //{
      //  return ( ( PolyCurve ) curve ).ToSpeckle();
      //}

      if ( curve.IsArc() )
      {
        Arc getObj; curve.TryGetArc( out getObj );
        SpeckleArc myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsCircle() )
      {
        Circle getObj; curve.TryGetCircle( out getObj );
        SpeckleCircle myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsEllipse() )
      {
        Ellipse getObj; curve.TryGetEllipse( out getObj );
        SpeckleEllipse myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      if ( curve.IsLinear() || curve.IsPolyline() ) // defaults to polyline
      {
        Polyline getObj; curve.TryGetPolyline( out getObj );
        SpecklePolyline myObject = getObj.ToSpeckle(); myObject.Properties = properties;
        return myObject;
      }

      Polyline poly;
      curve.ToPolyline( 0, 1, 0, 0, 0, 0.1, 0, 0, true ).TryGetPolyline( out poly );

      SpeckleCurve myCurve = new SpeckleCurve( poly.ToSpeckle(), properties: curve.UserDictionary.ToSpeckle() );

      myCurve.Weights = curve.Points.Select( ctp => ctp.Weight ).ToArray();
      myCurve.Points = curve.Points.Select( ctp => ctp.Location ).ToFlatArray();
      myCurve.Knots = curve.Knots.ToArray();
      myCurve.Degree = curve.Degree;
      myCurve.Periodic = curve.IsPeriodic;
      myCurve.Rational = curve.IsRational;
      myCurve.Domain = curve.Domain.ToSpeckle();

      myCurve.Properties = properties;
      return myCurve;
    }

    public static NurbsCurve ToNative( this SpeckleCurve curve )
    {
      var ptsList = curve.Points.ToPoints();

      // Bug/feature in Rhino sdk: creating a periodic curve adds two extra stupid points? 
      var myCurve = NurbsCurve.Create( curve.Periodic, curve.Degree, new Point3d[ curve.Periodic ? ptsList.Length - 2 : ptsList.Length ] );
      myCurve.Domain = curve.Domain.ToNative();

      for ( int i = 0; i < ptsList.Length; i++ )
        myCurve.Points.SetPoint( i, ptsList[ i ].X, ptsList[ i ].Y, ptsList[ i ].Z, curve.Weights[ i ] );

      for ( int i = 0; i < curve.Knots.Length; i++ )
        myCurve.Knots[ i ] = curve.Knots[ i ];

      return myCurve;
    }

    #endregion

    // do not worry (too much!) from down here onwards

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
      return new SpeckleMesh( verts, Faces, Colors, null, properties: mesh.UserDictionary.ToSpeckle() );
    }

    public static Mesh ToNative( this SpeckleMesh mesh )
    {
      Mesh m = new Mesh();
      m.Vertices.AddVertices( mesh.Vertices.ToPoints() );

      int i = 0;

      while ( i < mesh.Faces.Length )
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

      //if ( mesh.TextureCoordinates != null )
      //  for ( int j = 0; j < mesh.TextureCoordinates.Length; j += 2 )
      //  {
      //    m.TextureCoordinates.Add( mesh.TextureCoordinates[ j ], mesh.TextureCoordinates[ j + 1 ] );
      //  }

      m.UserDictionary.ReplaceContentsWith( mesh.Properties.ToNative() );
      return m;
    }

    // Breps
    public static SpeckleBrep ToSpeckle( this Brep brep )
    {
      var joinedMesh = new Mesh();
      Mesh.CreateFromBrep( brep ).All( meshPart => { joinedMesh.Append( meshPart ); return true; } );

      return new SpeckleBrep( displayValue: joinedMesh.ToSpeckle(), base64: Converter.getBase64( brep ), provenance: "ON", properties: brep.UserDictionary.ToSpeckle() );
    }

    public static Brep ToNative( this SpeckleBrep brep )
    {
      try
      {
        if ( brep.Provenance == "ON" )
        {
          var myBrep = ( Brep ) Converter.getObjFromString( brep.Base64 );
          myBrep.UserDictionary.ReplaceContentsWith( brep.Properties.ToNative() );
          return myBrep;
        }
        else
          throw new Exception( "Unknown brep provenance: " + brep.Provenance + ". Don't know how to convert from one to the other." );
      }
      catch
      {
        // Fail silently
        return null;
      }
    }

    // Extrusions
    public static SpeckleExtrusion ToSpeckle( this Rhino.Geometry.Extrusion extrusion )
    {
      //extrusion.PathTangent
      var myExtrusion = new SpeckleExtrusion( extrusion.Profile3d( 0, 0 ).ToSpeckle(), extrusion.PathStart.DistanceTo( extrusion.PathEnd ), extrusion.IsCappedAtBottom );

      myExtrusion.PathTangent = extrusion.PathTangent.ToSpeckle();
      myExtrusion.PathStart = extrusion.PathStart.ToSpeckle();
      myExtrusion.PathEnd = extrusion.PathEnd.ToSpeckle();

      myExtrusion.Properties = extrusion.UserDictionary.ToSpeckle();
      myExtrusion.SetHashes( myExtrusion );
      return myExtrusion;
    }

    public static Rhino.Geometry.Extrusion ToNative( this SpeckleExtrusion extrusion )
    {
      Curve profile = null;

      switch ( extrusion.Profile.Type )
      {
        case "Curve":
          profile = ( ( SpeckleCurve ) extrusion.Profile ).ToNative();
          break;
        case "Polycurve":
          profile = ( ( ( SpecklePolycurve ) extrusion.Profile ).ToNative() );
          if ( !profile.IsClosed )
            profile.Reverse();
          break;
        case "Polyline":
          profile = ( ( SpecklePolyline ) extrusion.Profile ).ToNative();
          if ( !profile.IsClosed )
            profile.Reverse();
          break;
        case "Arc":
          profile = ( ( SpeckleArc ) extrusion.Profile ).ToNative();
          break;
        case "Circle":
          profile = ( ( SpeckleCircle ) extrusion.Profile ).ToNative();
          break;
        case "Ellipse":
          profile = ( ( SpeckleEllipse ) extrusion.Profile ).ToNative();
          break;
        case "Line":
          profile = ( ( SpeckleLine ) extrusion.Profile ).ToNative();
          break;
        default:
          profile = null;
          break;
      }

      if ( profile == null ) return null;

      var myExtrusion = Extrusion.Create( profile.ToNurbsCurve(), extrusion.Length, extrusion.Capped );

      var res = myExtrusion.SetPathAndUp( extrusion.PathStart.ToNative().Location, extrusion.PathEnd.ToNative().Location, extrusion.PathTangent.ToNative() );
      var resss = res;

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
      myAnnotation.FaceName = font.FaceName;
      myAnnotation.TextHeight = textentity.TextHeight;
      myAnnotation.Bold = font.Bold;
      myAnnotation.Italic = font.Italic;
      myAnnotation.SetHashes( myAnnotation );

      return myAnnotation;
    }

    public static SpeckleAnnotation ToSpeckle( this TextDot textdot )
    {
      var myAnnotation = new SpeckleAnnotation();
      myAnnotation.Text = textdot.Text;
      myAnnotation.Location = textdot.Point.ToSpeckle();
      myAnnotation.SetHashes( myAnnotation );

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
                    FontIndex = Rhino.RhinoDoc.ActiveDoc.Fonts.FindOrCreate(annot.FaceName, annot.Bold, annot.Italic),
                    TextHeight = annot.TextHeight
                };
#if R6
                var dimStyleIndex = Rhino.RhinoDoc.ActiveDoc.DimStyles.Add("Speckle");
                var dimStyle = new Rhino.DocObjects.DimensionStyle
                {
                    TextHeight = annot.TextHeight,
                    Font = new Rhino.DocObjects.Font(annot.FaceName, Rhino.DocObjects.Font.FontWeight.Bold, Rhino.DocObjects.Font.FontStyle.Italic, false, false)
                };
                Rhino.RhinoDoc.ActiveDoc.DimStyles.Modify(dimStyle, dimStyleIndex, true);

                textEntity.DimensionStyleId = Rhino.RhinoDoc.ActiveDoc.DimStyles[dimStyleIndex].Id;

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
  }
}

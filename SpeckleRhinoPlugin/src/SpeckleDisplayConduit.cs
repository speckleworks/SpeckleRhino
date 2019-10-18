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
  /// kudos to Luis @fraguada
  /// </summary>
  public class SpeckleDisplayConduit : Rhino.Display.DisplayConduit
  {

    public List<GeometryBase> Geometry { get; set; }

    public SpeckleDisplayConduit( )
    {
      Geometry = new List<GeometryBase>();
    }


    protected override void CalculateBoundingBox( CalculateBoundingBoxEventArgs e )
    {
      Rhino.Geometry.BoundingBox bbox = Rhino.Geometry.BoundingBox.Unset;
      if ( null != Geometry )
      {
        var localCopy = Geometry.ToList();
        foreach ( var obj in localCopy )
          if ( obj != null )
            try { bbox.Union( obj.GetBoundingBox( false ) ); } catch { }
        e.IncludeBoundingBox( bbox );
      }

    }

    protected override void CalculateBoundingBoxZoomExtents( CalculateBoundingBoxEventArgs e )
    {
      Rhino.Geometry.BoundingBox bbox = Rhino.Geometry.BoundingBox.Unset;
      if ( null != Geometry )
      {
        var localCopy = Geometry.ToList();
        foreach ( var obj in localCopy )
          if ( obj != null )
            try { bbox.Union( obj.GetBoundingBox( false ) ); } catch { }
        e.IncludeBoundingBox( bbox );
      }
    }

    protected override void PostDrawObjects( DrawEventArgs e )
    {
      //if ( VisibleList.Count == 0 ) return;

      base.PostDrawObjects( e );
      int count = 0;

      //var LocalCopy = Geometry.ToArray();

      var color = Rhino.ApplicationSettings.AppearanceSettings.SelectedObjectColor;

      foreach ( var obj in Geometry )
      {
        if( obj.Disposed ) continue;
        //if ( VisibleList[ count ] && obj != null && !obj.IsDocumentControlled )
          switch ( obj.ObjectType )
          {
            case Rhino.DocObjects.ObjectType.Point:
              e.Display.DrawPoint( ( ( Rhino.Geometry.Point ) obj ).Location, PointStyle.X, 2, color );
              break;

            case Rhino.DocObjects.ObjectType.Curve:
              e.Display.DrawCurve( ( Curve ) obj, color );
              break;

            case Rhino.DocObjects.ObjectType.Extrusion:
              DisplayMaterial eMaterial = new DisplayMaterial( color, 0.5 );
              e.Display.DrawBrepShaded( ( ( Extrusion ) obj ).ToBrep(), eMaterial );
              break;
            case Rhino.DocObjects.ObjectType.Brep:
              DisplayMaterial bMaterial = new DisplayMaterial( color, 0.5 );
              e.Display.DrawBrepShaded( ( Brep ) obj, bMaterial );
              //e.Display.DrawBrepWires((Brep)obj, Color.DarkGray, 1);
              break;

            case Rhino.DocObjects.ObjectType.Mesh:
              var mesh = obj as Mesh;
              if ( mesh.VertexColors.Count > 0 )
              {
                for ( int i = 0; i < mesh.VertexColors.Count; i++ )
                  mesh.VertexColors[ i ] = Color.FromArgb( 100, mesh.VertexColors[ i ] );

                e.Display.DrawMeshFalseColors( mesh );
              }
              else
              {
                DisplayMaterial mMaterial = new DisplayMaterial( color, 0.5 );
                e.Display.DrawMeshShaded( mesh, mMaterial );
              }
              //e.Display.DrawMeshWires((Mesh)obj, Color.DarkGray);
              break;

            case Rhino.DocObjects.ObjectType.TextDot:
              //e.Display.Draw3dText( ((TextDot)obj).Text, Colors[count], new Plane(((TextDot)obj).Point));
              var textDot = ( TextDot ) obj;
              e.Display.DrawDot( textDot.Point, textDot.Text, color, Color.White );

              break;

            case Rhino.DocObjects.ObjectType.Annotation:
              if ( obj is TextEntity )
              {
                var textObj = ( Rhino.Geometry.TextEntity ) obj;
#if WINR6
                                var textHeight = Rhino.RhinoDoc.ActiveDoc.DimStyles.FindId(textObj.DimensionStyleId).TextHeight;
                e.Display.Draw3dText( textObj.PlainText, Colors[ count ], textObj.Plane, textHeight, textObj.Font.FaceName );
#else
                e.Display.Draw3dText(textObj.Text, Color.Black, textObj.Plane,textObj.TextHeight ,Rhino.RhinoDoc.ActiveDoc.Fonts[textObj.FontIndex].FaceName);
#endif
              }
              break;
          }
        count++;
      }
    }
  }
}

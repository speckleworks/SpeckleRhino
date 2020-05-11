﻿using System;
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

    public List<Color> Colors { get; set; }

    public List<bool> VisibleList { get; set; }

    public Interval? HoverRange { get; set; }

    public SpeckleDisplayConduit( )
    {
      Geometry = new List<GeometryBase>();
      Colors = new List<Color>();
      VisibleList = new List<bool>();
    }

    public SpeckleDisplayConduit( List<GeometryBase> _Geometry )
    {
      Geometry = _Geometry;
      Colors = new List<Color>();
      VisibleList = new List<bool>();
    }

    public SpeckleDisplayConduit( List<GeometryBase> _Geometry, List<Color> _Colors, List<bool> _VisibleList )
    {
      Geometry = _Geometry;
      Colors = _Colors;
      VisibleList = _VisibleList;
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
      if ( VisibleList.Count == 0 ) return;

      base.PostDrawObjects( e );
      int count = 0;

      var LocalCopy = Geometry.ToArray();

      foreach ( var obj in LocalCopy )
      {
        if ( VisibleList[ count ] && obj != null && !obj.IsDocumentControlled )
          switch ( obj.ObjectType )
          {
            case Rhino.DocObjects.ObjectType.Point:
              e.Display.DrawPoint( ( ( Rhino.Geometry.Point ) obj ).Location, PointStyle.X, 2, Colors[ count ] );
              break;

            case Rhino.DocObjects.ObjectType.Curve:
              e.Display.DrawCurve( ( Curve ) obj, Colors[ count ] );
              break;

            case Rhino.DocObjects.ObjectType.Extrusion:
              DisplayMaterial eMaterial = new DisplayMaterial( Colors[ count ], 0.5 );
              e.Display.DrawBrepShaded( ( ( Extrusion ) obj ).ToBrep(), eMaterial );
              break;
            case Rhino.DocObjects.ObjectType.Brep:
              DisplayMaterial bMaterial = new DisplayMaterial( Colors[ count ], 0.5 );
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
                DisplayMaterial mMaterial = new DisplayMaterial( Colors[ count ], 0.5 );
                e.Display.DrawMeshShaded( mesh, mMaterial );
              }
              //e.Display.DrawMeshWires((Mesh)obj, Color.DarkGray);
              break;

            case Rhino.DocObjects.ObjectType.TextDot:
              //e.Display.Draw3dText( ((TextDot)obj).Text, Colors[count], new Plane(((TextDot)obj).Point));
              var textDot = ( TextDot ) obj;
              e.Display.DrawDot( textDot.Point, textDot.Text, Colors[ count ], Color.White );

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

    protected override void DrawOverlay( DrawEventArgs e )
    {
      base.DrawOverlay( e );
      if ( HoverRange == null ) return;

      var LocalCopy = Geometry.ToArray();

      var selectColor = Rhino.ApplicationSettings.AppearanceSettings.SelectedObjectColor;

      for ( int i = ( int ) HoverRange.Value.T0; i < HoverRange.Value.T1; i++ )
      {
        if ( LocalCopy[ i ] != null )
        {
          var obj = LocalCopy[ i ];
          //if ( obj.IsDocumentControlled ) continue;

          switch ( obj.ObjectType )
          {
            case Rhino.DocObjects.ObjectType.Point:
              e.Display.DrawPoint( ( ( Rhino.Geometry.Point ) obj ).Location, PointStyle.X, 4, selectColor);
              break;

            case Rhino.DocObjects.ObjectType.Curve:
              e.Display.DrawCurve( ( Curve ) obj, selectColor);
              break;

            case Rhino.DocObjects.ObjectType.Brep:
              e.Display.DrawBrepWires((Brep)obj, selectColor, 1);
              break;

            case Rhino.DocObjects.ObjectType.Extrusion:
              e.Display.DrawBrepWires((obj as Extrusion).ToBrep(), selectColor);
              break;

            case Rhino.DocObjects.ObjectType.Mesh:
              e.Display.DrawMeshWires((Mesh)obj, selectColor);
              break;

            case Rhino.DocObjects.ObjectType.TextDot:
              var textDot = ( TextDot ) obj;
              e.Display.DrawDot( textDot.Point, textDot.Text, selectColor, Color.Black );
              break;

            case Rhino.DocObjects.ObjectType.Annotation:
              if ( obj is TextEntity )
              {
                var textObj = ( Rhino.Geometry.TextEntity ) obj;
#if WINR6
                e.Display.Draw3dText( textObj.PlainText, selectColor, textObj.Plane, textObj.TextHeight, textObj.Font.FaceName );
#else
                e.Display.Draw3dText(textObj.Text, selectColor, textObj.Plane,textObj.TextHeight ,Rhino.RhinoDoc.ActiveDoc.Fonts[textObj.FontIndex].FaceName);
#endif
               }
              break;
          }
        }

      }
    }
  }
}

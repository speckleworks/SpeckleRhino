using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleCore;
using SpeckleRhinoConverter;
using SpecklePopup;


using Grasshopper;
using Grasshopper.Kernel.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using System.Dynamic;
using System.Windows;
using System.Threading.Tasks;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using System.Windows.Forms;
using Grasshopper.GUI;

namespace SpeckleGrasshopper
{
  public class GhReceiverClient : GH_Component, IGH_VariableParameterComponent
  {
    string AuthToken;
    string RestApi;
    public string StreamId;

    public bool Paused = false;
    public bool Expired = false;

    public GH_Document Document;

    public SpeckleApiClient myReceiver;
    List<SpeckleLayer> Layers;
    List<SpeckleObjectPlaceholder> PlaceholderObjects;
    List<SpeckleObject> SpeckleObjects;
    List<object> ConvertedObjects;

    Action expireComponentAction;

    RhinoConverter Converter;

    private Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();

    public GhReceiverClient( )
      : base( "Data Receiver", "Data Receiver",
          "Receives data from Speckle.",
          "Speckle", "I/O" )
    {
    }

    public override void CreateAttributes( )
    {
      m_attributes = new GhReceiverClientAttributes( this );
    }

    public override bool Write( GH_IWriter writer )
    {
      try
      {
        if ( myReceiver != null )
          using ( var ms = new MemoryStream() )
          {
            var formatter = new BinaryFormatter();
            formatter.Serialize( ms, myReceiver );
            writer.SetByteArray( "speckleclient", ms.ToArray() );
          }
      }
      catch { }
      return base.Write( writer );
    }

    public override bool Read( GH_IReader reader )
    {
      try
      {
        Debug.WriteLine( "Trying to read client!" );
        var serialisedClient = reader.GetByteArray( "speckleclient" );
        using ( var ms = new MemoryStream() )
        {
          ms.Write( serialisedClient, 0, serialisedClient.Length );
          ms.Seek( 0, SeekOrigin.Begin );
          myReceiver = ( SpeckleApiClient ) new BinaryFormatter().Deserialize( ms );

          StreamId = myReceiver.StreamId;
          AuthToken = myReceiver.AuthToken;
          RestApi = myReceiver.BaseUrl;

          InitReceiverEventsAndGlobals();
        }
      }
      catch
      {
        Debug.WriteLine( "No client was present." );
      }
      return base.Read( reader );
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );
      Document = this.OnPingDocument();

      if ( myReceiver == null )
      {
        var myForm = new SpecklePopup.MainWindow();

        var some = new System.Windows.Interop.WindowInteropHelper( myForm );
        some.Owner = Rhino.RhinoApp.MainWindowHandle();

        myForm.ShowDialog();

        if ( myForm.restApi != null && myForm.apitoken != null )
        {
          RestApi = myForm.restApi;
          AuthToken = myForm.apitoken;
        }
        else
        {
          AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Account selection failed." );
          return;
        }
      }
    }

    public void InitReceiverEventsAndGlobals( )
    {
      Converter = new RhinoConverter();

      ObjectCache = new Dictionary<string, SpeckleObject>();

      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      myReceiver.OnReady += ( sender, e ) =>
      {
        UpdateGlobal();
      };

      myReceiver.OnWsMessage += OnWsMessage;

      myReceiver.OnError += ( sender, e ) =>
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, e.EventName + ": " + e.EventData );
      };

      expireComponentAction = ( ) => this.ExpireSolution( true );
    }

    public override void AppendAdditionalMenuItems( ToolStripDropDown menu )
    {
      base.AppendAdditionalMenuItems( menu );
      GH_DocumentObject.Menu_AppendItem( menu, "Copy streamId (" + StreamId + ") to clipboard.", ( sender, e ) =>
        {
          if ( StreamId != null )
            System.Windows.Clipboard.SetText( StreamId );
        } );

      GH_DocumentObject.Menu_AppendSeparator( menu );

      GH_DocumentObject.Menu_AppendItem( menu, "View stream.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi.Replace( "api", "view" ) + @"/?" + StreamId );
       } );

      GH_DocumentObject.Menu_AppendItem( menu, "View stream data.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi + @"/streams/" + StreamId );
       } );

      GH_DocumentObject.Menu_AppendItem( menu, "View layers data online.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi + @"/streams/" + StreamId + @"/layers" );
       } );

      GH_DocumentObject.Menu_AppendItem( menu, "View objects data online.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi + @"/streams/" + StreamId + @"/objects?omit=displayValue,base64" );
       } );

      if ( myReceiver.Stream == null ) return;

      GH_DocumentObject.Menu_AppendSeparator( menu );
      if ( myReceiver.Stream.Parent == null )
        GH_DocumentObject.Menu_AppendItem( menu: menu, text: "This is a parent stream.", enabled: false, click: null );
      else
        GH_DocumentObject.Menu_AppendItem( menu: menu, text: "Parent: " + myReceiver.Stream.Parent, click: ( sender, e ) =>
         {
           System.Windows.Clipboard.SetText( myReceiver.Stream.Parent );
           System.Windows.MessageBox.Show( "Parent id copied to clipboard. Share away!" );
         } );
      GH_DocumentObject.Menu_AppendSeparator( menu );

      GH_DocumentObject.Menu_AppendSeparator( menu );
      GH_DocumentObject.Menu_AppendItem( menu, "Children:" );
      GH_DocumentObject.Menu_AppendSeparator( menu );

      foreach ( string childId in myReceiver.Stream.Children )
      {
        GH_DocumentObject.Menu_AppendItem( menu, "Child " + childId, ( sender, e ) =>
         {
           System.Windows.Clipboard.SetText( childId );
           System.Windows.MessageBox.Show( "Child id copied to clipboard. Share away!" );
         } );
      }

    }

    public virtual void OnWsMessage( object source, SpeckleEventArgs e )
    {
      if ( Paused )
      {
        AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "Update available since " + DateTime.Now );
        Expired = true;
        return;
      }

      switch ( ( string ) e.EventObject.args.eventType )
      {
        case "update-global":
          UpdateGlobal();
          break;
        case "update-meta":
          UpdateMeta();
          break;
        case "update-children":
          UpdateChildren();
          break;
        default:
          CustomMessageHandler( ( string ) e.EventObject.args.eventType, e );
          break;
      }
    }

    public virtual void UpdateGlobal( )
    {
      var getStream = myReceiver.StreamGetAsync( myReceiver.StreamId );
      getStream.Wait();

      NickName = getStream.Result.Stream.Name;
      Layers = getStream.Result.Stream.Layers.ToList();

      // TODO: Implement cache
      // we can safely omit the displayValue, since this is rhino!
      PayloadObjectGetBulk payload = new PayloadObjectGetBulk();
      payload.Objects = getStream.Result.Stream.Objects.Where( o => !ObjectCache.ContainsKey( o ) );
      myReceiver.ObjectGetBulkAsync( "omit=displayValue", payload ).ContinueWith( tres =>
         {
           // add to cache
           foreach ( var x in tres.Result.Objects )
             ObjectCache[ x.DatabaseId ] = x;

           // populate real objects
           SpeckleObjects.Clear();
           foreach ( var objId in getStream.Result.Stream.Objects )
             SpeckleObjects.Add( ObjectCache[ objId ] );

           ConvertedObjects = Converter.ToNative( SpeckleObjects ).ToList();

           UpdateOutputStructure();

           Message = "Got data\n@" + DateTime.Now.ToString( "hh:mm:ss" );

           Rhino.RhinoApp.MainApplicationWindow.Invoke( expireComponentAction );
         } );
    }

    public virtual void UpdateMeta( )
    {
      var getName = myReceiver.StreamGetNameAsync( StreamId );
      var getLayers = myReceiver.GetLayersAsync( StreamId );

      Task.WhenAll( new Task[ ] { getName, getLayers } ).Wait();

      NickName = getName.Result.Name;
      Layers = getLayers.Result.Layers.ToList();
      UpdateOutputStructure();
    }

    public virtual void UpdateChildren( )
    {
      // need a call to just get the kids
      var getStream = myReceiver.StreamGet( myReceiver.StreamId );
      myReceiver.Stream = getStream.Stream;
    }

    public virtual void CustomMessageHandler( string eventType, SpeckleEventArgs e )
    {
      Debug.WriteLine( "Received {0} type message.", eventType );
    }

    public override void RemovedFromDocument( GH_Document document )
    {
      if ( myReceiver != null )
        myReceiver.Dispose();
      base.RemovedFromDocument( document );
    }

    public override void DocumentContextChanged( GH_Document document, GH_DocumentContext context )
    {
      if ( context == GH_DocumentContext.Close )
      {
        myReceiver.Dispose();
      }
      base.DocumentContextChanged( document, context );
    }

    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
      pManager.AddTextParameter( "ID", "ID", "The stream's short id.", GH_ParamAccess.item );
    }

    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      if ( Paused )
      {
        SetObjects( DA );
        return;
      }

      string inputId = null;
      DA.GetData( 0, ref inputId );

      if ( inputId == null && StreamId == null ) return;

      if ( inputId != StreamId )
      {
        Debug.WriteLine( "Changing streams: {0} ::> {1}", inputId, StreamId );

        StreamId = inputId;

        if ( myReceiver != null )
          myReceiver.Dispose( true );

        myReceiver = new SpeckleApiClient( RestApi, Converter, true );

        InitReceiverEventsAndGlobals();

        myReceiver.IntializeReceiver( StreamId, Document.DisplayName, "Grasshopper", Document.DocumentID.ToString(), AuthToken );

        return;
      }

      if ( myReceiver == null )
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Remark, "Receiver not intialised." );
        return;
      }

      if ( !myReceiver.IsConnected ) return;

      if ( Expired ) { Expired = false; UpdateGlobal(); return; }

      SetObjects( DA );
    }

    public void UpdateOutputStructure( )
    {
      List<SpeckleLayer> toRemove, toAdd, toUpdate;
      toRemove = new List<SpeckleLayer>(); toAdd = new List<SpeckleLayer>(); toUpdate = new List<SpeckleLayer>();

      SpeckleLayer.DiffLayerLists( GetLayers(), Layers, ref toRemove, ref toAdd, ref toUpdate );

      foreach ( SpeckleLayer layer in toRemove )
      {
        var myparam = Params.Output.FirstOrDefault( item => { return item.Name == layer.Guid; } );

        if ( myparam != null )
          Params.UnregisterOutputParameter( myparam );
      }

      foreach ( var layer in toAdd )
      {
        Param_GenericObject newParam = getGhParameter( layer );
        Params.RegisterOutputParam( newParam, ( int ) layer.OrderIndex );
      }

      foreach ( var layer in toUpdate )
      {
        var myparam = Params.Output.FirstOrDefault( item => { return item.Name == layer.Guid; } );
        myparam.NickName = layer.Name;
      }
      Params.OnParametersChanged();
    }

    public void SetObjects( IGH_DataAccess DA )
    {
      if ( Layers == null ) return;

      foreach ( SpeckleLayer layer in Layers )
      {
        var subset = ConvertedObjects.GetRange( ( int ) layer.StartIndex, ( int ) layer.ObjectCount );

        if ( layer.Topology == "" )
          DA.SetDataList( ( int ) layer.OrderIndex, subset );
        else
        {
          //HIC SVNT DRACONES
          var tree = new DataTree<object>();
          var treeTopo = layer.Topology.Split( ' ' );
          int subsetCount = 0;
          foreach ( var branch in treeTopo )
          {
            if ( branch != "" )
            {
              var branchTopo = branch.Split( '-' )[ 0 ].Split( ';' );
              var branchIndexes = new List<int>();
              foreach ( var t in branchTopo ) branchIndexes.Add( Convert.ToInt32( t ) );

              var elCount = Convert.ToInt32( branch.Split( '-' )[ 1 ] );
              GH_Path myPath = new GH_Path( branchIndexes.ToArray() );

              for ( int i = 0; i < elCount; i++ )
                tree.EnsurePath( myPath ).Add( new Grasshopper.Kernel.Types.GH_ObjectWrapper( subset[ subsetCount + i ] ) );
              subsetCount += elCount;
            }
          }
          DA.SetDataTree( ( int ) layer.OrderIndex, tree );
        }
      }
    }

    public override void DrawViewportMeshes( IGH_PreviewArgs args )
    {
      base.DrawViewportMeshes( args );
      if ( this.Hidden || this.Locked ) return;

      foreach ( var obj in ConvertedObjects )
      {
        //if(obj is Rhino.DocObject) { 
        obj.GetType();
      }

      // TODO
    }

    public override void DrawViewportWires( IGH_PreviewArgs args )
    {
      base.DrawViewportWires( args );
      
      if ( this.Hidden || this.Locked ) return;
      System.Drawing.Color clr = !this.Attributes.Selected ? System.Drawing.Color.AliceBlue : System.Drawing.Color.OrangeRed;

      foreach ( var obj in ConvertedObjects )
      {
        if ( !( obj is GeometryBase ) ) continue;
        switch ( ( ( GeometryBase ) obj ).ObjectType )
        {
          case Rhino.DocObjects.ObjectType.Point:
            args.Display.DrawPoint( ( ( Rhino.Geometry.Point ) obj ).Location, Rhino.Display.PointStyle.X, 2, clr );
            break;

          case Rhino.DocObjects.ObjectType.Curve:
            args.Display.DrawCurve( ( Curve ) obj, clr );
            break;

          case Rhino.DocObjects.ObjectType.Extrusion:
            Rhino.Display.DisplayMaterial eMaterial = new Rhino.Display.DisplayMaterial( clr, 0.5 );
            args.Display.DrawBrepShaded( ( ( Extrusion ) obj ).ToBrep(), eMaterial );
            break;
          case Rhino.DocObjects.ObjectType.Brep:
            Rhino.Display.DisplayMaterial bMaterial = new Rhino.Display.DisplayMaterial( clr, 0.5 );
            args.Display.DrawBrepShaded( ( Brep ) obj, bMaterial );
            //e.Display.DrawBrepWires((Brep)obj, Color.DarkGray, 1);
            break;

          case Rhino.DocObjects.ObjectType.Mesh:
            var mesh = obj as Mesh;
            if ( mesh.VertexColors.Count > 0 )
            {
              for ( int i = 0; i < mesh.VertexColors.Count; i++ )
                mesh.VertexColors[ i ] = System.Drawing.Color.FromArgb( 100, mesh.VertexColors[ i ] );

              args.Display.DrawMeshFalseColors( mesh );
            }
            else
            {
              Rhino.Display.DisplayMaterial mMaterial = new Rhino.Display.DisplayMaterial( clr, 0.5 );
              args.Display.DrawMeshShaded( mesh, mMaterial );
            }
            //e.Display.DrawMeshWires((Mesh)obj, Color.DarkGray);
            break;

          case Rhino.DocObjects.ObjectType.TextDot:
            //e.Display.Draw3dText( ((TextDot)obj).Text, Colors[count], new Plane(((TextDot)obj).Point));
            //todo
            break;

          case Rhino.DocObjects.ObjectType.Annotation:
            //todo
            break;
        }
        // TODO
      }
    }

    #region Variable Parm

    private Param_GenericObject getGhParameter( SpeckleLayer param )
    {
      Param_GenericObject newParam = new Param_GenericObject();
      newParam.Name = ( string ) param.Guid;
      newParam.NickName = ( string ) param.Name;
      newParam.MutableNickName = false;
      newParam.Access = GH_ParamAccess.tree;
      return newParam;
    }

    bool IGH_VariableParameterComponent.CanInsertParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    bool IGH_VariableParameterComponent.CanRemoveParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    bool IGH_VariableParameterComponent.DestroyParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    IGH_Param IGH_VariableParameterComponent.CreateParameter( GH_ParameterSide side, Int32 index )
    {
      return null;
    }
    public void VariableParameterMaintenance( )
    {
    }

    #endregion

    public string GetParamTopology( IGH_Param param )
    {
      string topology = "";
      foreach ( Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths )
      {
        topology += mypath.ToString( false ) + "-" + param.VolatileData.get_Branch( mypath ).Count + " ";
      }
      return topology;
    }

    public List<SpeckleLayer> GetLayers( )
    {
      List<SpeckleLayer> layers = new List<SpeckleLayer>();
      int startIndex = 0;
      int count = 0;
      foreach ( IGH_Param myParam in Params.Output )
      {
        // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
        SpeckleLayer myLayer = new SpeckleLayer(
            myParam.NickName,
            myParam.Name /* aka the orignal guid*/, GetParamTopology( myParam ),
            myParam.VolatileDataCount,
            startIndex,
            count );

        layers.Add( myLayer );
        startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.receiver_2;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid( "{e35c72a5-9e1c-4d79-8879-a9d6db8006fb}" ); }
    }
  }

  public class GhReceiverClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    GhReceiverClient Base;
    Rectangle BaseRectangle;
    Rectangle StreamIdBounds;
    Rectangle StreamNameBounds;
    Rectangle PauseButtonBounds;

    public GhReceiverClientAttributes( GhReceiverClient component ) : base( component )
    {
      Base = component;
    }

    protected override void Layout( )
    {
      base.Layout();
      BaseRectangle = GH_Convert.ToRectangle( Bounds );
      StreamIdBounds = new Rectangle( ( int ) ( BaseRectangle.X + ( BaseRectangle.Width - 120 ) * 0.5 ), BaseRectangle.Y - 25, 120, 20 );
      StreamNameBounds = new Rectangle( StreamIdBounds.X, BaseRectangle.Y - 50, 120, 20 );

      PauseButtonBounds = new Rectangle( ( int ) ( BaseRectangle.X + ( BaseRectangle.Width - 30 ) * 0.5 ), BaseRectangle.Y + BaseRectangle.Height, 30, 30 );

      Rectangle newBaseRectangle = new Rectangle( BaseRectangle.X, BaseRectangle.Y, BaseRectangle.Width, BaseRectangle.Height + 33 );
      Bounds = newBaseRectangle;
    }

    protected override void Render( GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel )
    {
      base.Render( canvas, graphics, channel );
      if ( channel == GH_CanvasChannel.Objects )
      {
        GH_PaletteStyle myStyle = new GH_PaletteStyle( System.Drawing.ColorTranslator.FromHtml( "#B3B3B3" ), System.Drawing.ColorTranslator.FromHtml( "#FFFFFF" ), System.Drawing.ColorTranslator.FromHtml( "#4C4C4C" ) );

        GH_PaletteStyle myTransparentStyle = new GH_PaletteStyle( System.Drawing.Color.FromArgb( 0, 0, 0, 0 ) );

        var streamIdCapsule = GH_Capsule.CreateTextCapsule( box: StreamIdBounds, textbox: StreamIdBounds, palette: GH_Palette.Transparent, text: "ID: " + Base.StreamId, highlight: 0, radius: 5 );
        streamIdCapsule.Render( graphics, myStyle );
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule( box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(R) " + Base.NickName + ( Base.Paused ? " (Paused)" : "" ), highlight: 0, radius: 5 );
        streamNameCapsule.Render( graphics, myStyle );
        streamNameCapsule.Dispose();

        //var pauseStreamingButton = GH_Capsule.CreateTextCapsule(PauseButtonBounds, PauseButtonBounds, GH_Palette.Black, "");
        //pauseStreamingButton.Text = Base.Paused ? "Paused" : "Streaming";
        //pauseStreamingButton.Render(graphics, myStyle);

        var pauseStreamingButton = GH_Capsule.CreateCapsule( PauseButtonBounds, GH_Palette.Transparent, 30, 0 );
        pauseStreamingButton.Render( graphics, Base.Paused ? Properties.Resources.play25px : Properties.Resources.pause25px, myTransparentStyle );
      }
    }

    public override GH_ObjectResponse RespondToMouseDown( GH_Canvas sender, GH_CanvasMouseEvent e )
    {
      if ( e.Button == System.Windows.Forms.MouseButtons.Left )
      {
        if ( ( ( RectangleF ) PauseButtonBounds ).Contains( e.CanvasLocation ) )
        {
          Base.Paused = !Base.Paused;
          Base.ExpireSolution( true );
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown( sender, e );
    }

  }
};
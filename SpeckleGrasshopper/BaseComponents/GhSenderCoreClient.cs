﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Diagnostics;
using System.Threading;

using Grasshopper.Kernel.Parameters;
using GH_IO.Serialization;

using SpeckleCore;
using SpeckleRhinoConverter;
using SpecklePopup;

using System.Dynamic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Collections.Specialized;

using SpeckleGrasshopper.Properties;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using System.Timers;
using System.Threading.Tasks;
using Grasshopper.Kernel.Special;
using System.Collections;

namespace SpeckleGrasshopper
{
  public class GhSenderClient : GH_Component, IGH_VariableParameterComponent
  {
    public string Log { get; set; }
    public OrderedDictionary JobQueue;

    string RestApi;
    string StreamId;

    public Action ExpireComponentAction;
    
    public SpeckleApiClient mySender;

    public GH_Document Document;

    System.Timers.Timer MetadataSender, DataSender;

    private string BucketName;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();

    public string CurrentJobClient = "none";
    public bool solutionPrepared = false;
    
    public Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();

    public GhSenderClient( )
      : base( "Data Sender", "Anonymous Stream",
          "Sends data to Speckle.",
          "Speckle", "I/O" )
    {
      var hack = new ConverterHack();
      JobQueue = new OrderedDictionary();
    }

    public override void CreateAttributes( )
    {
      m_attributes = new GhSenderClientAttributes( this );
    }

    public override bool Write( GH_IWriter writer )
    {
      try
      {
        if ( mySender != null )
          using ( var ms = new MemoryStream() )
          {
            var formatter = new BinaryFormatter();
            formatter.Serialize( ms, mySender );
            var arr = ms.ToArray();
            var arrr = arr;
            writer.SetByteArray( "speckleclient", ms.ToArray() );
          }
      }
      catch ( Exception err )
      {
        throw err;
      }
      return base.Write( writer );
    }

    public override bool Read( GH_IReader reader )
    {
      try
      {
        var serialisedClient = reader.GetByteArray( "speckleclient" );
        var copy = serialisedClient;
        using ( var ms = new MemoryStream() )
        {
          ms.Write( serialisedClient, 0, serialisedClient.Length );
          ms.Seek( 0, SeekOrigin.Begin );
          mySender = ( SpeckleApiClient ) new BinaryFormatter().Deserialize( ms );
          var x = mySender;
          RestApi = mySender.BaseUrl;
          StreamId = mySender.StreamId;
        }
      }
      catch ( Exception err )
      {
        throw err;
      }
      return base.Read( reader );
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );
      Document = this.OnPingDocument();

      if ( mySender == null )
      {
        this.NickName = "Initialising...";
        this.Locked = true;

        var myForm = new SpecklePopup.MainWindow();

        var some = new System.Windows.Interop.WindowInteropHelper( myForm )
        {
          Owner = Rhino.RhinoApp.MainWindowHandle()
        };

        myForm.ShowDialog();

        if ( myForm.restApi != null && myForm.apitoken != null )
        {
          mySender = new SpeckleApiClient( myForm.restApi );
          RestApi = myForm.restApi;
          mySender.IntializeSender( myForm.apitoken, Document.DisplayName, "Grasshopper", Document.DocumentID.ToString() ).ContinueWith( task =>
                {
                  Rhino.RhinoApp.MainApplicationWindow.Invoke( ExpireComponentAction );
                } );
        }
        else
        {
          AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Account selection failed" );
          return;
        }
      }
      else
      {
      }

      mySender.OnReady += ( sender, e ) =>
      {
        StreamId = mySender.StreamId;
        this.Locked = false;
        this.NickName = "Anonymous Stream";
        //this.UpdateMetadata();
        Rhino.RhinoApp.MainApplicationWindow.Invoke( ExpireComponentAction );
      };

      mySender.OnWsMessage += OnWsMessage;

      mySender.OnLogData += ( sender, e ) =>
      {
        this.Log += DateTime.Now.ToString( "dd:HH:mm:ss " ) + e.EventData + "\n";
      };

      mySender.OnError += ( sender, e ) =>
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, e.EventName + ": " + e.EventData );
        this.Log += DateTime.Now.ToString( "dd:HH:mm:ss " ) + e.EventData + "\n";
      };

      ExpireComponentAction = ( ) => ExpireSolution( true );
      
      ObjectChanged += ( sender, e ) => UpdateMetadata();

      foreach ( var param in Params.Input )
        param.ObjectChanged += ( sender, e ) => UpdateMetadata();

      MetadataSender = new System.Timers.Timer( 1000 ) { AutoReset = false, Enabled = false };
      MetadataSender.Elapsed += MetadataSender_Elapsed;

      DataSender = new System.Timers.Timer( 2000 ) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;

      ObjectCache = new Dictionary<string, SpeckleObject>();
    }

    public virtual void OnWsMessage( object source, SpeckleEventArgs e )
    {

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, e.EventObject.args.eventType + "received at " + DateTime.Now + " from " + e.EventObject.senderId);
        switch ((string)e.EventObject.args.eventType)
        {
            case "get-definition-io":
                List<SpeckleInput> speckleInputs = null;
                List<SpeckleOutput> speckleOutputs = null;
                GetSpeckleParams(ref speckleInputs, ref speckleOutputs);

                Dictionary<string, object> message = new Dictionary<string, object>();
                message["eventType"] = "get-def-io-response";
                message["controllers"] = speckleInputs;
                message["outputs"] = "A list of outputs";
                mySender.SendMessage(e.EventObject.senderId, message);
                break;

            case "compute-request":
                var key = (string)e.EventObject.senderId;
                if (JobQueue.Contains((string)e.EventObject.senderId))
                    JobQueue[key] = e.EventObject.args.requestParameters;
                else
                    JobQueue.Add(key, e.EventObject.args.requestParameters);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, Document.SolutionState.ToString());
                    if (!solutionPrepared)
                    {
                        System.Collections.DictionaryEntry t = JobQueue.Cast<DictionaryEntry>().ElementAt(0);
                        CurrentJobClient = (string)t.Key;
                        PrepareSolution((IEnumerable)t.Value);
                        solutionPrepared = true;
                        return;
                    }
                    break;
            default:
                Log += DateTime.Now.ToString("dd:HH:mm:ss") + " Defaulted, could not parse event. \n";
                break;
        }
        Debug.WriteLine( "[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease." );
    }

    private void GetSpeckleParams(ref List<SpeckleInput> speckleInputs, ref List<SpeckleOutput> speckleOutputs)
    {
        speckleInputs = new List<SpeckleInput>();
        speckleOutputs = new List<SpeckleOutput>();
        foreach (var comp in Document.Objects)
        {
            var slider = comp as GH_NumberSlider;
            if (slider != null)
            {
                if (slider.NickName.Contains("SPK_IN"))
                {
                    var n = new SpeckleInput();
                    n.Min = (float)slider.Slider.Minimum;
                    n.Max = (float)slider.Slider.Maximum;
                    n.Value = (float)slider.Slider.Value;
                    //n.Step = getSliderStep(slider.Slider);
                    //n.OrderIndex = Convert.ToInt32(slider.NickName.Split(':')[1]);
                    //n.Name = slider.NickName.Split(':')[2];
                    n.Name = slider.NickName;
                    n.InputType = "Slider";
                    n.Guid = slider.InstanceGuid.ToString();
                    speckleInputs.Add(n);
                }
            }
        }
    }

    public override void RemovedFromDocument( GH_Document document )
    {
      if ( mySender != null ) mySender.Dispose();
      base.RemovedFromDocument( document );
    }

    public override void DocumentContextChanged( GH_Document document, GH_DocumentContext context )
    {
      base.DocumentContextChanged( document, context );
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

      base.AppendAdditionalMenuItems( menu );
      GH_DocumentObject.Menu_AppendItem( menu, "Force refresh.", ( sender, e ) =>
      {
        if ( StreamId != null )
          DataSender.Start();
      } );

      GH_DocumentObject.Menu_AppendSeparator( menu );

      GH_DocumentObject.Menu_AppendItem( menu, "View stream.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi.Replace( "api/v1", "view" ) + @"/?streams=" + StreamId );
       } );

      GH_DocumentObject.Menu_AppendItem( menu, "(API) View stream data.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi + @"/streams/" + StreamId );
       } );

      GH_DocumentObject.Menu_AppendItem( menu, "(API) View objects data online.", ( sender, e ) =>
       {
         if ( StreamId == null ) return;
         System.Diagnostics.Process.Start( RestApi + @"/streams/" + StreamId + @"/objects?omit=displayValue,base64" );
       } );

      GH_DocumentObject.Menu_AppendSeparator( menu );
      GH_DocumentObject.Menu_AppendItem( menu, "Save current stream as a version.", ( sender, e ) =>
       {
         var cloneResult = mySender.StreamCloneAsync( StreamId ).Result;
         mySender.Stream.Children.Add( cloneResult.Clone.StreamId );

         mySender.BroadcastMessage( new { eventType = "update-children" } );

         System.Windows.MessageBox.Show( "Stream version saved. CloneId: " + cloneResult.Clone.StreamId );
       } );

      if ( mySender.Stream == null ) return;

      GH_DocumentObject.Menu_AppendSeparator( menu );
      if ( mySender.Stream.Parent == null )
        GH_DocumentObject.Menu_AppendItem( menu: menu, text: "This is a parent stream.", enabled: false, click: null );
      else
        GH_DocumentObject.Menu_AppendItem( menu: menu, text: "Parent: " + mySender.Stream.Parent, click: ( sender, e ) =>
         {
           System.Windows.Clipboard.SetText( mySender.Stream.Parent );
           System.Windows.MessageBox.Show( "Parent id copied to clipboard. Share away!" );
         } );
      GH_DocumentObject.Menu_AppendSeparator( menu );


      GH_DocumentObject.Menu_AppendSeparator( menu );
      GH_DocumentObject.Menu_AppendItem( menu, "Children:" );
      GH_DocumentObject.Menu_AppendSeparator( menu );

      foreach ( string childId in mySender.Stream.Children )
      {
        GH_DocumentObject.Menu_AppendItem( menu, "Child " + childId, ( sender, e ) =>
         {
           System.Windows.Clipboard.SetText( childId );
           System.Windows.MessageBox.Show( "Child id copied to clipboard. Share away!" );
         } );
      }
    }

    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
      pManager.AddGenericParameter( "A", "A", "A is for Apple", GH_ParamAccess.tree );
      pManager[ 0 ].Optional = true;
      pManager.AddGenericParameter( "B", "B", "B is for Book", GH_ParamAccess.tree );
      pManager[ 1 ].Optional = true;
      pManager.AddGenericParameter( "C", "C", "C is for Car", GH_ParamAccess.tree );
      pManager[ 2 ].Optional = true;

    }

    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
      pManager.AddTextParameter( "log", "L", "Log data.", GH_ParamAccess.item );
      pManager.AddTextParameter( "stream id", "ID", "The stream's id.", GH_ParamAccess.item );
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      if ( mySender == null ) return;
      this.Message = "JobQueue: " + JobQueue.Count;

      StreamId = mySender.StreamId;

      DA.SetData( 0, Log );
      DA.SetData( 1, mySender.StreamId );

      if ( !mySender.IsConnected ) return;
        
      if (JobQueue.Count == 0)
        {
           UpdateData();
           return;
        }

        else
        {
            solutionPrepared = false;
            var BucketObjects = GetData();
            var convertedObjects = Converter.Serialise(BucketObjects).Select(obj =>
            {
                if (ObjectCache.ContainsKey(obj.Hash))
                    return new SpecklePlaceholder() { Hash = obj.Hash, _id = ObjectCache[obj.Hash]._id };
                return obj;
            });

            JobQueue.RemoveAt(0);
            this.Message = "JobQueue: " + JobQueue.Count;
            if (JobQueue.Count != 0)
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }

        UpdateData();
    }
    
    private void PrepareSolution(IEnumerable args)
    {
        var x = args;

        foreach (dynamic param in args)
        {
            IGH_DocumentObject controller = null;
            try
            {
                controller = Document.Objects.First(doc => doc.InstanceGuid.ToString() == param.guid);
            }
            catch { }
            if (controller != null)
                switch ((string)param.inputType)
                {
                    case "TextPanel":
                        GH_Panel panel = controller as GH_Panel;
                        panel.UserText = (string)param.value;
                        panel.ExpireSolution(false);
                        break;
                    case "Slider":
                        GH_NumberSlider slider = controller as GH_NumberSlider;
                        slider.SetSliderValue(decimal.Parse(param.value.ToString()));
                        break;
                    //            case "Point":
                    //              PointController p = controller as PointController;
                    //              var xxxx = p;
                    //              p.setParam( ( double ) param.value.X, ( double ) param.value.Y, ( double ) param.value.Z );
                    //              break;
                    case "Toggle":
                        break;
                    default:
                        break;
                }
        }
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }
    
    public void UpdateData( )
    {
      BucketName = this.NickName;
      BucketLayers = this.GetLayers();
      BucketObjects = this.GetData();

      DataSender.Start();
    }

    private void DataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      if ( MetadataSender.Enabled )
      {
        //  start the timer again, as we need to make sure we're updating
        DataSender.Start();
        return;
      }

      this.Message = String.Format( "Converting {0} \n objects", BucketObjects.Count );

      var convertedObjects = Converter.Serialise( BucketObjects ).Select( obj =>
         {
           if ( ObjectCache.ContainsKey( obj.Hash ) )
             return new SpecklePlaceholder() { Hash = obj.Hash, _id = ObjectCache[ obj.Hash ]._id };
           return obj;
         } ).ToList();

      this.Message = String.Format( "Creating payloads" );

      long totalBucketSize = 0;
      long currentBucketSize = 0;
      List<List<SpeckleObject>> objectUpdatePayloads = new List<List<SpeckleObject>>();
      List<SpeckleObject> currentBucketObjects = new List<SpeckleObject>();
      List<SpeckleObject> allObjects = new List<SpeckleObject>();

      foreach ( SpeckleObject convertedObject in convertedObjects )
      {
        long size = Converter.getBytes( convertedObject ).Length;
        currentBucketSize += size;
        totalBucketSize += size;
        currentBucketObjects.Add( convertedObject );

        if ( currentBucketSize > 5e5 ) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
        {
          Debug.WriteLine( "Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count );
          objectUpdatePayloads.Add( currentBucketObjects );
          currentBucketObjects = new List<SpeckleObject>();
          currentBucketSize = 0;
        }
      }

      // add  the last bucket 
      if ( currentBucketObjects.Count > 0 )
        objectUpdatePayloads.Add( currentBucketObjects );

      Debug.WriteLine( "Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000 );

      if ( objectUpdatePayloads.Count > 100 )
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "This is a humongous update, in the range of ~50mb. For now, create more streams instead of just one massive one! Updates will be faster and snappier, and you can combine them back together at the other end easier." );
        return;
      }

      int k = 0;
      List<ResponseObject> responses = new List<ResponseObject>();
      foreach ( var payload in objectUpdatePayloads )
      {
        this.Message = String.Format( "Sending payload\n{0} / {1}", k++, objectUpdatePayloads.Count );

        responses.Add( mySender.ObjectCreateAsync( payload ).Result );
      }

      this.Message = "Updating stream...";

      // create placeholders for stream update payload
      List<SpeckleObject> placeholders = new List<SpeckleObject>();
      foreach ( var myResponse in responses )
        foreach ( var obj in myResponse.Resources ) placeholders.Add( new SpecklePlaceholder() { _id = obj._id } );

      SpeckleStream updateStream = new SpeckleStream()
      {
        Layers = BucketLayers,
        Name = BucketName,
        Objects = placeholders
      };

      // set some base properties (will be overwritten)
      var baseProps = new Dictionary<string, object>();
      baseProps[ "units" ] = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      baseProps[ "tolerance" ] = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      baseProps[ "angleTolerance" ] = Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      updateStream.BaseProperties = baseProps;

      var response = mySender.StreamUpdateAsync( mySender.StreamId, updateStream ).Result;

      mySender.BroadcastMessage( new { eventType = "update-global" } );

      // put the objects in the cache 
      int l = 0;
      foreach ( var obj in placeholders )
      {
        ObjectCache[ convertedObjects[ l ].Hash ] = placeholders[ l ];
        l++;
      }

      Log += response.Message;
      AddRuntimeMessage( GH_RuntimeMessageLevel.Remark, "Data sent at " + DateTime.Now );
      Message = "Data sent\n@" + DateTime.Now.ToString( "hh:mm:ss" );
    }

    public void UpdateMetadata( )
    {
      BucketName = this.NickName;
      BucketLayers = this.GetLayers();

      MetadataSender.Start();
    }

    private void MetadataSender_Elapsed( object sender, ElapsedEventArgs e )
    {
      // we do not need to enque another metadata sending event as the data update superseeds the metadata one.
      if ( DataSender.Enabled ) { return; };
      SpeckleStream updateStream = new SpeckleStream()
      {
        Name = BucketName,
        Layers = BucketLayers
      };

      var updateResult = mySender.StreamUpdateAsync( mySender.StreamId, updateStream ).Result;

      Log += updateResult.Message;
      mySender.BroadcastMessage( new { eventType = "update-meta" } );
    }


    public List<object> GetData( )
    {
      List<object> data = new List<dynamic>();
      foreach ( IGH_Param myParam in Params.Input )
      {
        foreach ( object o in myParam.VolatileData.AllData( false ) )
          data.Add( o );
      }

      data = data.Select( obj =>
      {
        try
        {
          return obj.GetType().GetProperty( "Value" ).GetValue( obj );
        } catch
        {
          return null;
        }
      } ).ToList();

      return data;
    }

    public List<Layer> GetLayers( )
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int count = 0;
      foreach ( IGH_Param myParam in Params.Input )
      {
        Layer myLayer = new Layer(
            myParam.NickName,
            myParam.InstanceGuid.ToString(),
            GetParamTopology( myParam ),
            myParam.VolatileDataCount,
            startIndex,
            count );

        layers.Add( myLayer );
        startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }

    public string GetParamTopology( IGH_Param param )
    {
      string topology = "";
      foreach ( Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths )
      {
        topology += mypath.ToString( false ) + "-" + param.VolatileData.get_Branch( mypath ).Count + " ";
      }
      return topology;
    }

    bool IGH_VariableParameterComponent.CanInsertParameter( GH_ParameterSide side, int index )
    {
      if ( side == GH_ParameterSide.Input )
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    bool IGH_VariableParameterComponent.CanRemoveParameter( GH_ParameterSide side, int index )
    {
      //We can only remove from the input
      if ( side == GH_ParameterSide.Input && Params.Input.Count > 1 )
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    IGH_Param IGH_VariableParameterComponent.CreateParameter( GH_ParameterSide side, int index )
    {
      Param_GenericObject param = new Param_GenericObject()
      {
        Name = GH_ComponentParamServer.InventUniqueNickname( "ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input )
      };
      param.NickName = param.Name;
      param.Description = "Things to be sent around.";
      param.Optional = true;
      param.Access = GH_ParamAccess.tree;

      param.AttributesChanged += ( sender, e ) => Debug.WriteLine( "Attributes have changed! (of param)" );
      param.ObjectChanged += ( sender, e ) => UpdateMetadata();

      this.UpdateMetadata();
      return param;
    }

    bool IGH_VariableParameterComponent.DestroyParameter( GH_ParameterSide side, int index )
    {
      this.UpdateMetadata();
      return true;
    }

    void IGH_VariableParameterComponent.VariableParameterMaintenance( )
    {
    }

    public string GetTopology( IGH_Param param )
    {
      string topology = "";
      foreach ( Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths )
      {
        topology += mypath.ToString( false ) + "-" + param.VolatileData.get_Branch( mypath ).Count + " ";
      }
      return topology;
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.sender_2;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid( "{e66e6873-ddcd-4089-93ff-75ae09f8ada3}" ); }
    }
  }

  public class GhSenderClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    GhSenderClient Base;
    Rectangle BaseRectangle;
    Rectangle StreamIdBounds;
    Rectangle StreamNameBounds;

    public GhSenderClientAttributes( GhSenderClient component ) : base( component )
    {
      Base = component;
    }

    protected override void Layout( )
    {
      base.Layout();
      BaseRectangle = GH_Convert.ToRectangle( Bounds );
      StreamIdBounds = new Rectangle( ( int ) ( BaseRectangle.X + ( BaseRectangle.Width - 120 ) * 0.5 ), BaseRectangle.Y - 25, 120, 20 );
      StreamNameBounds = new Rectangle( StreamIdBounds.X, BaseRectangle.Y - 50, 120, 20 );
    }

    protected override void Render( GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel )
    {
      base.Render( canvas, graphics, channel );
      if ( channel == GH_CanvasChannel.Objects )
      {
        GH_PaletteStyle myStyle = new GH_PaletteStyle( System.Drawing.ColorTranslator.FromHtml( "#B3B3B3" ), System.Drawing.ColorTranslator.FromHtml( "#FFFFFF" ), System.Drawing.ColorTranslator.FromHtml( "#4C4C4C" ) );

        GH_PaletteStyle myTransparentStyle = new GH_PaletteStyle( System.Drawing.Color.FromArgb( 0, 0, 0, 0 ) );

        var streamIdCapsule = GH_Capsule.CreateTextCapsule( box: StreamIdBounds, textbox: StreamIdBounds, palette: GH_Palette.Transparent, text: "ID: " + Base.mySender.StreamId, highlight: 0, radius: 5 );
        streamIdCapsule.Render( graphics, myStyle );
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule( box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(S) " + Base.NickName, highlight: 0, radius: 5 );
        streamNameCapsule.Render( graphics, myStyle );
        streamNameCapsule.Dispose();
      }
    }

  }
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using SpeckleCore;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper
{
  public class GhSenderClient : GH_Component, IGH_VariableParameterComponent
  {
    public string Log { get; set; }
    public OrderedDictionary JobQueue;
    private string RestApi;
    private string StreamId;

    public Action ExpireComponentAction;

    public SpeckleApiClient Client;

    public GH_Document Document;
    private System.Timers.Timer MetadataSender, DataSender;

    private string BucketName;
    private List<Layer> BucketLayers = new List<Layer>();
    private List<object> BucketObjects = new List<object>();

    public string CurrentJobClient = "none";
    public bool SolutionPrepared = false;

    public bool EnableRemoteControl = false;
    private bool WasSerialised = false;
    private bool DocumentIsClosing = false;
    private bool FirstSendUpdate = true;

    public bool IsSendingUpdate = false;
    private List<SpeckleInput> DefaultSpeckleInputs = null;
    private List<SpeckleOutput> DefaultSpeckleOutputs = null;

    public Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();

    public bool ManualMode = false;

    public string State;

    public GhSenderClient()
      : base("Data Sender", "Anonymous Stream",
          "Sends data to Speckle.",
          "Speckle", "I/O")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();

      JobQueue = new OrderedDictionary();
    }

    public override void CreateAttributes()
    {
      m_attributes = new GhSenderClientAttributes(this);
    }

    public override bool Write(GH_IWriter writer)
    {
      try
      {
        if (Client != null)
        {
          using (var ms = new MemoryStream())
          {
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, Client);
            var arr = ms.ToArray();
            var arrr = arr;
            writer.SetByteArray("speckleclient", ms.ToArray());
            writer.SetBoolean("remotecontroller", EnableRemoteControl);
            writer.SetBoolean("manualmode", ManualMode);
          }
        }
      }
      catch (Exception err)
      {
        throw err;
      }
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      try
      {
        var serialisedClient = reader.GetByteArray("speckleclient");
        var copy = serialisedClient;
        using (var ms = new MemoryStream())
        {
          ms.Write(serialisedClient, 0, serialisedClient.Length);
          ms.Seek(0, SeekOrigin.Begin);
          Client = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
          var x = Client;
          RestApi = Client.BaseUrl;
          StreamId = Client.StreamId;
          WasSerialised = true;
        }

        reader.TryGetBoolean("remotecontroller", ref EnableRemoteControl);
        reader.TryGetBoolean("manualmode", ref ManualMode);
      }
      catch (Exception err)
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Failed to reinitialise sender." );
        //throw err;
      }
      return base.Read(reader);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Document = OnPingDocument();

      if (Client == null)
      {
        NickName = "Initialising...";
        Locked = true;

        var myForm = new SpecklePopup.MainWindow(false, true);

        var some = new System.Windows.Interop.WindowInteropHelper(myForm)
        {
          Owner = Rhino.RhinoApp.MainWindowHandle()
        };

        myForm.ShowDialog();

        if (myForm.restApi != null && myForm.apitoken != null)
        {
          Client = new SpeckleApiClient(myForm.restApi);
          RestApi = myForm.restApi;
          Client.IntializeSender(myForm.apitoken, Document.DisplayName, "Grasshopper", Document.DocumentID.ToString()).ContinueWith(task =>
          {
            Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
          });
        }
        else
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Account selection failed");
          return;
        }

      }
      else
      {
      }

      Client.OnReady += (sender, e) =>
      {
        StreamId = Client.StreamId;
        if (!WasSerialised)
        {
          Locked = false;
          NickName = "Anonymous Stream";
        }
        ////this.UpdateMetadata();
        Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
      };

      Client.OnWsMessage += OnWsMessage;

      Client.OnLogData += (sender, e) =>
      {
        Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
      };

      Client.OnError += (sender, e) =>
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventName + ": " + e.EventData);
        Log += DateTime.Now.ToString("dd:HH:mm:ss ") + e.EventData + "\n";
      };

      ExpireComponentAction = () => ExpireSolution(true);

      ObjectChanged += (sender, e) => UpdateMetadata();

      foreach (var param in Params.Input)
      {
        param.ObjectChanged += (sender, e) => UpdateMetadata();
      }

      MetadataSender = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };
      MetadataSender.Elapsed += MetadataSender_Elapsed;

      DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
      DataSender.Elapsed += DataSender_Elapsed;

      ObjectCache = new Dictionary<string, SpeckleObject>();

      Grasshopper.Instances.DocumentServer.DocumentRemoved += DocumentServer_DocumentRemoved;
    }

    private void DocumentServer_DocumentRemoved(GH_DocumentServer sender, GH_Document doc)
    {
      if (doc.DocumentID == Document.DocumentID)
      {
        DocumentIsClosing = true;
      }
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      try
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, e.EventObject.args.eventType + "received at " + DateTime.Now + " from " + e.EventObject.senderId);
        switch ((string)e.EventObject.args.eventType)
        {
          case "get-definition-io":
            if (EnableRemoteControl == false)
            {
              return;
            }

            Dictionary<string, object> message = new Dictionary<string, object>();
            message["eventType"] = "get-def-io-response";
            message["controllers"] = DefaultSpeckleInputs;
            message["outputs"] = DefaultSpeckleOutputs;

            Client.SendMessage(e.EventObject.senderId, message);
            break;

          case "compute-request":
            if (EnableRemoteControl == true)
            {
              var requestClientId = (string)e.EventObject.senderId;
              if (JobQueue.Contains(requestClientId))
              {
                JobQueue[requestClientId] = e.EventObject.args.requestParameters;
              }
              else
              {
                JobQueue.Add(requestClientId, e.EventObject.args.requestParameters);
              }

              AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, Document.SolutionState.ToString());

              if (JobQueue.Count == 1) // means we  just added one, so we need to start the solve loop
              {
                Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
              }
            }
            else
            {
              Dictionary<string, object> computeMessage = new Dictionary<string, object>();
              computeMessage["eventType"] = "compute-request-error";
              computeMessage["response"] = "Remote control is disabled for this sender";
              Client.SendMessage(e.EventObject.senderId, computeMessage);
            }
            break;
          default:
            Log += DateTime.Now.ToString("dd:HH:mm:ss") + " Defaulted, could not parse event. \n";
            break;
        }
      }
      catch
      {

      }
      Debug.WriteLine("[Gh Sender] Got a volatile message. Extend this class and implement custom protocols at ease.");
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
            n.Min = (double)slider.Slider.Minimum;
            n.Max = (double)slider.Slider.Maximum;
            n.Value = (double)slider.Slider.Value;
            n.Step = getSliderStep(slider.Slider);
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

    private double getSliderStep(GH_SliderBase gH_NumberSlider)
    {
      switch (gH_NumberSlider.Type)
      {
        case GH_SliderAccuracy.Float:
          double i = 1 / Math.Pow(10, gH_NumberSlider.DecimalPlaces);
          return i;
        case GH_SliderAccuracy.Integer:
          return 1;
        case GH_SliderAccuracy.Even:
          return 2;
        case GH_SliderAccuracy.Odd:
          return 2;
      }
      throw new NotImplementedException();
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      if (Client != null)
      {
        //Client.StreamUpdateAsync(Client.StreamId, new SpeckleStream() { Deleted = true });
        Client.Dispose(false);
      }
      base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
      base.DocumentContextChanged(document, context);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Copy streamId (" + StreamId + ") to clipboard.", (sender, e) =>
      {
        if (StreamId != null)
        {
          System.Windows.Clipboard.SetText(StreamId);
        }
      });

      GH_DocumentObject.Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Force refresh.", (sender, e) =>
     {
       if (StreamId != null)
       {
         DataSender.Start();
       }
     });

      GH_DocumentObject.Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Toggle Manual Mode (Status: " + ManualMode + ")", (sender, e) =>
     {
       ManualMode = !ManualMode;
       m_attributes.ExpireLayout();

       if (!ManualMode && State == "Expired")
       {
         UpdateData();
       }
     });

      GH_DocumentObject.Menu_AppendSeparator(menu);

      GH_DocumentObject.Menu_AppendItem(menu, "View stream.", (sender, e) =>
      {
        if (StreamId == null)
        {
          return;
        }

        System.Diagnostics.Process.Start(RestApi.Replace("api/v1", "view") + @"/?streams=" + StreamId);
      });

      GH_DocumentObject.Menu_AppendItem(menu, "(API) View stream data.", (sender, e) =>
      {
        if (StreamId == null)
        {
          return;
        }

        System.Diagnostics.Process.Start(RestApi + @"/streams/" + StreamId);
      });

      GH_DocumentObject.Menu_AppendItem(menu, "(API) View objects data online.", (sender, e) =>
      {
        if (StreamId == null)
        {
          return;
        }

        System.Diagnostics.Process.Start(RestApi + @"/streams/" + StreamId + @"/objects?omit=displayValue,base64");
      });

      GH_DocumentObject.Menu_AppendSeparator(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Save current stream as a version.", (sender, e) =>
      {
        var cloneResult = Client.StreamCloneAsync(StreamId).Result;
        Client.Stream.Children.Add(cloneResult.Clone.StreamId);

        Client.BroadcastMessage("stream", Client.StreamId, new { eventType = "update-children" });

        System.Windows.MessageBox.Show("Stream version saved. CloneId: " + cloneResult.Clone.StreamId);
      });

      if (Client.Stream == null)
      {
        return;
      }

      GH_DocumentObject.Menu_AppendSeparator(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Enable remote control of definition", (sender, e) =>
      {
        EnableRemoteControl = !EnableRemoteControl;
        if (EnableRemoteControl)
        {
          List<SpeckleInput> speckleInputs = null;
          List<SpeckleOutput> speckleOutputs = null;
          GetSpeckleParams(ref speckleInputs, ref speckleOutputs);

          DefaultSpeckleInputs = speckleInputs;
          DefaultSpeckleOutputs = speckleOutputs;
        }
      }, true, EnableRemoteControl);

      if (EnableRemoteControl)
      {
        GH_DocumentObject.Menu_AppendItem(menu, "Update/Set the default state for the controller stream.", (sender, e) =>
       {
         SetDefaultState(true);
         System.Windows.MessageBox.Show("Updated default state.");
       }, true);
      }

      GH_DocumentObject.Menu_AppendSeparator(menu);

      if (Client.Stream.Parent == null)
      {
        GH_DocumentObject.Menu_AppendItem(menu: menu, text: "This is a parent stream.", enabled: false, click: null);
      }
      else
      {
        GH_DocumentObject.Menu_AppendItem(menu: menu, text: "Parent: " + Client.Stream.Parent, click: (sender, e) =>
        {
          System.Windows.Clipboard.SetText(Client.Stream.Parent);
          System.Windows.MessageBox.Show("Parent id copied to clipboard. Share away!");
        });
      }

      GH_DocumentObject.Menu_AppendSeparator(menu);


      GH_DocumentObject.Menu_AppendSeparator(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Children:");
      GH_DocumentObject.Menu_AppendSeparator(menu);

      foreach (string childId in Client.Stream.Children)
      {
        GH_DocumentObject.Menu_AppendItem(menu, "Child " + childId, (sender, e) =>
        {
          System.Windows.Clipboard.SetText(childId);
          System.Windows.MessageBox.Show("Child id copied to clipboard. Share away!");
        });
      }
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("A", "A", "A is for Apple", GH_ParamAccess.tree);
      pManager[0].Optional = true;
      pManager.AddGenericParameter("B", "B", "B is for Book", GH_ParamAccess.tree);
      pManager[1].Optional = true;
      pManager.AddGenericParameter("C", "C", "C is for Car", GH_ParamAccess.tree);
      pManager[2].Optional = true;

    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("log", "L", "Log data.", GH_ParamAccess.item);
      pManager.AddTextParameter("stream id", "ID", "The stream's id.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (Client == null)
      {
        return;
      }

      if (EnableRemoteControl)
      {
        Message = "JobQueue: " + JobQueue.Count;
      }

      StreamId = Client.StreamId;

      DA.SetData(0, Log);
      DA.SetData(1, Client.StreamId);

      if (!Client.IsConnected)
      {
        return;
      }

      if (WasSerialised && FirstSendUpdate)
      {
        FirstSendUpdate = false;
        return;
      }

      State = "Expired";

      // All flags are good to start an update
      if (!EnableRemoteControl && !ManualMode)
      {
        UpdateData();
        return;
      }
      // 
      else if (!EnableRemoteControl && ManualMode)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "State is expired, update push is required.");
        return;
      }

      #region RemoteControl

      // Code below deals with the remote control functionality.
      // Proceed at your own risk.
      if (JobQueue.Count == 0)
      {
        SetDefaultState();
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Updated default state for remote control.");
        return;
      }

      // prepare solution and exit
      if (!SolutionPrepared && JobQueue.Count != 0)
      {
        System.Collections.DictionaryEntry t = JobQueue.Cast<DictionaryEntry>().ElementAt(0);
        Document.ScheduleSolution(1, PrepareSolution);
        return;
      }

      // send out solution and exit
      if (SolutionPrepared)
      {
        SolutionPrepared = false;
        var BucketObjects = GetData();
        var BucketLayers = GetLayers();
        var convertedObjects = Converter.Serialise(BucketObjects).Select(obj =>
        {
          if (ObjectCache.ContainsKey(obj.Hash))
          {
            return new SpecklePlaceholder() { Hash = obj.Hash, _id = ObjectCache[obj.Hash]._id };
          }

          return obj;
        });


        // theoretically this should go through the same flow as in DataSenderElapsed(), ie creating
        // buckets for staggered updates, etc. but we're lazy to untangle that logic for now

        var responseClone = Client.StreamCloneAsync(StreamId).Result;
        var responseStream = new SpeckleStream();

        responseStream.IsComputedResult = true;

        responseStream.Objects = convertedObjects.ToList();
        responseStream.Layers = BucketLayers;

        List<SpeckleInput> speckleInputs = null;
        List<SpeckleOutput> speckleOutputs = null;
        GetSpeckleParams(ref speckleInputs, ref speckleOutputs);

        responseStream.GlobalMeasures = new { input = speckleInputs, output = speckleOutputs };

        // go unblocking
        var responseCloneUpdate = Client.StreamUpdateAsync(responseClone.Clone.StreamId, responseStream).ContinueWith(tres =>
     {
       Client.SendMessage(CurrentJobClient, new { eventType = "compute-response", streamId = responseClone.Clone.StreamId });
     });


        JobQueue.RemoveAt(0);
        Message = "JobQueue: " + JobQueue.Count;

        if (JobQueue.Count != 0)
        {
          Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponentAction);
        }
      }

      #endregion
    }

    #region Remote Control Helpers
    // Remote controller setting up the solution
    private void PrepareSolution(GH_Document gH_Document)
    {
      System.Collections.DictionaryEntry t = JobQueue.Cast<DictionaryEntry>().ElementAt(0);
      CurrentJobClient = (string)t.Key;

      foreach (dynamic param in (IEnumerable)t.Value)
      {
        IGH_DocumentObject controller = null;
        try
        {
          controller = Document.Objects.First(doc => doc.InstanceGuid.ToString() == param.guid);
        }
        catch { }
        if (controller != null)
        {
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
            case "Toggle":
              break;
            default:
              break;
          }
        }
      }
      SolutionPrepared = true;
    }

    /// <summary>
    /// Sets the default state for the remote controller. Will update parent stream too.
    /// </summary>
    private void SetDefaultState(bool force = false)
    {
      List<SpeckleInput> speckleInputs = null;
      List<SpeckleOutput> speckleOutputs = null;
      GetSpeckleParams(ref speckleInputs, ref speckleOutputs);

      DefaultSpeckleInputs = speckleInputs;
      DefaultSpeckleOutputs = speckleOutputs;

      if (force)
      {
        ForceUpdateData();
      }
      else
      {
        UpdateData();
      }

      Dictionary<string, object> message = new Dictionary<string, object>();
      message["eventType"] = "default-state-update";
      message["controllers"] = DefaultSpeckleInputs;
      message["outputs"] = DefaultSpeckleOutputs;
      message["originalStreamId"] = Client.StreamId;

      Client.BroadcastMessage( "stream", Client.StreamId, message );
    }
    #endregion

    /// <summary>
    /// Will start timer (500ms).
    /// </summary>
    public void UpdateData()
    {
      if (DocumentIsClosing)
      {
        return;
      }

      BucketName = NickName;
      BucketLayers = GetLayers();
      BucketObjects = GetData();

      DataSender.Start();
    }

    /// <summary>
    /// Bypasses debounce timer.
    /// </summary>
    public void ForceUpdateData()
    {
      BucketName = NickName;
      BucketLayers = GetLayers();
      BucketObjects = GetData();

      SendUpdate();
    }

    private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (!ManualMode)
      {
        SendUpdate();
      }
    }

    /// <summary>
    /// Sends the update to the server.
    /// </summary>
    private void SendUpdate()
    {
      if (MetadataSender.Enabled)
      {
        // start the timer again, as we need to make sure we're updating
        DataSender.Start();
        return;
      }

      if (IsSendingUpdate)
      {
        return;
      }

      IsSendingUpdate = true;

      Message = String.Format("Converting {0} \n objects", BucketObjects.Count);

      var convertedObjects = Converter.Serialise(BucketObjects).ToList();

      Message = String.Format("Creating payloads");

      LocalContext.PruneExistingObjects(convertedObjects, Client.BaseUrl);

      List<SpeckleObject> persistedObjects = new List<SpeckleObject>();

      if (convertedObjects.Count(obj => obj.Type == "Placeholder") != convertedObjects.Count)
      {
        // create the update payloads
        int count = 0;
        var objectUpdatePayloads = new List<List<SpeckleObject>>();
        long totalBucketSize = 0;
        long currentBucketSize = 0;
        var currentBucketObjects = new List<SpeckleObject>();
        var allObjects = new List<SpeckleObject>();
        foreach (SpeckleObject convertedObject in convertedObjects)
        {

          if (count++ % 100 == 0)
          {
            Message = "Converted " + count + " objects out of " + convertedObjects.Count() + ".";
          }

          // size checking & bulk object creation payloads creation
          long size = Converter.getBytes(convertedObject).Length;
          currentBucketSize += size;
          totalBucketSize += size;
          currentBucketObjects.Add(convertedObject);

          // Object is too big?
          if (size > 2e6)
          {

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This stream contains a super big object. These will fail. Sorry for the bad error message - we're working on improving this.");

            currentBucketObjects.Remove(convertedObject);
          }

          if (currentBucketSize > 5e5) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
          {
            Debug.WriteLine("Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count);
            objectUpdatePayloads.Add(currentBucketObjects);
            currentBucketObjects = new List<SpeckleObject>();
            currentBucketSize = 0;
          }
        }

        // add in the last bucket
        if (currentBucketObjects.Count > 0)
        {
          objectUpdatePayloads.Add(currentBucketObjects);
        }

        Debug.WriteLine("Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000);

        // create bulk object creation tasks
        int k = 0;
        List<ResponseObject> responses = new List<ResponseObject>();
        foreach (var payload in objectUpdatePayloads)
        {
          Message = String.Format("{0}/{1}", k++, objectUpdatePayloads.Count);

          try
          {
            var objResponse = Client.ObjectCreateAsync(payload).Result;
            responses.Add(objResponse);
            persistedObjects.AddRange(objResponse.Resources);

            int m = 0;
            foreach (var oL in payload)
            {
              oL._id = objResponse.Resources[m++]._id;
            }

            // push sent objects in the cache non-blocking
            Task.Run(() =>
           {
             foreach (var oL in payload)
             {
               if (oL.Type != "Placeholder" )
               {
                 LocalContext.AddSentObject(oL, Client.BaseUrl);
               }
             }
           });

          }
          catch (Exception err)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err.Message);
            return;
          }
        }
      }
      else
      {
        persistedObjects = convertedObjects;
      }

      // create placeholders for stream update payload
      List<SpeckleObject> placeholders = new List<SpeckleObject>();

      //foreach ( var myResponse in responses )
      foreach (var obj in persistedObjects)
      {
        placeholders.Add(new SpecklePlaceholder() { _id = obj._id });
      }

      SpeckleStream updateStream = new SpeckleStream()
      {
        Layers = BucketLayers,
        Name = BucketName,
        Objects = placeholders
      };

      // set some base properties (will be overwritten)
      var baseProps = new Dictionary<string, object>();
      baseProps["units"] = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
      baseProps["tolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      baseProps["angleTolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
      updateStream.BaseProperties = baseProps;

      var response = Client.StreamUpdateAsync(Client.StreamId, updateStream).Result;

      Client.BroadcastMessage( "stream", Client.StreamId, new { eventType = "update-global" });

      Log += response.Message;
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Data sent at " + DateTime.Now);
      Message = "Data sent\n@" + DateTime.Now.ToString("hh:mm:ss");

      IsSendingUpdate = false;
      State = "Ok";
    }

    public void UpdateMetadata()
    {
      if (DocumentIsClosing)
      {
        return;
      }

      BucketName = NickName;
      BucketLayers = GetLayers();

      MetadataSender.Start();
    }

    private void MetadataSender_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (ManualMode)
      {
        return;
      }
      // we do not need to enque another metadata sending event as the data update superseeds the metadata one.
      if (DataSender.Enabled) { return; };
      SpeckleStream updateStream = new SpeckleStream()
      {
        Name = BucketName,
        Layers = BucketLayers
      };

      var updateResult = Client.StreamUpdateAsync(Client.StreamId, updateStream).Result;

      Log += updateResult.Message;
      Client.BroadcastMessage("stream", Client.StreamId, new { eventType = "update-meta" });
    }

    public void ManualUpdate()
    {
      new Task(() =>
     {
       var cloneResult = Client.StreamCloneAsync(StreamId).Result;
       Client.Stream.Children.Add(cloneResult.Clone.StreamId);

       Client.BroadcastMessage( "stream", Client.StreamId, new { eventType = "update-children" });

       ForceUpdateData();

     }).Start();
    }

    public List<object> GetData()
    {
      List<object> data = new List<dynamic>();
      foreach (IGH_Param myParam in Params.Input)
      {
        foreach (object o in myParam.VolatileData.AllData(false))
        {
          data.Add(o);
        }
      }

      data = data.Select(obj =>
     {
       try
       {
         return obj.GetType().GetProperty("Value").GetValue(obj);
       }
       catch
       {
         return null;
       }
     }).ToList();

      return data;
    }

    public List<Layer> GetLayers()
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int count = 0;
      foreach (IGH_Param myParam in Params.Input)
      {
        Layer myLayer = new Layer(
            myParam.NickName,
            myParam.InstanceGuid.ToString(),
            GetParamTopology(myParam),
            myParam.VolatileDataCount,
            startIndex,
            count);

        layers.Add(myLayer);
        startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }

    public string GetParamTopology(IGH_Param param)
    {
      string topology = "";
      foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
      {
        topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
      }
      return topology;
    }

    bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index)
    {
      //We can only remove from the input
      if (side == GH_ParameterSide.Input && Params.Input.Count > 1)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index)
    {
      Param_GenericObject param = new Param_GenericObject()
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input)
      };
      param.NickName = param.Name;
      param.Description = "Things to be sent around.";
      param.Optional = true;
      param.Access = GH_ParamAccess.tree;

      param.AttributesChanged += (sender, e) => Debug.WriteLine("Attributes have changed! (of param)");
      param.ObjectChanged += (sender, e) => UpdateMetadata();

      UpdateMetadata();
      return param;
    }

    bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index)
    {
      UpdateMetadata();
      return true;
    }

    void IGH_VariableParameterComponent.VariableParameterMaintenance()
    {
    }

    public string GetTopology(IGH_Param param)
    {
      string topology = "";
      foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
      {
        topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
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
      get { return new Guid("{e66e6873-ddcd-4089-93ff-75ae09f8ada3}"); }
    }
  }

  public class GhSenderClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    private GhSenderClient Base;
    private Rectangle BaseRectangle;
    private Rectangle StreamIdBounds;
    private Rectangle StreamNameBounds;
    private Rectangle PushStreamButtonRectangle;

    public GhSenderClientAttributes(GhSenderClient component) : base(component)
    {
      Base = component;
    }

    protected override void Layout()
    {
      base.Layout();
      BaseRectangle = GH_Convert.ToRectangle(Bounds);
      StreamIdBounds = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 120) * 0.5), BaseRectangle.Y - 25, 120, 20);
      StreamNameBounds = new Rectangle(StreamIdBounds.X, BaseRectangle.Y - 50, 120, 20);

      PushStreamButtonRectangle = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 30) * 0.5), BaseRectangle.Y + BaseRectangle.Height, 30, 30);

      if (Base.ManualMode)
      {
        Rectangle newBaseRectangle = new Rectangle(BaseRectangle.X, BaseRectangle.Y, BaseRectangle.Width, BaseRectangle.Height + 33);
        Bounds = newBaseRectangle;
      }
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      if (channel == GH_CanvasChannel.Objects)
      {
        GH_PaletteStyle myStyle = new GH_PaletteStyle(System.Drawing.ColorTranslator.FromHtml(Base.EnableRemoteControl ? "#147DE9" : "#B3B3B3"), System.Drawing.ColorTranslator.FromHtml("#FFFFFF"), System.Drawing.ColorTranslator.FromHtml(Base.EnableRemoteControl ? "#ffffff" : "#4C4C4C"));

        GH_PaletteStyle myTransparentStyle = new GH_PaletteStyle(System.Drawing.Color.FromArgb(0, 0, 0, 0));

        var streamIdCapsule = GH_Capsule.CreateTextCapsule(box: StreamIdBounds, textbox: StreamIdBounds, palette: Base.EnableRemoteControl ? GH_Palette.Black : GH_Palette.Transparent, text: Base.EnableRemoteControl ? "Remote Controller" : "ID: " + (Base.Client != null ? Base.Client.StreamId : "error"), highlight: 0, radius: 5);
        streamIdCapsule.Render(graphics, myStyle);
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule(box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(S) " + Base.NickName, highlight: 0, radius: 5);
        streamNameCapsule.Render(graphics, myStyle);
        streamNameCapsule.Dispose();

        if (Base.ManualMode)
        {
          var pushStreamButton = GH_Capsule.CreateCapsule(PushStreamButtonRectangle, GH_Palette.Pink, 2, 0);
          pushStreamButton.Render(graphics, true ? Properties.Resources.play25px : Properties.Resources.pause25px, myTransparentStyle);
        }
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button == System.Windows.Forms.MouseButtons.Left)
      {
        if (((RectangleF)PushStreamButtonRectangle).Contains(e.CanvasLocation))
        {
          Base.ManualUpdate();
          //Base.ExpireSolution( true );
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }

  }
}


//extern alias SpeckleNewtonsoft;
using SNJ = Newtonsoft.Json;
using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Base;

namespace SpeckleRhino
{
  /// <summary>
  /// Class that holds a rhino receiver client warpped around the
  /// SpeckleApiClient.
  /// </summary>
  [Serializable]
  public class RhinoReceiver : ISpeckleRhinoClient
  {
    public Interop Context { get; set; }

    public SpeckleApiClient Client { get; set; }

    public List<SpeckleObject> Objects { get; set; }

    public SpeckleDisplayConduit Display;

    public string StreamId { get; private set; }

    public bool Paused { get; set; } = false;

    public bool Visible { get; set; } = true;

    public RhinoReceiver(string _payload, Interop _parent)
    {
      Context = _parent;
      dynamic payload = SNJ.JsonConvert.DeserializeObject(_payload);

      StreamId = (string)payload.streamId;

      Client = new SpeckleApiClient((string)payload.account.restApi, true);

      Client.OnReady += Client_OnReady;
      Client.OnLogData += Client_OnLogData;
      Client.OnWsMessage += Client_OnWsMessage;
      Client.OnError += Client_OnError;

      Client.IntializeReceiver((string)payload.streamId, Context.GetDocumentName(), "Rhino", Context.GetDocumentGuid(), (string)payload.account.token);

      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;

      Objects = new List<SpeckleObject>();
    }

    #region events
    private void Client_OnError(object source, SpeckleEventArgs e)
    {
      Context.NotifySpeckleFrame("client-error", StreamId, SNJ.JsonConvert.SerializeObject(e.EventData, Interop.camelCaseSettings));
    }

    public virtual void Client_OnLogData(object source, SpeckleEventArgs e)
    {
      Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject(e.EventData, Interop.camelCaseSettings));
    }

    public virtual void Client_OnReady(object source, SpeckleEventArgs e)
    {
      Context.NotifySpeckleFrame("client-add", StreamId, SNJ.JsonConvert.SerializeObject(new { stream = Client.Stream, client = Client }, Interop.camelCaseSettings));

      Context.UserClients.Add(this);

      UpdateGlobal();
    }

    public virtual void Client_OnWsMessage(object source, SpeckleEventArgs e)
    {
      if (Paused)
      {
        Context.NotifySpeckleFrame("client-expired", StreamId, "");
        return;
      }
      if (e == null) return;
      if (e.EventObject == null) return;
      switch ((string)e.EventObject.args.eventType)
      {
        case "update-global":
          UpdateGlobal();
          break;
        case "update-meta":
          UpdateMeta();
          break;
        case "update-name":
          UpdateName();
          break;
        case "update-object":
          break;
        case "update-children":
          UpdateChildren();
          break;
        default:
          Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject("Unkown event: " + (string)e.EventObject.args.eventType));
          break;
      }
    }
    #endregion

    #region updates

    public void UpdateName()
    {
      try
      {
        var response = Client.StreamGetAsync(StreamId, "fields=name");
        Client.Stream.Name = response.Result.Resource.Name;
        Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson()); // i'm lazy
      }
      catch (Exception err)
      {
        Context.NotifySpeckleFrame("client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject(err.Message));
        Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        return;
      }
    }

    public void UpdateMeta()
    {
      Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject("Metadata update received."));

      try
      {
        var streamGetResponse = Client.StreamGetAsync(StreamId, null).Result;

        if (streamGetResponse.Success == false)
        {
          Context.NotifySpeckleFrame("client-error", StreamId, streamGetResponse.Message);
          Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject("Failed to retrieve global update."));
        }

        Client.Stream = streamGetResponse.Resource;

        Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson());
      }
      catch (Exception err)
      {
        Context.NotifySpeckleFrame("client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject(err.Message));
        Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        return;
      }

    }

    public void UpdateGlobal()
    {
      Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject("Global update received."));
      try
      {
        var streamGetResponse = Client.StreamGetAsync(StreamId, null).Result;
        if (streamGetResponse.Success == false)
        {
          Context.NotifySpeckleFrame("client-error", StreamId, streamGetResponse.Message);
          // TODO
          // Try and get from cache
          // First stream
          // Then objects
        }
        else
        {
          Client.Stream = streamGetResponse.Resource;
          Context.NotifySpeckleFrame("client-metadata-update", StreamId, Client.Stream.ToJson());
          Context.NotifySpeckleFrame("client-is-loading", StreamId, "");

          // add or update the newly received stream in the cache.
          LocalContext.AddOrUpdateStream(Client.Stream, Client.BaseUrl);

          // pass the object list through a cache check 
          LocalContext.GetCachedObjects(Client.Stream.Objects, Client.BaseUrl);

          // filter out the objects that were not in the cache and still need to be retrieved
          var payload = Client.Stream.Objects.Where(o => o.Type == "Placeholder").Select(obj => obj._id).ToArray();

          // how many objects to request from the api at a time
          int maxObjRequestCount = 20;

          // list to hold them into
          var newObjects = new List<SpeckleObject>();

          // jump in `maxObjRequestCount` increments through the payload array
          for (int i = 0; i < payload.Length; i += maxObjRequestCount)
          {
            // create a subset
            var subPayload = payload.Skip(i).Take(maxObjRequestCount).ToArray();

            // get it sync as this is always execed out of the main thread
            var res = Client.ObjectGetBulkAsync(subPayload, "omit=displayValue").Result;

            // put them in our bucket
            newObjects.AddRange(res.Resources);
            Context.NotifySpeckleFrame("client-log", StreamId, SNJ.JsonConvert.SerializeObject(String.Format("Got {0} out of {1} objects.", i, payload.Length)));
          }

          // populate the retrieved objects in the original stream's object list
          foreach (var obj in newObjects)
          {
            var locationInStream = Client.Stream.Objects.FindIndex(o => o._id == obj._id);
            try { Client.Stream.Objects[locationInStream] = obj; } catch { }
          }

          // add objects to cache async
          Task.Run(() =>
         {
           foreach (var obj in newObjects)
           {
             LocalContext.AddCachedObject(obj, Client.BaseUrl);
           }
         });

          DisplayContents();
          Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        }
      }
      catch (Exception err)
      {
        Context.NotifySpeckleFrame("client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject(err.Message));
        Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        return;
      }
    }

    public void UpdateChildren()
    {
      try
      {
        var getStream = Client.StreamGetAsync(StreamId, null).Result;
        Client.Stream = getStream.Resource;

        Context.NotifySpeckleFrame("client-children", StreamId, Client.Stream.ToJson());
      }
      catch (Exception err)
      {
        Context.NotifySpeckleFrame("client-error", Client.Stream.StreamId, SNJ.JsonConvert.SerializeObject(err.Message));
        Context.NotifySpeckleFrame("client-done-loading", StreamId, "");
        return;
      }
    }
    #endregion

    #region display & helpers
    public void DisplayContents()
    {
      Display.Geometry = new List<GeometryBase>();
      Display.Colors = new List<System.Drawing.Color>();
      Display.VisibleList = new List<bool>();

      var localCopy = Client.Stream.Objects.ToList();

      // BHoM code.
      var bhomObjectsWrapped = localCopy.Where(obj => obj.Properties.ContainsKey("BHoM")).OfType<SpeckleObject>().ToList();
      if (bhomObjectsWrapped.Count != 0)
      {
        List<IBHoMObject> bhomObjects;
        List<IObject> iObjects;
        List<object> reminder;

        BH.Engine.Speckle.Convert.ToBHoM(bhomObjectsWrapped, out bhomObjects, out iObjects, out reminder);

        // Do stuff with these.

      }

      int count = 0;
      foreach (SpeckleObject myObject in localCopy)
      {
        var gb = Converter.Deserialise(myObject);

        if (gb is BH.oM.Base.IBHoMObject)


          Display.Colors.Add(GetColorFromLayer(GetLayerFromIndex(count)));

        Display.VisibleList.Add(true);

        if (gb is Rhino.Geometry.Box)
#if WINR5
          gb = ( ( Rhino.Geometry.Box ) gb ).ToBrep();
#else
          gb = ((Rhino.Geometry.Box)gb).ToExtrusion();
#endif

        if (gb is GeometryBase)
        {
          Display.Geometry.Add(gb as GeometryBase);
        }
        else
        {
          Display.Geometry.Add(null);
        }

        count++;
      }

      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    public SpeckleCore.Layer GetLayerFromIndex(int index)
    {
      return Client.Stream.Layers.FirstOrDefault(layer => ((index >= layer.StartIndex) && (index < layer.StartIndex + layer.ObjectCount)));
    }

    public System.Drawing.Color GetColorFromLayer(SpeckleCore.Layer layer)
    {
      System.Drawing.Color layerColor = System.Drawing.ColorTranslator.FromHtml("#AEECFD");
      try
      {
        if (layer != null && layer.Properties != null)
          layerColor = System.Drawing.ColorTranslator.FromHtml(layer.Properties.Color.Hex);
      }
      catch
      {
        Debug.WriteLine("Layer '{0}' had no assigned color", layer.Name);
      }
      return layerColor;
    }

    public string GetClientId()
    {
      return Client.ClientId;
    }

    public ClientRole GetRole()
    {
      return ClientRole.Receiver;
    }
    #endregion

    #region Bake

    public void Bake()
    {
      string parent = String.Format("{0} | {1}", Client.Stream.Name, Client.Stream.StreamId);

      var parentId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent, -1);


      if (parentId == -1)
      {

        //There is no layer in the document with the Stream Name | Stream Id as a name, so create one

        var parentLayer = new Rhino.DocObjects.Layer()
        {
          Color = System.Drawing.Color.Black,
          Name = parent
        };

        //Maybe could be useful in the future
        parentLayer.SetUserString("spk", Client.Stream.StreamId);

        parentId = Rhino.RhinoDoc.ActiveDoc.Layers.Add(parentLayer);
      }
      else

        //Layer with this name does exist. 
        //This is either a coincidence or a receiver has affected this file before.
        //In any case, delete any sublayers and any objects within them.

        foreach (var layer in Rhino.RhinoDoc.ActiveDoc.Layers[parentId].GetChildren())
          Rhino.RhinoDoc.ActiveDoc.Layers.Purge(layer.LayerIndex, false);

      foreach (var spkLayer in Client.Stream.Layers)
      {
        var layerId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + spkLayer.Name, -1);


        //This is always going to be the case. 

        if (layerId == -1)
        {

          var index = -1;

          if (spkLayer.Name.Contains("::"))
          {
            var spkLayerPath = spkLayer.Name.Split(new string[] { "::" }, StringSplitOptions.None);

            var parentLayerId = Guid.Empty;

            foreach (var layerPath in spkLayerPath)
            {

              if (parentLayerId == Guid.Empty)
                parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[parentId].Id;

              var layer = new Rhino.DocObjects.Layer()
              {
                Name = layerPath,
                ParentLayerId = parentLayerId,
                Color = GetColorFromLayer(spkLayer),
                IsVisible = true
              };

              var parentLayerName = Rhino.RhinoDoc.ActiveDoc.Layers.First(l => l.Id == parentLayerId).FullPath;

              var layerExist = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parentLayerName + "::" + layer.Name, -1);


              if (layerExist == -1)
              {
                index = Rhino.RhinoDoc.ActiveDoc.Layers.Add(layer);
                parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[index].Id;
              }
              else
              {
                parentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[layerExist].Id;
              }

            }
          }
          else
          {

            var layer = new Rhino.DocObjects.Layer()
            {
              Name = spkLayer.Name,
              Id = Guid.Parse(spkLayer.Guid),
              ParentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[parentId].Id,
              Color = GetColorFromLayer(spkLayer),
              IsVisible = true
            };

            index = Rhino.RhinoDoc.ActiveDoc.Layers.Add(layer);
          }

          for (int i = (int)spkLayer.StartIndex; i < spkLayer.StartIndex + spkLayer.ObjectCount; i++)
          {
            if (Display.Geometry[i] != null && !Display.Geometry[i].IsDocumentControlled)
            {
              Rhino.RhinoDoc.ActiveDoc.Objects.Add(Display.Geometry[i], new ObjectAttributes() { LayerIndex = index });
            }
          }
        }
      }

      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    public void BakeLayer(string layerId)
    {
      SpeckleCore.Layer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);

      // create or get parent
      string parent = String.Format("{1} | {0}", Client.Stream.StreamId, Client.Stream.Name);

      var parentId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent, true);
      if (parentId == -1)
      {
        var parentLayer = new Rhino.DocObjects.Layer()
        {
          Color = System.Drawing.Color.Black,
          Name = parent
        };
        parentId = Rhino.RhinoDoc.ActiveDoc.Layers.Add(parentLayer);
      }
      else
      {
        int prev = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + myLayer.Name, true);
        if (prev != -1)
          Rhino.RhinoDoc.ActiveDoc.Layers.Purge(prev, true);
      }

      int theLayerId = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(parent + "::" + myLayer.Name, true);
      if (theLayerId == -1)
      {
        var layer = new Rhino.DocObjects.Layer()
        {
          Name = myLayer.Name,
          Id = Guid.Parse(myLayer.Guid),
          ParentLayerId = Rhino.RhinoDoc.ActiveDoc.Layers[parentId].Id,
          Color = GetColorFromLayer(myLayer),
          IsVisible = true
        };
        var index = Rhino.RhinoDoc.ActiveDoc.Layers.Add(layer);
        for (int i = (int)myLayer.StartIndex; i < myLayer.StartIndex + myLayer.ObjectCount; i++)
        {
          if (Display.Geometry[i] != null)
          {
            Rhino.RhinoDoc.ActiveDoc.Objects.Add(Display.Geometry[i], new ObjectAttributes() { LayerIndex = index });
          }
        }
      }
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    #endregion

    #region Toggles

    public void TogglePaused(bool status)
    {
      this.Paused = status;
    }

    public void ToggleVisibility(bool status)
    {
      this.Display.Enabled = status;
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    public void ToggleLayerHover(string layerId, bool status)
    {
      SpeckleCore.Layer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);
      if (myLayer == null) throw new Exception("Bloopers. Layer not found.");

      if (status)
      {
        Display.HoverRange = new Interval((double)myLayer.StartIndex, (double)(myLayer.StartIndex + myLayer.ObjectCount));
      }
      else
        Display.HoverRange = null;
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    public void ToggleLayerVisibility(string layerId, bool status)
    {
      SpeckleCore.Layer myLayer = Client.Stream.Layers.FirstOrDefault(l => l.Guid == layerId);
      if (myLayer == null) throw new Exception("Bloopers. Layer not found.");

      for (int i = (int)myLayer.StartIndex; i < myLayer.StartIndex + myLayer.ObjectCount; i++)
        Display.VisibleList[i] = status;
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }
    #endregion

    #region serialisation & end of life

    public void Dispose(bool delete = false)
    {
      Client.Dispose(delete);
      Display.Enabled = false;
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    public void Dispose()
    {
      Client.Dispose();
      Display.Enabled = false;
      Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
    }

    protected RhinoReceiver(SerializationInfo info, StreamingContext context)
    {
      Display = new SpeckleDisplayConduit();
      Display.Enabled = true;

      Objects = new List<SpeckleObject>();

      byte[] serialisedClient = Convert.FromBase64String((string)info.GetString("client"));

      using (var ms = new MemoryStream())
      {
        ms.Write(serialisedClient, 0, serialisedClient.Length);
        ms.Seek(0, SeekOrigin.Begin);
        Client = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);
        Client.ClientType = "Rhino";
        StreamId = Client.StreamId;
      }

      Client.OnReady += Client_OnReady;
      Client.OnLogData += Client_OnLogData;
      Client.OnWsMessage += Client_OnWsMessage;
      Client.OnError += Client_OnError;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      using (var ms = new MemoryStream())
      {
        var formatter = new BinaryFormatter();
        formatter.Serialize(ms, Client);
        info.AddValue("client", Convert.ToBase64String(ms.ToArray()));
        info.AddValue("paused", Paused);
        info.AddValue("visible", Visible);
      }
    }
    #endregion
  }
}

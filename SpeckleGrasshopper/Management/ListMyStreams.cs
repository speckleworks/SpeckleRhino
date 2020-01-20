using GH_IO.Serialization;
using Grasshopper.Kernel;
using SpeckleCore;
using SpeckleGrasshopper.Parameters;
using SpeckleGrasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeckleGrasshopper.Management
{
  public class ListStreams : GH_Component
  {
    private const string HistoryKey = "History";
    List<SpeckleStream> UserStreams = new List<SpeckleStream>();
    List<SpeckleStream> SharedStreams = new List<SpeckleStream>();
    SpeckleApiClient Client = new SpeckleApiClient();
    private bool IncludeHistory;

    Action ExpireComponent;

    public ListStreams() : base("Streams", "Streams", "Lists your existing Speckle streams for a specified account.", "Speckle", "Management")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();

    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Speckle account you want to retrieve your streams from.", GH_ParamAccess.item);

    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Param_SpeckleStreams(), "streams", "S", "Streams that you own or are shared with you.", GH_ParamAccess.list);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);

      ExpireComponent = () => this.ExpireSolution(true);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Account Account = null;
      DA.GetData(0, ref Account);

      if (Account == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't set the account");
        return;
      }

      DA.SetDataList(0, UserStreams.Select(x => new GH_SpeckleStream(x)));

      Client.BaseUrl = Account.RestApi; Client.AuthToken = Account.Token;
      Client.StreamsGetAllAsync("fields=streamId,name,description,parent,children,ancestors,tags,layers&isComputedResult=false&deleted=false").ContinueWith(tsk =>
       {
         if (tsk.Result.Success == false)
         {
           AddRuntimeMessage(GH_RuntimeMessageLevel.Error, tsk.Result.Message);
           return;
         }

         var streams = new List<SpeckleStream>();
         if (!IncludeHistory)
         {
           streams = tsk.Result.Resources.Where(x => x.Parent == null).ToList();
         }
         else
         {
           streams = tsk.Result.Resources.ToList();
         }

         var newStreams = tsk.Result.Resources.ToList();
         var notUpdated = UserStreams.Select(x => x._id).SequenceEqual(streams.Select(x => x._id));

         if (!notUpdated)
         {
           UserStreams = streams;
           Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
         }
       });

    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      var item0 = Menu_AppendItem(menu, "History", Menu_IncludeHistory, true, IncludeHistory);
      item0.ToolTipText = "Make this parameter an Item";
    }

    private void Menu_IncludeHistory(object sender, EventArgs e)
    {
      IncludeHistory = !IncludeHistory;
      ClearData();
      ExpireSolution(true);
    }

    public override bool Write(GH_IWriter writer)
    {
      var result = base.Write(writer);
      writer.SetBoolean(HistoryKey, IncludeHistory);
      return result;
    }

    public override bool Read(GH_IReader reader)
    {
      var result = base.Read(reader);
      bool value = false;
      if (reader.TryGetBoolean(HistoryKey, ref value))
      {
        IncludeHistory = value;
      }
      return result;
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.Streams;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("{12d9bb3a-6cc3-4fe2-95cb-c58b1415977b}"); }
    }
  }
}

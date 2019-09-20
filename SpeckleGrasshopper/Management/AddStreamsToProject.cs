using System;
using SpeckleCore;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper.Management
{
  public class AddStreamsToProject : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the AddStreamsToProject class.
    /// </summary>

    SpeckleApiClient Client = new SpeckleApiClient();
    Action ExpireComponent;
    List<SpeckleStream> AddedStreams = new List<SpeckleStream>();
    public AddStreamsToProject()
      : base("AddStreamsToProject", "Add Streams","Add a list of streams to a project","Speckle", "Management")
    {

    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      ExpireComponent = () => this.ExpireSolution(true);
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account with project write access", GH_ParamAccess.item);
      pManager.AddGenericParameter("Project", "P", "Project to add streams to", GH_ParamAccess.item);
      pManager.AddGenericParameter("Streams", "S", "Streams to add", GH_ParamAccess.list);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var streams = new List<SpeckleStream>();
      Project project = null;
      Account account = null;

      DA.GetData("Project", ref project);
      DA.GetDataList("Streams", streams);
      DA.GetData("Account", ref account);

      if (null == project) {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "You must connect an account to this component");
        return;
      }

      if (null == streams)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "You must connect at least one stream to this component");
        return;
      }

      if (null == project)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
          "You must connect a project to this component");
        return;
      }

      if (null != AddedStreams && AddedStreams.Count > 0)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Added " +
          AddedStreams.Select(stream => stream.StreamId).ToString() +
          "to the project");
      }

      //Do nothing if the project already contains these streams
      var streamsToAdd = streams.Where(stream => !project.Streams.Contains(stream.StreamId)).ToList();
      if (streamsToAdd.Count() < 1 )
      {
        AddedStreams.Clear();
        return;
      }

      project.Streams.AddRange(streamsToAdd.Select(stream => stream.StreamId));

      Client.BaseUrl = account.RestApi;
      Client.AuthToken = account.Token;

      Client.ProjectUpdateAsync(project._id, project).ContinueWith(task =>
      {
        if (task.Result.Success == false)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, task.Result.Message);
          return;
        }
        AddedStreams.Clear();
        AddedStreams.AddRange(streamsToAdd);
        Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
      });
      
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return Resources.GenericIconXS;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("6712f7f4-3faa-4d3a-b060-7385966dc5c7"); }
    }
  }
}

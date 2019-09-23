using System;
using System.Collections.Generic;
using SpeckleCore;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using SpeckleGrasshopper.Properties;
using System.Windows.Forms;

namespace SpeckleGrasshopper.Management
{
  //based on dimitrie's ListStreams component
  public class ListMyProjects : GH_Component
  {
    List<Project> UserProjects = new List<Project>();
    SpeckleApiClient Client = new SpeckleApiClient();
    Project SelectedProject = null;
    Action ExpireComponent;

    public ListMyProjects()
      : base("Projects", "Projects", "Lists projects you own or have access to", "Speckle", "Management")
    {
      SpeckleInitializer.Initialize();
      LocalContext.Init();
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      foreach (Project project in UserProjects)
      {
        var nameString = "";
        if (project.number != null) { nameString += project.number + " - "; }
        nameString += project.Name;
        Menu_AppendItem(menu, nameString, (sender, e) => 
        {
          if (SelectedProject == project)
          {
            SelectedProject = null;
          }
          else { SelectedProject = project; }
          Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
        },true, project == SelectedProject );
      }
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Speckle account you want to retrieve your streams from.", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Projects", "Ps", "Projects you own or have write access to", GH_ParamAccess.list);
      pManager.AddGenericParameter("Selected Project", "P", "Double click this component to select one project", GH_ParamAccess.item);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      ExpireComponent = () => this.ExpireSolution(true);
    }
    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Account Account = null;
      DA.GetData("Account", ref Account);


      if (Account == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not set the account");
        return;
      }

      if (SelectedProject == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Right click this component to select a project");
      }

   
      DA.SetData("Selected Project", SelectedProject);
      DA.SetDataList("Projects", UserProjects);

      Client.BaseUrl = Account.RestApi; Client.AuthToken = Account.Token;
      Client.ProjectGetAllAsync().ContinueWith(tsk =>
      {
        if (tsk.Result.Success == false)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, tsk.Result.Message);
          return;
        }
        var newProjects = tsk.Result.Resources.ToList();
        var notUpdated = UserProjects.Select(x => x._id).SequenceEqual(newProjects.Select(x => x._id));

        if (!notUpdated){
          UserProjects = tsk.Result.Resources.ToList();
          Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
        }
      });
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {

        return Resources.Projects;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("d82e4c2c-dd81-4ff7-bdfd-d15163dc64f7"); }
    }
  }
}

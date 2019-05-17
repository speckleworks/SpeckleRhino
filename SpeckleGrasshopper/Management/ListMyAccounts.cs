extern alias SpeckleNewtonsoft;
using SNJ = SpeckleNewtonsoft.Newtonsoft.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SpeckleCore;
using SpeckleGrasshopper.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_IO.Serialization;
//using Newtonsoft.Json;

namespace SpeckleGrasshopper.Management
{
  public class ListMyAccounts : GH_Component
  {
    List<Account> Accounts = new List<Account>();
    Account Selected;
    Action ExpireComponent;

    public ListMyAccounts( ) : base( "Accounts", "Accounts", "Lists your existing Speckle accounts.", "Speckle", "Management" )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }


    public override bool Read( GH_IReader reader )
    {
      string serialisedAccount = "";
      reader.TryGetString( "selectedAccount", ref serialisedAccount );

      if ( serialisedAccount != "" )
      {
        Account myAccount = SNJ.JsonConvert.DeserializeObject<Account>( serialisedAccount );
        Selected = myAccount;
      }

      return base.Read( reader );
    }

    public override bool Write( GH_IWriter writer )
    {
      writer.SetString( "selectedAccount", SNJ.JsonConvert.SerializeObject( Selected ) );
      return base.Write( writer );
    }

    protected override void RegisterInputParams( GH_InputParamManager pManager )
    {
    }

    protected override void RegisterOutputParams( GH_OutputParamManager pManager )
    {
      pManager.Register_GenericParam( "account", "A", "Selected account." );
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );

      ExpireComponent = ( ) => this.ExpireSolution( true );

      Accounts = LocalContext.GetAllAccounts();
    }
    public override void AppendAdditionalMenuItems( ToolStripDropDown menu )
    {
      base.AppendAdditionalMenuItems( menu );
      int count = 0;

      foreach ( var account in Accounts )
      {
        Menu_AppendItem( menu, count++ + ". " + account.ServerName, ( sender, e ) =>
         {
           Selected = account;
           this.NickName = account.ServerName;
           Rhino.RhinoApp.MainApplicationWindow.Invoke( ExpireComponent );
         }, true );
      }
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      if ( Selected == null )
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "Right click the component and select an account." );
        return;
      }

      AddRuntimeMessage( GH_RuntimeMessageLevel.Remark, Selected.ServerName );

      DA.SetData( 0, Selected );
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.GenericIconXS;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid( "{958de333-1ad0-4989-acbe-f59329d5b569}" ); }
    }
  }
}

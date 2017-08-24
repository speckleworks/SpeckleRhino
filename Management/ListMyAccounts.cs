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

namespace SpeckleGrasshopper.Management
{
    public class ListMyAccounts : GH_Component
    {
        List<SpeckleAccount> Accounts = new List<SpeckleAccount>();
        SpeckleAccount Selected;
        Action ExpireComponent;

        public ListMyAccounts() : base("Accounts", "Accounts", "Lists your existing Speckle accounts.", "Speckle", "Management") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("account", "A", "Selected account.");
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            ExpireComponent = () => this.ExpireSolution(true);

            string strPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            strPath = strPath + @"\SpeckleSettings";

            if (Directory.Exists(strPath) && Directory.EnumerateFiles(strPath, "*.txt").Count() > 0)
                foreach (string file in Directory.EnumerateFiles(strPath, "*.txt"))
                {
                    string content = File.ReadAllText(file);
                    string[] pieces = content.TrimEnd('\r', '\n').Split(',');

                    Accounts.Add(new SpeckleAccount() { email = pieces[0], apiToken = pieces[1], serverName = pieces[2], restApi = pieces[3], rootUrl = pieces[4] });
                }

            if (Accounts.Count == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No accounts present.");
                return;
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            int count = 0;

            foreach (var account in Accounts)
            {
                Menu_AppendItem(menu, count++ + ". " + account.serverName, (sender, e) =>
                {
                    Selected = account;
                    this.NickName = account.serverName;
                    Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
                }, true);
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (Selected == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Right click the component and select an account.");
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, Selected.serverName);

            DA.SetData(0, Selected);
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
            get { return new Guid("{958de333-1ad0-4989-acbe-f59329d5b569}"); }
        }
    }

    public class SpeckleAccount
    {
        public string email { get; set; }
        public string apiToken { get; set; }
        public string serverName { get; set; }
        public string restApi { get; set; }
        public string rootUrl { get; set; }

        public SpeckleAccount() { }
    }
}

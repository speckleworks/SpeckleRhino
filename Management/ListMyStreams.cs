using Grasshopper.Kernel;
using SpeckleCore;
using SpeckleGrasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleGrasshopper.Management
{
    public class ListStreams : GH_Component
    {
        List<DataStream> OwnedStreams = new List<DataStream>();
        List<DataStream> SharedStreams = new List<DataStream>();
        SpeckleApiClient Client = new SpeckleApiClient();

        SpeckleAccount OldAccount;

        Action ExpireComponent;

        public ListStreams() : base("Streams", "Streams", "Lists your existing Speckle streams for a specified account.", "Speckle", "Management") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Account", "A", "Speckle account you want to retrieve your streams from.", GH_ParamAccess.item);
            
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("owned streams", "OS", "Streams that you own (created).");
            pManager.Register_GenericParam("shared streams", "SS", "Streams that have been shared with you");
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);

            ExpireComponent = () => this.ExpireSolution(true);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object preAccount = null;
            SpeckleAccount Account = null;
            DA.GetData(0, ref preAccount);

            Account = ((SpeckleAccount)preAccount.GetType().GetProperty("Value").GetValue(preAccount, null));

            if (Account == null)
                return;

            DA.SetDataList(0, OwnedStreams);
            DA.SetDataList(1, SharedStreams);

            if (Account == OldAccount)
                return;

            OldAccount = Account;

            Client.BaseUrl = Account.restApi; Client.AuthToken = Account.apiToken;
            Client.UserStreamsGetAsync().ContinueWith(tsk =>
            {
                OwnedStreams = tsk.Result.OwnedStreams.ToList();
                SharedStreams = tsk.Result.SharedWithStreams.ToList();
                Rhino.RhinoApp.MainApplicationWindow.Invoke(ExpireComponent);
            });
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
            get { return new Guid("{12d9bb3a-6cc3-4fe2-95cb-c58b1415977b}"); }
        }
    }
}

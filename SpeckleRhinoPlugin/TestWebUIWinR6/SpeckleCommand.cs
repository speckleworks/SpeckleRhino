using Rhino;
using Rhino.Commands;
using System.IO;
using System.Reflection;

namespace SpeckleRhino
{
    public class TestWebUICommand : Command
    {

        public string PathResources { get; set; }
        public string IndexPath { get; set; }

        public TestWebUICommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static TestWebUICommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "TestWebUICommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} command is under construction.", EnglishName);

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string assemblyPath = Path.GetDirectoryName(assemblyLocation);
            PathResources = Path.Combine(assemblyPath, "app");
            IndexPath = Path.Combine(PathResources, "index.html");

#if ETO

            var form = new EtoForm();
            form.Topmost = true;

#elif WINR5
            var form = new WinForm();
            form.TopMost = true;
#endif

            form.ShowInTaskbar = true;
            form.BringToFront();
            form.SetWVUrl(IndexPath);
            form.Show();
            return Result.Success;
        }
    }
}

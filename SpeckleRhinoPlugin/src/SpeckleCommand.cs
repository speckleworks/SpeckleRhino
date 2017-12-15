using Rhino;
using Rhino.Commands;
using System.IO;
using System.Reflection;

namespace SpeckleRhino
{
    public class SpeckleCommand : Command
    {
        private WinForm TheForm;

        public string PathResources { get; set; }
        public string IndexPath { get; set; }

        public bool Init { get; set; } = false;

        public SpeckleCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SpeckleCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "Speckle"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("The {0} command is under construction.", EnglishName);

            if (!Init)
            {
                TheForm = new WinForm();
                TheForm.TopMost = true;
                TheForm.AllowDrop = true;
                TheForm.ShowInTaskbar = true;
                TheForm.BringToFront();
                TheForm.Show();
                Init = true;
                return Result.Success;
            }
            else
            {
                TheForm.Show();
                return Result.Success;
            }
        }
    }
}

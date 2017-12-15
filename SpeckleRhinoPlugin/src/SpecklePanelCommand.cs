using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;
using Rhino.UI;

namespace SpeckleRhino
{
    [System.Runtime.InteropServices.Guid("8C8930B3-637C-4DE0-8D42-5B109171B94D")]
    public class SpecklePanelCommand : Command
    {

        public SpecklePanelCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SpecklePanelCommand Instance
        {
            get; private set;
        }

        public override string EnglishName
        {
            get { return "SpecklePanel"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var panel_id = SpeckleRhinoUserControl.PanelId;
            var visible = Panels.IsPanelVisible(panel_id);

            var prompt = visible
              ? "Speckle panel is visible."
              : "Speckle panel is hidden.";

            var go = new GetOption();
            go.SetCommandPrompt(prompt);
            var hide_index = go.AddOption("Hide");
            var show_index = go.AddOption("Show");
            var toggle_index = go.AddOption("Toggle");

            go.Get();
            if (go.CommandResult() != Result.Success)
                return go.CommandResult();

            var option = go.Option();
            if (null == option)
                return Result.Failure;

            var index = option.Index;

            if (index == hide_index)
            {
                if (visible)
                    Panels.ClosePanel(panel_id);
            }
            else if (index == show_index)
            {
                if (!visible)
                    Panels.OpenPanel(panel_id);
            }
            else if (index == toggle_index)
            {
                if (visible)
                    Panels.ClosePanel(panel_id);
                else
                    Panels.OpenPanel(panel_id);
            }

            return Result.Success;
        }
    }
}

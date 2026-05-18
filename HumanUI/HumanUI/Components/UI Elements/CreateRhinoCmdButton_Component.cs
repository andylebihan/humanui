using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Special button that runs a Rhino command-line script when clicked. Inherits
    /// the Name/Image inputs from CreateButton_Component and adds a Rhino Script
    /// input; the Click handler differs from the plain button.
    /// </summary>
    public class CreateRhinoCmdButton_Component : CreateButton_Component
    {
        private string commandString = "";

        public CreateRhinoCmdButton_Component()
            : base("Create Rhino Command Button", "CmdButton",
                "Create a Special Button object to trigger a Rhino command.")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Button Name", "N", "The text to display on the button", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager.AddTextParameter("Image Path", "I", "The image to display on the button.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("Rhino Script", "S", "The command line script to execute on button press", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Button";
            string imagePath = "";
            string localCmd = "";
            if (DA.GetData("Rhino Script", ref localCmd))
            {
                commandString = localCmd;
            }

            bool hasText = DA.GetData("Button Name", ref name);
            bool hasIcon = DA.GetData("Image Path", ref imagePath);
            if (!hasText && !hasIcon) return;

            var btn = new Button();
            SetupButton(name, imagePath, hasText, hasIcon, btn, bs);
            btn.Click += ExecuteCommand;
            DA.SetData("Button", new UIElement_Goo(btn, name, InstanceGuid, DA.Iteration));
        }

        private void ExecuteCommand(object sender, EventArgs e)
        {
            Rhino.RhinoApp.RunScript(commandString, false);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateRhinoButton;

        public override Guid ComponentGuid => new Guid("{58AEE14D-8214-4E76-8D51-3432CD30B3AC}");
    }
}

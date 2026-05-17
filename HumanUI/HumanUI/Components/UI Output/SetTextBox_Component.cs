using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    public class SetTextBox_Component : GH_Component
    {
        public SetTextBox_Component()
            : base("Set TextBox Contents", "SetTextBox",
                "Modify the contents of an existing Text Box object.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Box to modify", "TB", "The text box object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New Text Box contents", "C", "The new text to display in the text box", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object textBoxObject = null;
            string newContents = "";
            if (!DA.GetData("New Text Box contents", ref newContents)) return;
            if (!DA.GetData("Text Box to modify", ref textBoxObject)) return;
            // HUI textboxes are stack layouts wrapping (optional Label) + TextBox + (optional Button).
            // Walk inside to find the TextBox.
            var container = HUI_Util.GetUIElement<Control>(textBoxObject);
            var tb = HUI_Util.findTextBox(container);
            if (tb != null) tb.Text = newContents;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetTextBox;

        public override Guid ComponentGuid => new Guid("{59b523a4-d32c-4a20-883c-a9cb828bf880}");
    }
}

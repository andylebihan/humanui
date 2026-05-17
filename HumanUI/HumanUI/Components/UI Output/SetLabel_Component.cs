using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    public class SetLabel_Component : GH_Component
    {
        public SetLabel_Component()
            : base("Set Label Contents", "SetLabel",
                "Modify the contents of an existing label object.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Label to modify", "L", "The label object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New Label contents", "C", "The new text to display in the label", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object labelObject = null;
            string newLabelContents = "";
            if (!DA.GetData("New Label contents", ref newLabelContents)) return;
            if (!DA.GetData("Label to modify", ref labelObject)) return;
            var label = HUI_Util.GetUIElement<Label>(labelObject);
            if (label != null)
            {
                label.Text = newLabelContents;
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetLabel;

        public override Guid ComponentGuid => new Guid("{07b9d48c-bfc5-4f49-a449-50ffe4e6d4c7}");
    }
}

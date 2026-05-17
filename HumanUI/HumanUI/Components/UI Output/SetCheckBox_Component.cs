using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    public class SetCheckBox_Component : GH_Component
    {
        public SetCheckBox_Component()
            : base("Set CheckBox", "SetCheckBox",
                "Modify an existing Check Box object.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Check Box to modify", "CB", "The check box object to modify", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("New Check Box Label", "L", "The new label to display next to the check box", GH_ParamAccess.item)].Optional = true;
            pManager[pManager.AddBooleanParameter("New Value", "V", "The new value (checked/unchecked) for the box.", GH_ParamAccess.item)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object checkBoxObject = null;
            bool isSelected = false;
            string newLabel = "";
            bool setLabel = DA.GetData("New Check Box Label", ref newLabel);
            if (!DA.GetData("Check Box to modify", ref checkBoxObject)) return;
            bool setChecked = DA.GetData("New Value", ref isSelected);

            var cb = HUI_Util.GetUIElement<CheckBox>(checkBoxObject);
            if (cb != null)
            {
                if (setLabel) cb.Text = newLabel;
                if (setChecked) cb.Checked = isSelected;
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetCheckbox;

        public override Guid ComponentGuid => new Guid("{59b523a4-d32c-4a21-883c-a9cb828bf880}");
    }
}

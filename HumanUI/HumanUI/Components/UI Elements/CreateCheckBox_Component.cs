using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    public class CreateCheckBox_Component : GH_Component
    {
        public CreateCheckBox_Component()
            : base("Create Checkbox", "Checkbox",
                "Creates a single checkbox",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Label", "L", "The label for the checkbox.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Starting Value", "V", "The starting value (checked/unchecked) for the box.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Checkbox", "CB", "The created checkbox.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool isSelected = false;
            string label = "";
            if (!DA.GetData("Label", ref label)) return;
            DA.GetData("Starting Value", ref isSelected);

            var cb = new CheckBox
            {
                Text = label,
                Checked = isSelected,
            };

            DA.SetData("Checkbox", new UIElement_Goo(cb, $"Checkbox: {label}", InstanceGuid, DA.Iteration));
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateCheckbox;

        public override Guid ComponentGuid => new Guid("{c2c5cc88-9812-4769-b482-bd6f32697836}");
    }
}

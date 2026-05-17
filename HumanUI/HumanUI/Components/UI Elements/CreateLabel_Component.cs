using System;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components.UI_Elements
{
    public class CreateLabel_Component : GH_Component
    {
        public CreateLabel_Component()
            : base("Create Label", "Label",
                "Creates a label in the window.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Label Text", "T", "The text to display in the label", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Label Size", "S", "The size of the label to display", GH_ParamAccess.item, 12);
            var labelSize = (Param_Integer)pManager[1];
            labelSize.AddNamedValue("Micro", 8);
            labelSize.AddNamedValue("Normal", 12);
            labelSize.AddNamedValue("Heading", 18);
            labelSize.AddNamedValue("Major Heading", 24);

            pManager.AddIntegerParameter("Justification", "J", "Text justification", GH_ParamAccess.item, 0);
            var justification = (Param_Integer)pManager[2];
            justification.AddNamedValue("Left", 0);
            justification.AddNamedValue("Center", 1);
            justification.AddNamedValue("Right", 2);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Label", "L", "The created labels.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string labelContent = "";
            int labelSize = 12;
            int justification = 0;
            if (!DA.GetData("Label Text", ref labelContent)) return;
            DA.GetData("Label Size", ref labelSize);
            DA.GetData("Justification", ref justification);

            var label = new Label
            {
                Text = labelContent,
                Font = new Eto.Drawing.Font(Eto.Drawing.SystemFonts.Default().FamilyName, labelSize),
                TextAlignment = MapAlignment(justification),
            };

            DA.SetData("Label", new UIElement_Goo(label, $"Label: {labelContent}", InstanceGuid, DA.Iteration));
        }

        private static TextAlignment MapAlignment(int justification) => justification switch
        {
            1 => TextAlignment.Center,
            2 => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateLabel;

        public override Guid ComponentGuid => new Guid("{b844ab20-b7ae-4a21-99d5-83c5666a7432}");
    }
}

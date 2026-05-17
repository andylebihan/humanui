using System;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Multi-line text block. Eto.Forms doesn't have a separate TextBlock type; we use
    /// Label with Wrap=Word so it wraps to the available width.
    /// </summary>
    public class CreateTextBlock_Component : GH_Component
    {
        public CreateTextBlock_Component()
            : base("Create Text Block", "TB",
                "Creates a multi-line text block",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "The text to display in the text block", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Text Size", "S", "The size of the label to display", GH_ParamAccess.item, 12);
            var labelSize = (Param_Integer)pManager[1];
            labelSize.AddNamedValue("Micro", 8);
            labelSize.AddNamedValue("Normal", 12);
            labelSize.AddNamedValue("Medium", 16);
            labelSize.AddNamedValue("Large", 18);
            pManager.AddIntegerParameter("Justification", "J", "Text justification", GH_ParamAccess.item, 0);
            var justification = (Param_Integer)pManager[2];
            justification.AddNamedValue("Left", 0);
            justification.AddNamedValue("Center", 1);
            justification.AddNamedValue("Right", 2);
            justification.AddNamedValue("Justify", 3);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Block", "TB", "The created text block.", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string labelContent = "";
            int labelSize = 12;
            int justification = 0;
            if (!DA.GetData("Text", ref labelContent)) return;
            DA.GetData("Text Size", ref labelSize);
            DA.GetData("Justification", ref justification);

            var label = new Label
            {
                Text = labelContent,
                Font = new Eto.Drawing.Font(Eto.Drawing.SystemFonts.Default().FamilyName, labelSize),
                Wrap = WrapMode.Word,
                TextAlignment = MapAlignment(justification),
            };

            DA.SetData("Text Block", new UIElement_Goo(label, $"Text Block: {labelContent}", InstanceGuid, DA.Iteration));
        }

        // Eto.Forms.Label has no "Justify" alignment; fall back to Left for unsupported values.
        private static TextAlignment MapAlignment(int justification) => justification switch
        {
            1 => TextAlignment.Center,
            2 => TextAlignment.Right,
            _ => TextAlignment.Left,
        };

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateTextBlock;

        public override Guid ComponentGuid => new Guid("{088f694c-6b70-4baf-afe4-5bfd46526d6f}");
    }
}

using System;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// "True-only" button: same visuals as a normal button, but type-tagged so
    /// ValueListener (and Set/Get components) can distinguish it. Used by GH
    /// scripts that need press-edge semantics rather than the default toggle.
    /// </summary>
    public class CreateTrueOnlyButton_Component : CreateButton_Component
    {
        public CreateTrueOnlyButton_Component()
            : base("Create True-Only Button", "True Button",
                "Create a True only Button object.")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Button";
            string imagePath = "";
            bool hasText = DA.GetData("Button Name", ref name);
            bool hasIcon = DA.GetData("Image Path", ref imagePath);
            if (!hasText && !hasIcon) return;

            var btn = new TrueOnlyButton();
            SetupButton(name, imagePath, hasText, hasIcon, btn, bs);
            DA.SetData("Button", new UIElement_Goo(btn, name, InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateTrueButton;

        public override Guid ComponentGuid => new Guid("{5AB78609-C132-45C7-BA44-200A4F2E4188}");
    }
}

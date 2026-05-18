using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Show/hide and enable/disable a UI element. Eto exposes only Visible (bool)
    /// rather than WPF's three-state Visibility.{Visible,Hidden,Collapsed}; the
    /// Collapse input is kept for backward compatibility but only the boolean
    /// show/hide is honored.
    /// </summary>
    public class HideShowElement_Component : GH_Component
    {
        public HideShowElement_Component()
            : base("Hide/Show Element", "HideShow",
                "Allows you to hide or show an element ",
                "Human UI", "UI Main")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element to Hide/Show", "E", "The elements to hide/show", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Show", "S", "Set to true to show the element, false to hide", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Collapse", "C", "If true, no space is reserved for the element - if false, the space remains but the element hides", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Disable", "D", "If true, item will appear \"greyed out\" and a user will be unable to interact with it.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool show = true;
            bool collapse = true;
            object elem = null;
            bool disable = false;

            if (!DA.GetData("Show", ref show)) return;
            DA.GetData("Collapse", ref collapse);
            bool hasDisable = DA.GetData("Disable", ref disable);
            if (!DA.GetData("Element to Hide/Show", ref elem)) return;

            var ctrl = HUI_Util.GetUIElement<Control>(elem);
            if (ctrl == null) return;

            if (hasDisable) ctrl.Enabled = !disable;
            ctrl.Visible = show;
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.HideShowElement;

        public override Guid ComponentGuid => new Guid("{a3c49442-c136-4553-9a0d-637d5fbf27d4}");
    }
}

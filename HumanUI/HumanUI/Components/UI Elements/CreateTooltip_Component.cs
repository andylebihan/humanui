using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI
{
    /// <summary>
    /// Attach a tooltip to an existing UI element. WPF supported arbitrary control
    /// content inside tooltips and configurable show/hide timing via ToolTipService;
    /// Eto's Control.ToolTip is a plain string, so the port converts the content
    /// to text and silently accepts the timing inputs for backward compatibility.
    /// </summary>
    public class CreateTooltip_Component : GH_Component
    {
        public CreateTooltip_Component()
            : base("Attach Tooltip to Element", "Tooltip",
                "Attach a tooltip to a UI element",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements to Attach to", "E", "The elements to attach the tooltip to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Tooltip Content", "C", "The content (text or elements) to put in the tooltip", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Show Delay", "SD", "How long in ms before the tooltip displays", GH_ParamAccess.item, 500);
            pManager.AddIntegerParameter("Duration", "D", "How long in ms that the tooltip should show", GH_ParamAccess.item, 2000);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object elem = null;
            object contentElem = null;
            int showDelay = 500;
            int duration = 2000;
            if (!DA.GetData("Elements to Attach to", ref elem)) return;
            if (!DA.GetData("Tooltip Content", ref contentElem)) return;
            DA.GetData("Show Delay", ref showDelay);
            DA.GetData("Duration", ref duration);

            var ctrl = HUI_Util.GetUIElement<Control>(elem);
            if (ctrl == null) return;

            ctrl.ToolTip = ExtractTooltipText(contentElem);
        }

        private static string ExtractTooltipText(object content)
        {
            switch (content)
            {
                case null: return string.Empty;
                case string s: return s;
                case UIElement_Goo goo when goo.element is Label l: return l.Text ?? string.Empty;
                case UIElement_Goo goo when goo.element is TextControl tc: return tc.Text ?? string.Empty;
                case UIElement_Goo goo: return goo.name ?? string.Empty;
                case Control c when c is Label l: return l.Text ?? string.Empty;
                case Control c when c is TextControl tc: return tc.Text ?? string.Empty;
                default: return content.ToString();
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateTooltip;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{a6e4cefc-ca10-4dc4-9120-696e02e5cfeb}");
    }
}

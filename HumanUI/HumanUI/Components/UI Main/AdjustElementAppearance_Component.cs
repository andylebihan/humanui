using System;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Apply Foreground / Background / FontSize to a HUI element. Walks
    /// the element's logical children for containers so a "color the whole
    /// stack" call hits every Eto.TextControl underneath. The WPF version's
    /// ChartBase / WPF Expander-header coupling is dropped — chart fills
    /// are managed by SetChartAppearance now, and Expander headers are
    /// plain strings on the Eto side.
    /// </summary>
    public class AdjustElementAppearance_Component : GH_Component
    {
        public AdjustElementAppearance_Component()
            : base("Adjust Element Appearance", "AdjustElem",
                "Adjust the color and appearance of individual elements.",
                "Human UI", "UI Main")
        { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements to Adjust", "E", "The elements to adjust", GH_ParamAccess.item);
            pManager.AddColourParameter("Foreground", "FC", "The foreground color of the element", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddColourParameter("Background", "BC", "The background color of the element", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddNumberParameter("Font Size", "S", "The font size of the element", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object elem = null;
            System.Drawing.Color? fgCol = null;
            System.Drawing.Color? bgCol = null;
            double fontSize = -1;

            if (!DA.GetData("Elements to Adjust", ref elem)) return;
            DA.GetData("Foreground", ref fgCol);
            DA.GetData("Background", ref bgCol);
            DA.GetData("Font Size", ref fontSize);

            var control = HUI_Util.GetUIElement<Control>(elem);
            if (control == null) return;

            ApplyToTree(control, fgCol, bgCol, fontSize);
        }

        private static void ApplyToTree(Control c, System.Drawing.Color? fg, System.Drawing.Color? bg, double fontSize)
        {
            ApplyToOne(c, fg, bg, fontSize);
            // Recurse into containers so a parent target paints all children.
            // The Eto container surface is the same StackLayout / TableLayout /
            // Scrollable tree HUI_Util.findSlider walks for sliders.
            if (c is Container container)
            {
                foreach (var child in container.Controls.ToList())
                    ApplyToTree(child, fg, bg, fontSize);
            }
        }

        private static void ApplyToOne(Control c, System.Drawing.Color? fg, System.Drawing.Color? bg, double fontSize)
        {
            if (c == null) return;
            var fgColor = fg.HasValue ? Color.FromArgb(fg.Value.R, fg.Value.G, fg.Value.B, fg.Value.A) : (Color?)null;
            var bgColor = bg.HasValue ? Color.FromArgb(bg.Value.R, bg.Value.G, bg.Value.B, bg.Value.A) : (Color?)null;

            switch (c)
            {
                // TextControl is the Eto base for Label / TextBox / TextArea /
                // RichTextArea / TextStepper — TextColor / BackgroundColor /
                // Font all live on the common base, so the one case handles
                // every text-bearing control.
                case TextControl tc:
                    if (fgColor.HasValue) tc.TextColor = fgColor.Value;
                    if (bgColor.HasValue) tc.BackgroundColor = bgColor.Value;
                    if (fontSize > 0 && tc.Font != null)
                        tc.Font = new Font(tc.Font.Family, (float)fontSize, tc.Font.FontStyle);
                    break;
                case Panel panel:
                    if (bgColor.HasValue) panel.BackgroundColor = bgColor.Value;
                    break;
                default:
                    // Generic fallback for any Eto.Forms.CommonControl that
                    // exposes BackgroundColor. Reflection sidesteps the type
                    // explosion (DropDown, ListBox, CheckBox, RadioButton,
                    // ColorPicker, GridView, ...).
                    var bgProp = c.GetType().GetProperty("BackgroundColor");
                    if (bgColor.HasValue && bgProp != null && bgProp.CanWrite)
                        bgProp.SetValue(c, bgColor.Value);
                    break;
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.AdjustElementAppearance;
        public override Guid ComponentGuid => new Guid("76eb5930-7b2b-4a11-839e-d3c00990af8b");
    }
}

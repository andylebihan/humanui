using System;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using SysColor = System.Drawing.Color;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Wraps content in a Drawable that paints a coloured border around the child. Eto
    /// doesn't have a built-in WPF-equivalent Border control; we render the outline via
    /// the Drawable's Paint event and host the actual content in a Panel layered on top.
    /// </summary>
    public class CreateBorder_Component : GH_Component
    {
        public CreateBorder_Component()
            : base("Create Border", "Border",
                "Wrap Elements with a border.",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Element", "E", "The UI element to include", GH_ParamAccess.item);
            pManager.AddNumberParameter("Border Thickness", "T", "Border thickness", GH_ParamAccess.item, 5.0);
            pManager.AddColourParameter("Border Color", "C", "Border color", GH_ParamAccess.item, SysColor.Black);
            pManager.AddNumberParameter("Corner Radius", "R", "Border corner radius", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Border", "B", "The Border", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            UIElement_Goo elementToAdd = null;
            double thickness = 5;
            SysColor color = SysColor.Black;
            double radius = 0;

            if (!DA.GetData("UI Element", ref elementToAdd)) return;
            if (!DA.GetData("Border Thickness", ref thickness)) return;
            if (!DA.GetData("Border Color", ref color)) return;
            if (!DA.GetData("Corner Radius", ref radius)) return;
            if (elementToAdd?.element == null) return;

            HUI_Util.removeParent(elementToAdd.element);

            // PixelLayout lets us paint a border around the child without consuming layout
            // space ourselves; the child sits at (thickness, thickness) so it doesn't
            // collide with the painted outline.
            var stroke = (float)thickness;
            var corner = (float)radius;
            var etoColor = Color.FromArgb(color.R, color.G, color.B, color.A);

            var panel = new Panel
            {
                Padding = new Padding((int)thickness),
                Content = elementToAdd.element,
            };

            var drawable = new Drawable { Content = panel };
            drawable.Paint += (s, e) =>
            {
                using var pen = new Pen(etoColor, stroke);
                var rect = new RectangleF(stroke / 2f, stroke / 2f,
                    drawable.Width - stroke, drawable.Height - stroke);
                if (corner > 0)
                {
                    var path = GraphicsPath.GetRoundRect(rect, corner);
                    e.Graphics.DrawPath(pen, path);
                }
                else
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            };

            DA.SetData("Border", new UIElement_Goo(drawable, "Border", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateBorder;

        public override Guid ComponentGuid => new Guid("DFB1703A-45FD-44E6-BE50-E2A4A3C415B4");
    }
}

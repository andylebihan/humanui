using System;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Thin Eto Drawable that paints a single horizontal or vertical line. WPF
    /// Shape primitives don't exist in Eto, so we draw it and let the parent
    /// stack stretch us to fill the available space.
    /// </summary>
    internal sealed class HUI_Separator : Drawable
    {
        public double Thickness { get; }
        public bool Horizontal { get; }
        public Color LineColor { get; }

        public HUI_Separator(double thickness, bool horizontal, Color color)
        {
            Thickness = thickness <= 0 ? 0.5 : thickness;
            Horizontal = horizontal;
            LineColor = color;
            Paint += OnPaint;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            using var brush = new SolidBrush(LineColor);
            var size = ClientSize;
            if (Horizontal)
            {
                var y = (float)(size.Height / 2.0 - Thickness / 2.0);
                e.Graphics.FillRectangle(brush, 0, y, size.Width, (float)Thickness);
            }
            else
            {
                var x = (float)(size.Width / 2.0 - Thickness / 2.0);
                e.Graphics.FillRectangle(brush, x, 0, (float)Thickness, size.Height);
            }
        }
    }

    public class CreateSeparator_Component : GH_Component
    {
        public CreateSeparator_Component()
            : base("Create Separator", "Separator",
                "Create a line separator object.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Thickness", "T", "The thickness of the separator.", GH_ParamAccess.item, 0.5);
            pManager.AddBooleanParameter("Horizontal", "H", "Separator is horizontal.", GH_ParamAccess.item, true);
            pManager.AddColourParameter("Color", "C", "The color of the separator (optional).", GH_ParamAccess.item, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Width", "W", "The width of the separator (optional). Default is Stretch.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The height of the separator (optional). Default is Stretch.", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Separator", "S", "The created Separator", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double thickness = 0.5;
            bool horizontal = true;
            var color = new GH_Colour();
            double width = 0;
            double height = 0;
            if (!DA.GetData("Thickness", ref thickness)) return;
            if (!DA.GetData("Horizontal", ref horizontal)) return;

            DA.GetData("Color", ref color);
            DA.GetData("Width", ref width);
            DA.GetData("Height", ref height);

            var src = color.Value;
            var etoColor = Color.FromArgb(src.R, src.G, src.B, src.A);
            var sep = new HUI_Separator(thickness, horizontal, etoColor);

            if (horizontal)
            {
                sep.Height = (int)Math.Max(1, height > 0 ? height : thickness);
                if (width > 0) sep.Width = (int)width;
            }
            else
            {
                sep.Width = (int)Math.Max(1, width > 0 ? width : thickness);
                if (height > 0) sep.Height = (int)height;
            }

            DA.SetData("Separator", new UIElement_Goo(sep, "Separator", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateSeparator;

        public override Guid ComponentGuid => new Guid("a7a0c814-ab68-45e0-9a9e-5515b0c4adc6");
    }
}

// Mac implementation of the Create Shape / Set Shape components.
// Windows still uses the WPF System.Windows.Shapes.Path port (see
// Components/UI Elements/CreateShape_Component.cs which is gated to the
// HUI_WINDOWS TFM); this file is only compiled into the Mac net7.0 TFM
// via the MacStubs/**/*.cs Compile Include in HumanUI.csproj.
//
// The rendering goes through Eto.Drawing.GraphicsPath so it's structurally
// cross-platform — if we ever decide to retire the WPF Shape path on
// Windows too, this same class can move into Custom Types/ and replace
// the WPF version with no API changes.

using System;
using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI
{
    /// <summary>
    /// Eto Drawable that renders a list of Rhino curves as a single 2D shape
    /// with optional fill and stroke. The path is rebuilt lazily whenever any
    /// of the inputs change. Curves are mirrored around the X axis (Rhino is
    /// +Y up, screen is +Y down) and scaled around the origin to match the
    /// original WPF Shape behavior.
    /// </summary>
    public sealed class HUI_Shape : Drawable
    {
        private List<Curve> _curves = new();
        private Color _fill = Colors.Transparent;
        private Color _stroke = Colors.Transparent;
        private float _strokeThickness;
        private double _scale = 1.0;

        private GraphicsPath _cachedPath;
        // True when the user explicitly sized the Drawable (Width/Height
        // properties were set from outside). When false, the path-build step
        // auto-sizes us to the curves' bounding box so something is visible
        // without forcing the user to wire W / H inputs.
        private bool _userSizedWidth;
        private bool _userSizedHeight;

        public HUI_Shape()
        {
            Paint += OnPaint;
        }

        /// <summary>Mark Width as user-controlled so subsequent path rebuilds don't auto-resize it.</summary>
        public void SetExplicitWidth(int w) { Width = w; _userSizedWidth = true; }
        /// <summary>Mark Height as user-controlled so subsequent path rebuilds don't auto-resize it.</summary>
        public void SetExplicitHeight(int h) { Height = h; _userSizedHeight = true; }

        public IReadOnlyList<Curve> Curves
        {
            get => _curves;
            set
            {
                _curves = value == null ? new List<Curve>() : new List<Curve>(value);
                RebuildPath();
            }
        }

        public Color FillColor
        {
            get => _fill;
            set { _fill = value; Invalidate(); }
        }

        public Color StrokeColor
        {
            get => _stroke;
            set { _stroke = value; Invalidate(); }
        }

        public float StrokeThickness
        {
            get => _strokeThickness;
            set { _strokeThickness = value; Invalidate(); }
        }

        public double Scale
        {
            get => _scale;
            set { _scale = value; RebuildPath(); }
        }

        /// <summary>
        /// Build the path eagerly so the auto-sizing (when the user hasn't
        /// pinned Width / Height) happens before the next paint, not inside
        /// it.
        /// </summary>
        private void RebuildPath()
        {
            _cachedPath = BuildPath();
            Invalidate();
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if (_cachedPath == null) return;
            if (_fill.A > 0)
            {
                using var brush = new SolidBrush(_fill);
                e.Graphics.FillPath(brush, _cachedPath);
            }
            if (_strokeThickness > 0 && _stroke.A > 0)
            {
                using var pen = new Pen(_stroke, _strokeThickness);
                e.Graphics.DrawPath(pen, _cachedPath);
            }
        }

        /// <summary>
        /// Convert the held Rhino curves to an Eto GraphicsPath: each curve becomes
        /// a sub-path emitted as MoveTo + LineTo from the polyline approximation.
        /// The curves themselves aren't mutated — a per-curve duplicate carries
        /// the mirror+scale transform.
        ///
        /// After the mirror around the X axis the polyline Y values are all
        /// negative (Rhino is +Y up, screen is +Y down), so we collect every
        /// transformed polyline, compute the union bounding box, then emit the
        /// path translated so the bounding-box top-left lands at (0,0). When
        /// the user hasn't supplied Width / Height we also auto-size the
        /// Drawable to the bounding box so the shape actually has a frame to
        /// paint into.
        /// </summary>
        private GraphicsPath BuildPath()
        {
            var path = new GraphicsPath();
            if (_curves == null || _curves.Count == 0) return path;

            var mirror = Rhino.Geometry.Transform.Mirror(Plane.WorldZX);
            var scale = Rhino.Geometry.Transform.Scale(Point3d.Origin, _scale);
            var combined = scale * mirror;

            var polys = new List<Polyline>();
            var bb = BoundingBox.Empty;

            foreach (var crv in _curves)
            {
                if (crv == null) continue;
                var copy = crv.DuplicateCurve();
                copy.Transform(combined);

                if (!copy.TryGetPolyline(out var pl))
                {
                    var p = copy.ToPolyline(0, 0, 0.1, 2.0, 0, 0, copy.GetLength() / 50, 0, true);
                    p?.TryGetPolyline(out pl);
                }
                if (pl == null || pl.Count == 0) continue;
                polys.Add(pl);
                bb.Union(pl.BoundingBox);
            }

            if (polys.Count == 0) return path;

            double dx = -bb.Min.X;
            double dy = -bb.Min.Y;
            foreach (var pl in polys)
            {
                path.MoveTo((float)(pl[0].X + dx), (float)(pl[0].Y + dy));
                for (int i = 1; i < pl.Count; i++)
                    path.LineTo((float)(pl[i].X + dx), (float)(pl[i].Y + dy));
            }

            // Auto-size the Drawable when the user didn't pin W / H. Without
            // this the Drawable defaults to 0x0 and the paint never has a
            // visible canvas.
            if (!_userSizedWidth)
                Width = Math.Max(1, (int)Math.Ceiling(bb.Diagonal.X));
            if (!_userSizedHeight)
                Height = Math.Max(1, (int)Math.Ceiling(bb.Diagonal.Y));

            return path;
        }

        /// <summary>System.Drawing.Color → Eto.Drawing.Color shim.</summary>
        public static Color FromSysColor(System.Drawing.Color c)
            => Color.FromArgb(c.R, c.G, c.B, c.A);
    }
}

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Mac implementation of Create Shape. Same Guid + parameter shape as the
    /// Windows WPF version (Components/UI Elements/CreateShape_Component.cs);
    /// the Compile Remove + MacStubs Compile Include in HumanUI.csproj routes
    /// each platform to its respective implementation.
    /// </summary>
    public class CreateShape_Component : GH_Component
    {
        public CreateShape_Component()
            : base("Create Shape", "Shape",
                "Creates a simple shape from a polyline.",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shape", "S", "The shape to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill color. Leave empty for no fill", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weight. Leave empty or set to 0 for no stroke.", GH_ParamAccess.item);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke color", GH_ParamAccess.item, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Width", "W", "The width of the container for the shape. \nLeave blank to autosize.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Height", "H", "The height of the container for the shape. \nLeave blank to autosize.", GH_ParamAccess.item);
            for (int i = 1; i < 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var crvs = new List<Curve>();
            var fillCol = System.Drawing.Color.Transparent;
            double strokeWeight = 0;
            var strokeCol = System.Drawing.Color.Transparent;
            double scale = 1.0;
            int width = 0, height = 0;

            if (!DA.GetDataList("Shape", crvs)) return;
            DA.GetData("Scale", ref scale);

            var shape = new HUI_Shape
            {
                Curves = crvs,
                Scale = scale,
            };
            if (DA.GetData("Fill Color", ref fillCol)) shape.FillColor = HUI_Shape.FromSysColor(fillCol);
            if (DA.GetData("Stroke Weight", ref strokeWeight)) shape.StrokeThickness = (float)strokeWeight;
            if (DA.GetData("Stroke Color", ref strokeCol)) shape.StrokeColor = HUI_Shape.FromSysColor(strokeCol);
            if (DA.GetData("Width", ref width) && width > 0) shape.SetExplicitWidth(width);
            if (DA.GetData("Height", ref height) && height > 0) shape.SetExplicitHeight(height);

            DA.SetData("Shape", new UIElement_Goo(shape, "Shape", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShape;

        public override Guid ComponentGuid => new Guid("{0ab1c8a7-4182-4a7b-bda3-67c24677182c}");
    }
}

namespace HumanUI.Components.UI_Output
{
    /// <summary>Mac implementation of Set Shape — updates the HUI_Shape in place.</summary>
    public class SetShape_Component : GH_Component
    {
        public SetShape_Component()
            : base("Set Shape", "SetShape",
                "Modify an existing shape",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "The shape element to modify", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curve", "SC", "The new curve outline", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddColourParameter("Fill Color", "FC", "Fill color", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddNumberParameter("Stroke Weight", "SW", "Stroke weight", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddColourParameter("Stroke Color", "SC", "Stroke color", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddNumberParameter("Scale", "Scl", "Scale factor", GH_ParamAccess.item, 1.0);
            pManager[5].Optional = true;
            pManager.AddIntegerParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager[6].Optional = true;
            pManager.AddIntegerParameter("Height", "H", "Height", GH_ParamAccess.item);
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object shapeObj = null;
            if (!DA.GetData("Shape to Modify", ref shapeObj)) return;
            var shape = HUI_Util.GetUIElement<HUI_Shape>(shapeObj);
            if (shape == null) return;

            var crvs = new List<Curve>();
            if (DA.GetDataList("Shape Curve", crvs) && crvs.Count > 0) shape.Curves = crvs;

            double scale = 1.0;
            if (DA.GetData("Scale", ref scale)) shape.Scale = scale;

            var fillCol = System.Drawing.Color.Transparent;
            if (DA.GetData("Fill Color", ref fillCol)) shape.FillColor = HUI_Shape.FromSysColor(fillCol);

            double strokeWeight = 0;
            if (DA.GetData("Stroke Weight", ref strokeWeight)) shape.StrokeThickness = (float)strokeWeight;

            var strokeCol = System.Drawing.Color.Transparent;
            if (DA.GetData("Stroke Color", ref strokeCol)) shape.StrokeColor = HUI_Shape.FromSysColor(strokeCol);

            int width = 0, height = 0;
            if (DA.GetData("Width", ref width) && width > 0) shape.SetExplicitWidth(width);
            if (DA.GetData("Height", ref height) && height > 0) shape.SetExplicitHeight(height);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetShape;

        public override Guid ComponentGuid => new Guid("{f6881435-7de3-4098-ada8-f3068ed7331c}");
    }
}

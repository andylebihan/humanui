// Mac implementation of Create Shapes / Set Shapes — the click-mode-aware
// composite of multiple sub-shapes. Same pattern as HUI_Shape (Eto.Drawable
// + GraphicsPath + auto-size to the union bounding box), with per-shape
// fill / stroke / weight, plus point-in-polygon hit testing for the
// Button / Toggle / Picker ClickModes.
//
// Windows keeps its WPF ClickableShapeGrid; this file is only compiled
// into the Mac net7.0 TFM via the MacStubs/**/*.cs Compile Include.

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ToolStripDropDown = System.Object; // unused on Mac, see #if guard in WindowsForms-bound files

namespace HumanUI
{
    /// <summary>
    /// Click-mode semantics for HUI_MultiShape. Numeric values match the
    /// Windows ClickableShapeGrid.ClickMode enum so serialized component
    /// state crosses platforms cleanly.
    /// </summary>
    public enum HUI_MultiShapeClickMode
    {
        ButtonMode = 0,
        ToggleMode = 1,
        PickerMode = 2,
        None = 3,
    }

    /// <summary>
    /// Eto Drawable rendering multiple shapes with per-shape styling and
    /// optional click behavior. SelectedStates is the live list of bools
    /// ValueListener can read via HUI_Util.
    /// </summary>
    public sealed class HUI_MultiShape : Drawable
    {
        private sealed class ShapeEntry
        {
            public List<PointF> Polyline;
            public RectangleF Bounds;
            public Color Fill;
            public Color Stroke;
            public float StrokeWeight;
            public bool IsSelected;
            public bool IsHover;
        }

        private readonly List<ShapeEntry> _shapes = new();
        private double _scale = 1.0;
        private bool _userSizedWidth;
        private bool _userSizedHeight;

        public HUI_MultiShapeClickMode ClickMode { get; set; } = HUI_MultiShapeClickMode.None;

        public IReadOnlyList<bool> SelectedStates =>
            _shapes.Select(s => s.IsSelected).ToList();

        public event EventHandler SelectionChanged;

        public HUI_MultiShape()
        {
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
        }

        public void SetExplicitWidth(int w) { Width = w; _userSizedWidth = true; }
        public void SetExplicitHeight(int h) { Height = h; _userSizedHeight = true; }

        public void SetShapes(
            List<Curve> curves,
            List<System.Drawing.Color> fills,
            List<double> strokeWeights,
            List<System.Drawing.Color> strokes,
            double scale)
        {
            _scale = scale;
            _shapes.Clear();
            if (curves == null || curves.Count == 0)
            {
                Invalidate();
                return;
            }

            var mirror = Rhino.Geometry.Transform.Mirror(Plane.WorldZX);
            var scl = Rhino.Geometry.Transform.Scale(Point3d.Origin, _scale);
            var combined = scl * mirror;

            // Two passes: collect polylines + bounds, then translate so bb top-left
            // sits at (0,0). Same auto-size dance as HUI_Shape.
            var unionBb = BoundingBox.Empty;
            var rawPolys = new List<Polyline>();
            foreach (var crv in curves)
            {
                if (crv == null) continue;
                var copy = crv.DuplicateCurve();
                copy.Transform(combined);
                if (!copy.TryGetPolyline(out var pl))
                {
                    var pcrv = copy.ToPolyline(0, 0, 0.1, 2.0, 0, 0, copy.GetLength() / 50, 0, true);
                    pcrv?.TryGetPolyline(out pl);
                }
                if (pl == null || pl.Count == 0)
                {
                    rawPolys.Add(null);
                    continue;
                }
                rawPolys.Add(pl);
                unionBb.Union(pl.BoundingBox);
            }

            double dx = -unionBb.Min.X;
            double dy = -unionBb.Min.Y;

            for (int i = 0; i < rawPolys.Count; i++)
            {
                var pl = rawPolys[i];
                if (pl == null) continue;

                var pts = new List<PointF>(pl.Count);
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                foreach (var p in pl)
                {
                    var x = (float)(p.X + dx);
                    var y = (float)(p.Y + dy);
                    pts.Add(new PointF(x, y));
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                _shapes.Add(new ShapeEntry
                {
                    Polyline = pts,
                    Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY),
                    Fill = (fills != null && fills.Count > 0)
                        ? ToEto(fills[i % fills.Count])
                        : Colors.Transparent,
                    StrokeWeight = (strokeWeights != null && strokeWeights.Count > 0)
                        ? (float)strokeWeights[i % strokeWeights.Count]
                        : 0f,
                    Stroke = (strokes != null && strokes.Count > 0)
                        ? ToEto(strokes[i % strokes.Count])
                        : Colors.Transparent,
                });
            }

            if (!_userSizedWidth)
                Width = Math.Max(1, (int)Math.Ceiling(unionBb.Diagonal.X));
            if (!_userSizedHeight)
                Height = Math.Max(1, (int)Math.Ceiling(unionBb.Diagonal.Y));

            Invalidate();
        }

        private static Color ToEto(System.Drawing.Color c)
            => Color.FromArgb(c.R, c.G, c.B, c.A);

        private void OnPaint(object sender, PaintEventArgs e)
        {
            foreach (var s in _shapes)
            {
                if (s.Polyline == null || s.Polyline.Count < 2) continue;
                using var path = new GraphicsPath();
                path.MoveTo(s.Polyline[0]);
                for (int i = 1; i < s.Polyline.Count; i++) path.LineTo(s.Polyline[i]);

                if (s.Fill.A > 0)
                {
                    var fill = s.Fill;
                    if (s.IsHover) fill = AdjustAlpha(fill, 0.8f);
                    using var brush = new SolidBrush(fill);
                    e.Graphics.FillPath(brush, path);
                }
                if (s.StrokeWeight > 0 && s.Stroke.A > 0)
                {
                    using var pen = new Pen(s.Stroke, s.StrokeWeight);
                    e.Graphics.DrawPath(pen, path);
                }
                if (s.IsSelected)
                {
                    // Selected highlight: a slightly heavier outline in the
                    // user-defined stroke color (or a dark contrasting tint
                    // when no stroke is set), echoing the WPF drop-shadow.
                    var highlight = s.Stroke.A > 0 ? s.Stroke : new Color(0.1f, 0.1f, 0.1f, 0.8f);
                    using var pen = new Pen(highlight, Math.Max(2f, s.StrokeWeight + 1f));
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private static Color AdjustAlpha(Color c, float multiplier)
            => new Color(c.R, c.G, c.B, c.A * multiplier);

        private int HitTest(PointF p)
        {
            // Reverse iteration so visually-on-top shapes win the hit when overlapping.
            for (int i = _shapes.Count - 1; i >= 0; i--)
            {
                var s = _shapes[i];
                if (!s.Bounds.Contains(p)) continue;
                if (PointInPolygon(p, s.Polyline)) return i;
            }
            return -1;
        }

        /// <summary>Standard ray-casting point-in-polygon.</summary>
        private static bool PointInPolygon(PointF p, List<PointF> poly)
        {
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = poly[i];
                var pj = poly[j];
                if (((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                    (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / ((pj.Y - pi.Y) == 0 ? 1e-9 : (pj.Y - pi.Y)) + pi.X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (ClickMode == HUI_MultiShapeClickMode.None) return;
            int idx = HitTest(e.Location);
            if (idx < 0) return;
            switch (ClickMode)
            {
                case HUI_MultiShapeClickMode.ButtonMode:
                    _shapes[idx].IsSelected = true;
                    break;
                case HUI_MultiShapeClickMode.PickerMode:
                    for (int i = 0; i < _shapes.Count; i++) _shapes[i].IsSelected = (i == idx);
                    break;
                case HUI_MultiShapeClickMode.ToggleMode:
                    _shapes[idx].IsSelected = !_shapes[idx].IsSelected;
                    break;
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (ClickMode == HUI_MultiShapeClickMode.ButtonMode)
            {
                bool anyChange = false;
                foreach (var s in _shapes)
                    if (s.IsSelected) { s.IsSelected = false; anyChange = true; }
                if (anyChange)
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            int idx = HitTest(e.Location);
            bool changed = false;
            for (int i = 0; i < _shapes.Count; i++)
            {
                bool wantHover = i == idx;
                if (_shapes[i].IsHover != wantHover)
                {
                    _shapes[i].IsHover = wantHover;
                    changed = true;
                }
            }
            if (changed) Invalidate();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            bool changed = false;
            foreach (var s in _shapes)
                if (s.IsHover) { s.IsHover = false; changed = true; }
            // Match the WPF gridMouseMove behavior: ButtonMode shapes lose
            // their selection when the mouse leaves without being released.
            if (ClickMode == HUI_MultiShapeClickMode.ButtonMode)
            {
                foreach (var s in _shapes)
                    if (s.IsSelected) { s.IsSelected = false; changed = true; }
                if (changed) SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            if (changed) Invalidate();
        }
    }
}

namespace HumanUI.Components.UI_Elements
{
    public class CreateMultiShape_Component : GH_Component
    {
        internal HUI_MultiShapeClickMode clickMode = HUI_MultiShapeClickMode.None;

        public CreateMultiShape_Component()
            : base("Create Shapes", "Shapes",
                "Creates shapes from polylines.",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shapes", "S", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill colors. Leave empty for no fill", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weights. Leave empty or set to 0 for no stroke.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Optional output width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Optional output height.", GH_ParamAccess.item);
            for (int i = 1; i < 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var crvs = new List<Curve>();
            var fills = new List<System.Drawing.Color>();
            var weights = new List<double>();
            var strokes = new List<System.Drawing.Color>();
            double scale = 1.0;
            double width = 0, height = 0;

            DA.GetDataList("Fill Color", fills);
            DA.GetDataList("Stroke Weight", weights);
            DA.GetDataList("Stroke Color", strokes);
            DA.GetData("Scale", ref scale);
            if (!DA.GetDataList("Shapes", crvs)) return;

            var shape = new HUI_MultiShape { ClickMode = clickMode };
            shape.SetShapes(crvs, fills, weights, strokes, scale);
            if (DA.GetData("Width", ref width) && width > 0) shape.SetExplicitWidth((int)width);
            if (DA.GetData("Height", ref height) && height > 0) shape.SetExplicitHeight((int)height);

            DA.SetData("Shape", new UIElement_Goo(shape, "Shape", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShapes;
        public override Guid ComponentGuid => new Guid("{94288CED-76F6-438A-9216-E60C8290F640}");

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ClickMode", (int)clickMode);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int v = -1;
            if (reader.TryGetInt32("ClickMode", ref v) && Enum.IsDefined(typeof(HUI_MultiShapeClickMode), v))
                clickMode = (HUI_MultiShapeClickMode)v;
            UpdateMessage();
            return base.Read(reader);
        }

        private void UpdateMessage()
        {
            Message = clickMode switch
            {
                HUI_MultiShapeClickMode.ButtonMode => "Button Mode",
                HUI_MultiShapeClickMode.ToggleMode => "Toggle Mode",
                HUI_MultiShapeClickMode.PickerMode => "Picker Mode",
                _ => "",
            };
        }

        // Mac: no ToolStripDropDown menu hook (HUI_WINDOWS gate elsewhere
        // skips AppendAdditionalComponentMenuItems on the net7.0 TFM). The
        // ClickMode is still persisted via Write/Read above so a .gh file
        // saved on Windows with a non-None ClickMode opens on Mac with that
        // mode intact; users on Mac who need to change the mode can edit
        // the file via Windows or wait until the Mac canvas exposes its
        // right-click menu through an Eto-friendly API.
    }
}

namespace HumanUI.Components.UI_Output
{
    public class SetShapes_Component : GH_Component
    {
        public SetShapes_Component()
            : base("Set Shapes", "SetShapes",
                "Replace an existing shape in the window",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "The shapes container to update", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curves", "SC", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Colors", "FC", "The fill colors. Leave empty for no fill", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weights", "SW", "The stroke weights. Leave empty or set to 0 for no stroke.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Colors", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Optional output width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Optional output height.", GH_ParamAccess.item);
            for (int i = 2; i < pManager.ParamCount; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object shapeObj = null;
            if (!DA.GetData("Shape to Modify", ref shapeObj)) return;
            var shape = HUI_Util.GetUIElement<HUI_MultiShape>(shapeObj);
            if (shape == null) return;

            var crvs = new List<Curve>();
            if (!DA.GetDataList("Shape Curves", crvs)) return;

            var fills = new List<System.Drawing.Color>();
            var weights = new List<double>();
            var strokes = new List<System.Drawing.Color>();
            double scale = 1.0;
            DA.GetDataList("Fill Colors", fills);
            DA.GetDataList("Stroke Weights", weights);
            DA.GetDataList("Stroke Colors", strokes);
            DA.GetData("Scale", ref scale);

            shape.SetShapes(crvs, fills, weights, strokes, scale);

            double width = 0, height = 0;
            if (DA.GetData("Width", ref width) && width > 0) shape.SetExplicitWidth((int)width);
            if (DA.GetData("Height", ref height) && height > 0) shape.SetExplicitHeight((int)height);
        }

        public override Guid ComponentGuid => new Guid("{EDC4A536-7412-46F2-B56F-6D8668D6B983}");
    }
}

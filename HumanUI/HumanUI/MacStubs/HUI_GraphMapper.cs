// Mac implementation of Create Graph Mapper — bezier-curve editor with
// four draggable control points. Windows keeps the WPF GraphMapperElement
// (Custom Types/GraphMapperElement.cs); this file is only compiled into
// the Mac net7.0 TFM via the MacStubs/**/*.cs Compile Include.
//
// Control points are stored as normalized [0,1] coordinates. C0 and C3 are
// endpoint handles constrained to X=0 and X=1 respectively (Y is movable);
// C1 and C2 are free handles. Display flips the Y axis so Y=0 sits at the
// bottom of the drawable, matching the WPF version.

using System;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace HumanUI
{
    /// <summary>
    /// Eto.Drawable bezier graph mapper. Four control points (C0..C3) in
    /// normalized [0,1] space. C0 / C3 are X-locked; C1 / C2 are free.
    /// Fires HandlesChanged whenever a handle is dragged so ValueListener
    /// can re-solve. Exposes GetCurve() returning a Rhino BezierCurve for
    /// the downstream remap consumers.
    /// </summary>
    public sealed class HUI_GraphMapper : Drawable
    {
        private const float Padding = 10f;
        private const float HandleSize = 12f;
        private const float HandleHitRadius = 14f;

        private PointF _c0 = new(0f, 0f);
        private PointF _c1 = new(0.25f, 0.25f);
        private PointF _c2 = new(0.75f, 0.75f);
        private PointF _c3 = new(1f, 1f);

        private int _dragHandle = -1; // 0..3, or -1 when not dragging
        private bool _hoverHandle;

        public event EventHandler HandlesChanged;

        public HUI_GraphMapper()
        {
            // Match the WPF Viewbox size so windows from saved .gh files look
            // similar across platforms.
            Width = 230;
            Height = 230;
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseLeave += (_, _) => { if (_hoverHandle) { _hoverHandle = false; Invalidate(); } };
        }

        public HUI_GraphMapper(PointF c0, PointF c1, PointF c2, PointF c3) : this()
        {
            _c0 = c0; _c1 = c1; _c2 = c2; _c3 = c3;
            Invalidate();
        }

        public PointF C0 => _c0;
        public PointF C1 => _c1;
        public PointF C2 => _c2;
        public PointF C3 => _c3;

        /// <summary>Initialize the four control points from a GH BezierGraph's grips.</summary>
        public void SetByGrips(GH_BezierGraph graph)
        {
            if (graph?.Grips == null || graph.Grips.Count < 4) return;
            _c0 = new PointF((float)graph.Grips[0].X, (float)graph.Grips[0].Y);
            _c1 = new PointF((float)graph.Grips[1].X, (float)graph.Grips[1].Y);
            _c2 = new PointF((float)graph.Grips[2].X, (float)graph.Grips[2].Y);
            _c3 = new PointF((float)graph.Grips[3].X, (float)graph.Grips[3].Y);
            Invalidate();
        }

        /// <summary>Return the current curve as a Rhino BezierCurve in normalized [0,1] space.</summary>
        public BezierCurve GetCurve()
        {
            return new BezierCurve(new[]
            {
                new Point3d(_c0.X, _c0.Y, 0),
                new Point3d(_c1.X, _c1.Y, 0),
                new Point3d(_c2.X, _c2.Y, 0),
                new Point3d(_c3.X, _c3.Y, 0),
            });
        }

        private RectangleF GetFrameRect()
        {
            var w = (float)Width;
            var h = (float)Height;
            return new RectangleF(Padding, Padding, w - 2 * Padding, h - 2 * Padding);
        }

        private PointF NormalizedToPixel(PointF n, RectangleF frame)
        {
            // Y flipped: normalized Y=0 → bottom of frame, Y=1 → top.
            return new PointF(
                frame.X + n.X * frame.Width,
                frame.Y + (1f - n.Y) * frame.Height);
        }

        private PointF PixelToNormalized(PointF p, RectangleF frame)
        {
            float nx = frame.Width <= 0 ? 0 : (p.X - frame.X) / frame.Width;
            float ny = frame.Height <= 0 ? 0 : 1f - (p.Y - frame.Y) / frame.Height;
            return new PointF(Math.Clamp(nx, 0, 1), Math.Clamp(ny, 0, 1));
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var frame = GetFrameRect();
            if (frame.Width <= 0 || frame.Height <= 0) return;

            // Frame: faint outer border, thin inner border, white fill.
            using (var fillBrush = new SolidBrush(new Color(1f, 1f, 1f, 0.5f)))
                g.FillRectangle(fillBrush, frame);
            using (var pen = new Pen(Colors.Black, 1f))
                g.DrawRectangle(pen, frame);

            // Light gridlines at the quarter points so the mapping is readable.
            using (var gridPen = new Pen(new Color(0, 0, 0, 0.15f), 1f))
            {
                for (int i = 1; i <= 3; i++)
                {
                    float xq = frame.X + frame.Width * i / 4f;
                    float yq = frame.Y + frame.Height * i / 4f;
                    g.DrawLine(gridPen, xq, frame.Y, xq, frame.Y + frame.Height);
                    g.DrawLine(gridPen, frame.X, yq, frame.X + frame.Width, yq);
                }
            }

            var p0 = NormalizedToPixel(_c0, frame);
            var p1 = NormalizedToPixel(_c1, frame);
            var p2 = NormalizedToPixel(_c2, frame);
            var p3 = NormalizedToPixel(_c3, frame);

            // Red handle lines connecting each endpoint to its control point.
            using (var pen = new Pen(Colors.Red, 1f))
            {
                g.DrawLine(pen, p0, p1);
                g.DrawLine(pen, p3, p2);
            }

            // Bezier curve.
            using (var path = new GraphicsPath())
            {
                path.MoveTo(p0);
                path.AddBezier(p0, p1, p2, p3);
                using var pen = new Pen(Colors.Black, 3f);
                g.DrawPath(pen, path);
            }

            // Handles.
            using var handleFill = new SolidBrush(Colors.Red);
            using var handleStroke = new Pen(Colors.Black, 2f);
            foreach (var p in new[] { p0, p1, p2, p3 })
            {
                var hr = new RectangleF(p.X - HandleSize / 2f, p.Y - HandleSize / 2f, HandleSize, HandleSize);
                g.FillEllipse(handleFill, hr);
                g.DrawEllipse(handleStroke, hr);
            }
        }

        private int HitHandle(PointF p, RectangleF frame)
        {
            var pts = new[]
            {
                NormalizedToPixel(_c0, frame),
                NormalizedToPixel(_c1, frame),
                NormalizedToPixel(_c2, frame),
                NormalizedToPixel(_c3, frame),
            };
            for (int i = 0; i < 4; i++)
            {
                var dx = pts[i].X - p.X;
                var dy = pts[i].Y - p.Y;
                if (dx * dx + dy * dy <= HandleHitRadius * HandleHitRadius) return i;
            }
            return -1;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Primary) return;
            var frame = GetFrameRect();
            int idx = HitHandle(e.Location, frame);
            if (idx < 0) return;
            _dragHandle = idx;
            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_dragHandle >= 0)
            {
                _dragHandle = -1;
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var frame = GetFrameRect();
            if (_dragHandle < 0)
            {
                // Hover feedback — kept light so it doesn't dominate the UI.
                bool nowHover = HitHandle(e.Location, frame) >= 0;
                if (nowHover != _hoverHandle)
                {
                    _hoverHandle = nowHover;
                    Cursor = nowHover ? Cursors.Pointer : Cursors.Default;
                }
                return;
            }

            var n = PixelToNormalized(e.Location, frame);
            switch (_dragHandle)
            {
                case 0: _c0 = new PointF(0f, n.Y); break;
                case 1: _c1 = n; break;
                case 2: _c2 = n; break;
                case 3: _c3 = new PointF(1f, n.Y); break;
            }
            HandlesChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}

namespace HumanUI.Components.UI_Elements
{
    public class CreateGraphMapper_Component : GH_Component
    {
        public CreateGraphMapper_Component()
            : base("Create Graph Mapper", "GraphMapper",
                "Creates a Bezier Graph Mapper",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Graph Mapper", "G", "An optional Graph Mapper to represent the starting configuration. Connect directly or use Metahopper to obtain a reference.", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Graph Mapper", "G", "The Graph Mapper UI element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_GraphMapper gm = null;
            try { gm = Params.Input[0].Sources.OfType<GH_GraphMapper>().FirstOrDefault(); }
            catch { /* upstream slot not wired to a GH_GraphMapper */ }
            if (gm == null)
            {
                try { DA.GetData("Graph Mapper", ref gm); } catch { /* not a GraphMapper instance */ }
            }

            var mapper = new HUI_GraphMapper();
            if (gm != null)
            {
                if (gm.Graph.GraphTypeID != new Guid("{7026A6D2-9B94-4314-B6D3-6850EFF942FE}"))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Currently, only Bezier type graphs are supported.");
                }
                else if (gm.Graph is GH_BezierGraph bz)
                {
                    mapper.SetByGrips(bz);
                }
            }

            DA.SetData("Graph Mapper", new UIElement_Goo(mapper, "Graph Mapper", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GraphMapper;
        public override Guid ComponentGuid => new Guid("{bc47947b-f83f-4cdd-b7d8-abc546a26c5e}");
    }
}

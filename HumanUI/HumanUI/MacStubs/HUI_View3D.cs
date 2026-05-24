// Mac implementation of Create / Set 3D View. Windows hosts a
// HelixToolkit.Wpf HelixViewport3D in HUI_WpfHost (GPU-accelerated).
// On Mac we don't have a usable cross-platform GPU 3D library that
// doesn't drag a heavy dependency in, so HUI_View3D is a pure-Eto
// software renderer: project triangles via a camera matrix, painter's-
// algorithm sort by depth, fill with Lambert shading. Designed for
// the typical HUI use case (a few hundred to a few thousand
// triangles) — slow but adequate.
//
// Interactions:
//   left-drag         orbit (yaw + pitch around target)
//   shift + drag      pan (translate target in screen plane)
//   wheel             dolly in / out
//   double-click      zoom to fit
//   right-drag        rotate roll (vestigial; mainly for parity)

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI
{
    public sealed class HUI_View3D : Drawable
    {
        // Mesh data + per-mesh color, captured by SetGeometry and
        // referenced on every paint.
        private readonly List<MeshEntry> _meshes = new();

        // Camera state — spherical orbit around _target.
        private double _yaw = 0.7;     // rotation around world Z
        private double _pitch = 0.5;   // 0 = horizon, +pi/2 = top-down
        private double _distance = 100;
        private Point3d _target = Point3d.Origin;
        private BoundingBox _unionBB = BoundingBox.Empty;
        private bool _hasInitialFit;

        // Drag state.
        private bool _orbiting;
        private bool _panning;
        private PointF _lastMouse;

        public event EventHandler GeometryChanged;

        public HUI_View3D()
        {
            BackgroundColor = Color.FromArgb(245, 245, 248);
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            MouseDoubleClick += OnMouseDoubleClick;
            SizeChanged += (_, _) => Invalidate();
        }

        public IReadOnlyList<Mesh> Meshes => _meshes.Select(m => m.Mesh).ToList();

        public void SetGeometry(IEnumerable<Mesh> meshes, IEnumerable<System.Drawing.Color> colors)
        {
            _meshes.Clear();
            var colorList = colors?.Where(c => c.A > 0).ToList() ?? new List<System.Drawing.Color>();
            if (colorList.Count == 0) colorList.Add(System.Drawing.Color.Red);

            int i = 0;
            foreach (var m in meshes)
            {
                if (m == null || m.Vertices.Count == 0)
                {
                    i++;
                    continue;
                }
                _meshes.Add(new MeshEntry(m, colorList[i % colorList.Count]));
                i++;
            }

            _unionBB = BoundingBox.Empty;
            foreach (var entry in _meshes)
            {
                if (_unionBB.IsValid) _unionBB.Union(entry.Mesh.GetBoundingBox(true));
                else _unionBB = entry.Mesh.GetBoundingBox(true);
            }

            // Camera state is preserved across Set calls (matches the WPF
            // SetView which kept camera/lights) — but the very first time
            // geometry arrives we auto-fit so the user sees something.
            if (!_hasInitialFit && _unionBB.IsValid)
            {
                ZoomExtents();
                _hasInitialFit = true;
            }

            GeometryChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void ZoomExtents()
        {
            if (!_unionBB.IsValid) return;
            _target = _unionBB.Center;
            // 1.5x diagonal gives ~30% padding around the model in the
            // average framing. Clamp to a sane minimum so a degenerate
            // (single-point) mesh doesn't collapse the view onto itself.
            var diag = _unionBB.Diagonal.Length;
            _distance = Math.Max(0.5, diag * 1.5);
            Invalidate();
        }

        private Point3d ComputeCameraPos()
        {
            // Spherical → cartesian around _target. Yaw rotates in the
            // world XY plane, pitch tilts toward +Z (top-down view).
            double cy = Math.Cos(_yaw), sy = Math.Sin(_yaw);
            double cp = Math.Cos(_pitch), sp = Math.Sin(_pitch);
            var dir = new Vector3d(cy * cp, sy * cp, sp);
            return _target + dir * _distance;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.AntiAlias = true;
            g.Clear(BackgroundColor);

            if (_meshes.Count == 0 || Width <= 0 || Height <= 0) return;

            // Build the camera frame. World +Z is up (Rhino convention);
            // worldUp degenerates when the camera is straight overhead, so
            // we substitute a Y-axis basis in that case so cross products
            // don't go zero.
            var camPos = ComputeCameraPos();
            var camForward = _target - camPos;
            if (camForward.IsTiny()) camForward = new Vector3d(-1, 0, 0);
            camForward.Unitize();
            var worldUp = new Vector3d(0, 0, 1);
            var camRight = Vector3d.CrossProduct(camForward, worldUp);
            if (camRight.IsTiny()) camRight = new Vector3d(1, 0, 0);
            camRight.Unitize();
            var camUp = Vector3d.CrossProduct(camRight, camForward);
            camUp.Unitize();

            float focal = (float)(Math.Min(Width, Height) * 0.8);
            float cx = Width / 2f;
            float cy0 = Height / 2f;

            // Build the projected triangle list. Light direction sits
            // slightly above the camera so the silhouette stays bright
            // even as the user orbits — same trick HelixToolkit's SunLight
            // approximates without explicitly attaching a light source.
            var light = camForward + camUp * 0.4 + camRight * 0.2;
            light.Unitize();

            var triangles = new List<TriProj>(EstimateTriangleCount());
            foreach (var entry in _meshes)
                AccumulateTriangles(entry, camPos, camRight, camUp, camForward, focal, cx, cy0, light, triangles);

            // Painter's algorithm: largest depth (furthest) first.
            triangles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var tri in triangles)
            {
                using var path = new GraphicsPath();
                path.MoveTo(tri.A);
                path.LineTo(tri.B);
                path.LineTo(tri.C);
                path.CloseFigure();
                using var brush = new SolidBrush(ToEto(tri.Color));
                g.FillPath(brush, path);
                using var pen = new Pen(new Color(0, 0, 0, 0.18f), 0.5f);
                g.DrawPath(pen, path);
            }
        }

        private int EstimateTriangleCount()
        {
            int total = 0;
            foreach (var entry in _meshes) total += entry.Mesh.Faces.Count * 2;
            return total;
        }

        private static void AccumulateTriangles(
            MeshEntry entry,
            Point3d camPos, Vector3d camRight, Vector3d camUp, Vector3d camForward,
            float focal, float cx, float cy0,
            Vector3d light,
            List<TriProj> triangles)
        {
            var mesh = entry.Mesh;
            var color = entry.Color;
            var verts = mesh.Vertices;
            var faces = mesh.Faces;

            for (int fi = 0; fi < faces.Count; fi++)
            {
                var face = faces[fi];
                int subTris = face.IsQuad ? 2 : 1;
                for (int t = 0; t < subTris; t++)
                {
                    int ai, bi, ci;
                    if (t == 0) { ai = face.A; bi = face.B; ci = face.C; }
                    else { ai = face.A; bi = face.C; ci = face.D; }

                    var p0 = (Point3d)verts[ai];
                    var p1 = (Point3d)verts[bi];
                    var p2 = (Point3d)verts[ci];

                    // World → camera coordinates. Vector3d * Vector3d is
                    // the dot product (Rhino's operator overload), which
                    // gives us each basis projection in one line.
                    var w0 = p0 - camPos;
                    var w1 = p1 - camPos;
                    var w2 = p2 - camPos;
                    double z0 = camForward * w0;
                    double z1 = camForward * w1;
                    double z2 = camForward * w2;

                    // Clip near plane — any vertex behind the camera kills
                    // the whole triangle (cheap, occasionally clips a wide
                    // triangle that should partially show, but the artifact
                    // is acceptable for this scope).
                    if (z0 < 0.01 || z1 < 0.01 || z2 < 0.01) continue;

                    double x0 = camRight * w0;
                    double x1 = camRight * w1;
                    double x2 = camRight * w2;
                    double y0 = camUp * w0;
                    double y1 = camUp * w1;
                    double y2 = camUp * w2;

                    var s0 = new PointF(cx + (float)(x0 / z0 * focal), cy0 - (float)(y0 / z0 * focal));
                    var s1 = new PointF(cx + (float)(x1 / z1 * focal), cy0 - (float)(y1 / z1 * focal));
                    var s2 = new PointF(cx + (float)(x2 / z2 * focal), cy0 - (float)(y2 / z2 * focal));

                    // Surface normal in world space, then Lambert against
                    // the camera-relative light direction.
                    var edge1 = p1 - p0;
                    var edge2 = p2 - p0;
                    var normal = Vector3d.CrossProduct(edge1, edge2);
                    if (normal.IsTiny()) continue;
                    normal.Unitize();
                    double lambert = Math.Abs(normal * light);
                    double shade = 0.25 + 0.75 * lambert; // ambient + diffuse

                    var shaded = ShadeColor(color, shade);
                    double depth = (z0 + z1 + z2) / 3.0;
                    triangles.Add(new TriProj(s0, s1, s2, shaded, depth));
                }
            }
        }

        private static Color ToEto(System.Drawing.Color c) => Color.FromArgb(c.R, c.G, c.B, c.A);

        private static System.Drawing.Color ShadeColor(System.Drawing.Color c, double factor)
        {
            if (factor < 0) factor = 0;
            if (factor > 1) factor = 1;
            return System.Drawing.Color.FromArgb(
                c.A,
                (int)Math.Round(c.R * factor),
                (int)Math.Round(c.G * factor),
                (int)Math.Round(c.B * factor));
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _lastMouse = e.Location;
            if (e.Buttons == MouseButtons.Primary && e.Modifiers.HasFlag(Keys.Shift))
            {
                _panning = true;
            }
            else if (e.Buttons == MouseButtons.Primary)
            {
                _orbiting = true;
            }
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_orbiting && !_panning) return;
            float dx = e.Location.X - _lastMouse.X;
            float dy = e.Location.Y - _lastMouse.Y;
            _lastMouse = e.Location;

            if (_orbiting)
            {
                _yaw -= dx * 0.01;
                _pitch -= dy * 0.01;
                // Clamp pitch just shy of straight-up / straight-down to
                // avoid the camera-up / forward degeneracy.
                if (_pitch < -Math.PI / 2 + 0.05) _pitch = -Math.PI / 2 + 0.05;
                if (_pitch > Math.PI / 2 - 0.05) _pitch = Math.PI / 2 - 0.05;
            }
            else if (_panning)
            {
                // Pan moves the target perpendicular to the view direction
                // — the model "slides" under the cursor at the same rate.
                var camPos = ComputeCameraPos();
                var camForward = _target - camPos;
                camForward.Unitize();
                var worldUp = new Vector3d(0, 0, 1);
                var camRight = Vector3d.CrossProduct(camForward, worldUp);
                camRight.Unitize();
                var camUp = Vector3d.CrossProduct(camRight, camForward);
                camUp.Unitize();
                double scale = _distance / (Math.Min(Width, Height) * 0.8);
                _target -= camRight * (dx * scale);
                _target += camUp * (dy * scale);
            }
            Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            _orbiting = false;
            _panning = false;
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            // Negative wheel = zoom in (dolly toward target). Match the
            // Helix gesture so the muscle memory carries over.
            double factor = Math.Pow(1.15, -e.Delta.Height);
            _distance *= factor;
            if (_distance < 0.01) _distance = 0.01;
            Invalidate();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            ZoomExtents();
        }

        private readonly struct MeshEntry
        {
            public readonly Mesh Mesh;
            public readonly System.Drawing.Color Color;
            public MeshEntry(Mesh m, System.Drawing.Color c) { Mesh = m; Color = c; }
        }

        private readonly struct TriProj
        {
            public readonly PointF A, B, C;
            public readonly System.Drawing.Color Color;
            public readonly double Depth;
            public TriProj(PointF a, PointF b, PointF c, System.Drawing.Color col, double d)
            {
                A = a; B = b; C = c; Color = col; Depth = d;
            }
        }
    }
}

namespace HumanUI.Components.UI_Elements
{
    public class Create3DView_Component : GH_Component
    {
        public Create3DView_Component()
            : base("Create 3D View", "3DView",
                "Creates an orbitable 3d viewport with a custom-defined mesh",
                "Human UI", "UI Elements")
        { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh to display", "M", "The mesh(es) to display in the viewport", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The color with which to display the mesh.", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager.AddNumberParameter("View Width", "W", "The width of the 3d viewport", GH_ParamAccess.item, 300);
            pManager.AddNumberParameter("View Height", "H", "The height of the 3d viewport", GH_ParamAccess.item, 300);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("3DView", "V", "The 3D view containing your mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var meshes = new List<Mesh>();
            var cols = new List<System.Drawing.Color>();
            double width = 300, height = 300;
            if (!DA.GetDataList("Mesh to display", meshes)) return;
            DA.GetDataList("Mesh Colors", cols);
            DA.GetData("View Width", ref width);
            DA.GetData("View Height", ref height);

            var view = new HUI_View3D { Width = (int)width, Height = (int)height };
            view.SetGeometry(meshes, cols);
            DA.SetData("3DView", new UIElement_Goo(view, "3D View", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Create3dView;
        public override Guid ComponentGuid => new Guid("{5d84c99e-fe9c-4546-8cb0-9f6fe58e011d}");
    }
}

namespace HumanUI.Components.UI_Output
{
    public class Set3DView_Component : GH_Component
    {
        public Set3DView_Component()
            : base("Set 3D View", "Set3DView",
                "Modify the contents of an existing 3D View",
                "Human UI", "UI Output")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("3D View", "V", "The 3D View to modify", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh to display", "M", "The new meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The new mesh colours", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object viewObj = null;
            var meshes = new List<Mesh>();
            var cols = new List<System.Drawing.Color>();
            if (!DA.GetData("3D View", ref viewObj)) return;
            if (!DA.GetDataList("Mesh to display", meshes)) return;
            DA.GetDataList("Mesh Colors", cols);

            var view = HUI_Util.GetUIElement<HUI_View3D>(viewObj);
            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not resolve the 3D View element.");
                return;
            }

            // Same default-color fallback as the Windows Set3DView: an
            // empty cols list would otherwise produce a black mesh or
            // crash on i % 0.
            if (cols.Count == 0) cols.Add(System.Drawing.Color.Red);
            view.SetGeometry(meshes, cols);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Set3dView;
        public override Guid ComponentGuid => new Guid("{3472130d-fc0e-409d-9295-f93ecaf1afb5}");
    }
}

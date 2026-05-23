// Mac implementation of Create Gradient Editor — gradient strip with
// draggable color stops and optional preset dropdown. Windows keeps the
// WPF HUI_GradientEditor (Custom Types/HUI_GradientEditor.cs); this file
// is only compiled into the Mac net7.0 TFM via the MacStubs/**/*.cs
// Compile Include.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.GUI.Gradient;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;

namespace HumanUI
{
    /// <summary>
    /// Mac-side gradient data type. Same surface as the WPF HUI_Gradient on
    /// Windows (Stops, ToString / FromString round-trip, FromGHGradient) but
    /// the stop color is stored as System.Drawing.Color so it doesn't drag
    /// in any WPF dependency. Downstream consumers read the gradient via
    /// HUI_Util.GetElementValue.
    /// </summary>
    public sealed class HUI_Gradient
    {
        public List<(double T, System.Drawing.Color Color)> Stops { get; }

        public HUI_Gradient()
        {
            Stops = new List<(double, System.Drawing.Color)>();
        }

        public HUI_Gradient(IEnumerable<double> ts, IEnumerable<System.Drawing.Color> colors)
        {
            Stops = new List<(double, System.Drawing.Color)>();
            using var ti = ts.GetEnumerator();
            using var ci = colors.GetEnumerator();
            while (ti.MoveNext() && ci.MoveNext()) Stops.Add((ti.Current, ci.Current));
            Stops.Sort((a, b) => a.T.CompareTo(b.T));
        }

        public override string ToString()
        {
            // Same line format as the Windows HUI_Gradient.ToString so .gh
            // files with serialized presets parse identically on either
            // platform.
            var sb = new System.Text.StringBuilder();
            foreach (var (t, c) in Stops)
                sb.AppendLine($"{c.A},{c.R},{c.G},{c.B},{t.ToString(CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

        public static HUI_Gradient FromString(string s)
        {
            var g = new HUI_Gradient();
            if (string.IsNullOrWhiteSpace(s)) return g;
            foreach (var line in s.Split('\n', '\r'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var bits = line.Split(',');
                    var c = System.Drawing.Color.FromArgb(byte.Parse(bits[0]), byte.Parse(bits[1]), byte.Parse(bits[2]), byte.Parse(bits[3]));
                    double t = double.Parse(bits[4], CultureInfo.InvariantCulture);
                    g.Stops.Add((t, c));
                }
                catch { /* skip malformed lines — matches the WPF parser's lenience */ }
            }
            g.Stops.Sort((a, b) => a.T.CompareTo(b.T));
            return g;
        }

        /// <summary>Read a Grasshopper gradient via reflection into the (m_grips) field.</summary>
        public static HUI_Gradient FromGHGradient(GH_Gradient gh)
        {
            var result = new HUI_Gradient();
            try
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField;
                var field = typeof(GH_Gradient).GetField("m_grips", flags);
                var grips = field?.GetValue(gh) as System.Collections.IList;
                if (grips == null) return result;
                foreach (var g in grips)
                {
                    var t = (double)g.GetType().GetProperty("Parameter").GetValue(g);
                    var c = (System.Drawing.Color)g.GetType().GetProperty("ColourLeft").GetValue(g);
                    result.Stops.Add((t, c));
                }
                result.Stops.Sort((a, b) => a.T.CompareTo(b.T));
            }
            catch { /* leave the gradient empty if reflection fails */ }
            return result;
        }

        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Eto.Panel containing a HUI_GradientStrip (Drawable) and optional
    /// preset DropDown. Listens to handle drags / preset selection and
    /// re-paints itself, then fires GradientChanged for ValueListener.
    /// </summary>
    public sealed class HUI_GradientEditor : Panel
    {
        private readonly HUI_GradientStrip _strip;
        private readonly DropDown _presetSelector;

        public bool HasOptionSelector { get; }

        public event EventHandler GradientChanged;

        public HUI_Gradient Gradient
        {
            get => _strip.Gradient;
            set { _strip.Gradient = value; }
        }

        public HUI_GradientEditor(bool hasOptionSelector, bool showEditor, List<HUI_Gradient> presets)
        {
            HasOptionSelector = hasOptionSelector;
            Padding = new Padding(10);

            _strip = new HUI_GradientStrip();
            _strip.GradientChanged += (_, _) => GradientChanged?.Invoke(this, EventArgs.Empty);

            var stack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 6,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };

            if (showEditor)
            {
                stack.Items.Add(new StackLayoutItem(_strip, HorizontalAlignment.Stretch, expand: true));
            }

            if (hasOptionSelector && presets != null && presets.Count > 0)
            {
                _presetSelector = new DropDown();
                for (int i = 0; i < presets.Count; i++)
                {
                    var label = string.IsNullOrEmpty(presets[i].DisplayName)
                        ? $"Preset {i + 1}"
                        : presets[i].DisplayName;
                    _presetSelector.Items.Add(new ListItem { Text = label, Tag = presets[i] });
                }
                _presetSelector.SelectedIndex = 0;
                _presetSelector.SelectedIndexChanged += (_, _) =>
                {
                    if (_presetSelector.SelectedValue is ListItem li && li.Tag is HUI_Gradient g)
                    {
                        Gradient = CloneGradient(g);
                        GradientChanged?.Invoke(this, EventArgs.Empty);
                    }
                };
                stack.Items.Add(_presetSelector);
            }

            Content = stack;

            if (presets != null && presets.Count > 0)
                Gradient = CloneGradient(presets[0]);
            else
                Gradient = DefaultBlackWhite();
        }

        private static HUI_Gradient DefaultBlackWhite()
        {
            return new HUI_Gradient(
                new[] { 0.0, 1.0 },
                new[] { System.Drawing.Color.Black, System.Drawing.Color.White });
        }

        private static HUI_Gradient CloneGradient(HUI_Gradient src)
        {
            // The strip mutates Stops in place; clone so picking a preset
            // multiple times always starts from the canonical version.
            var clone = new HUI_Gradient();
            foreach (var s in src.Stops) clone.Stops.Add(s);
            clone.DisplayName = src.DisplayName;
            return clone;
        }
    }

    /// <summary>
    /// The interactive gradient strip. Paints a transparency-checker
    /// background, the linear gradient on top, and a row of color stops
    /// below the strip. Drag a stop to move it; click below the stops to
    /// add a new one at that offset; double-click a stop to open an Eto
    /// ColorDialog; drag a stop vertically off the strip to delete it.
    /// </summary>
    internal sealed class HUI_GradientStrip : Drawable
    {
        private const float StripHeight = 24f;
        private const float HandleSize = 14f;
        private const float HandleStripGap = 4f;
        private const float DeleteDragDistance = 28f;

        private readonly List<Stop> _stops = new();
        private int _dragIndex = -1;
        private bool _dragOutOfBounds;
        private int _hoverIndex = -1;

        public event EventHandler GradientChanged;

        public HUI_Gradient Gradient
        {
            get
            {
                var g = new HUI_Gradient();
                foreach (var s in _stops) g.Stops.Add((s.T, s.Color));
                return g;
            }
            set
            {
                _stops.Clear();
                if (value?.Stops != null)
                    foreach (var (t, c) in value.Stops) _stops.Add(new Stop { T = t, Color = c });
                _stops.Sort((a, b) => a.T.CompareTo(b.T));
                Invalidate();
            }
        }

        public HUI_GradientStrip()
        {
            Height = (int)(StripHeight + HandleStripGap + HandleSize + 4);
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseDoubleClick += OnMouseDoubleClick;
        }

        private RectangleF GetStripRect()
        {
            return new RectangleF(0, 0, Width, StripHeight);
        }

        private float OffsetToX(double t)
        {
            return (float)(t * Width);
        }

        private double XToOffset(float x)
        {
            return Width <= 0 ? 0 : Math.Clamp(x / (double)Width, 0.0, 1.0);
        }

        private PointF HandleCenter(int i)
        {
            return new PointF(OffsetToX(_stops[i].T), StripHeight + HandleStripGap + HandleSize / 2f);
        }

        private int HitHandle(PointF p)
        {
            for (int i = _stops.Count - 1; i >= 0; i--)
            {
                var c = HandleCenter(i);
                var dx = c.X - p.X;
                var dy = c.Y - p.Y;
                if (dx * dx + dy * dy <= (HandleSize / 2f + 2) * (HandleSize / 2f + 2)) return i;
            }
            return -1;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var strip = GetStripRect();
            if (strip.Width <= 0) return;

            DrawTransparencyChecker(g, strip);

            if (_stops.Count >= 2)
            {
                // Eto LinearGradientBrush only carries two stops, so we
                // shade each adjacent stop-pair in its own sub-rect to
                // approximate multi-stop gradients. Constructor order is
                // (startColor, endColor, startPoint, endPoint).
                for (int i = 0; i < _stops.Count - 1; i++)
                {
                    var x0 = OffsetToX(_stops[i].T);
                    var x1 = OffsetToX(_stops[i + 1].T);
                    if (x1 - x0 < 0.5f) continue;
                    var segment = new RectangleF(x0, strip.Y, x1 - x0, strip.Height);
                    using var segBrush = new LinearGradientBrush(
                        ToEto(_stops[i].Color), ToEto(_stops[i + 1].Color),
                        new PointF(x0, 0), new PointF(x1, 0));
                    g.FillRectangle(segBrush, segment);
                }
            }
            else if (_stops.Count == 1)
            {
                using var br = new SolidBrush(ToEto(_stops[0].Color));
                g.FillRectangle(br, strip);
            }

            using (var pen = new Pen(Colors.Black, 1f))
                g.DrawRectangle(pen, strip);

            // Handles.
            for (int i = 0; i < _stops.Count; i++)
            {
                if (i == _dragIndex && _dragOutOfBounds) continue;
                var c = HandleCenter(i);
                var hr = new RectangleF(c.X - HandleSize / 2f, c.Y - HandleSize / 2f, HandleSize, HandleSize);
                using var fill = new SolidBrush(ToEto(_stops[i].Color));
                g.FillEllipse(fill, hr);
                using var stroke = new Pen(i == _hoverIndex || i == _dragIndex ? Colors.Blue : Colors.Black, i == _dragIndex ? 2.5f : 1.5f);
                g.DrawEllipse(stroke, hr);
            }
        }

        private static void DrawTransparencyChecker(Graphics g, RectangleF rect)
        {
            const float tile = 6f;
            var dark = new Color(0.78f, 0.78f, 0.78f);
            var light = Colors.White;
            using var dBrush = new SolidBrush(dark);
            using var lBrush = new SolidBrush(light);
            g.FillRectangle(lBrush, rect);
            for (float y = rect.Y; y < rect.Y + rect.Height; y += tile)
            {
                for (float x = rect.X; x < rect.X + rect.Width; x += tile)
                {
                    bool odd = (((int)((x - rect.X) / tile) + (int)((y - rect.Y) / tile)) & 1) == 1;
                    if (odd) g.FillRectangle(dBrush, new RectangleF(x, y, tile, tile));
                }
            }
        }

        private static Color ToEto(System.Drawing.Color c) => Color.FromArgb(c.R, c.G, c.B, c.A);
        private static System.Drawing.Color FromEto(Color c)
            => System.Drawing.Color.FromArgb(
                (int)Math.Round(c.A * 255),
                (int)Math.Round(c.R * 255),
                (int)Math.Round(c.G * 255),
                (int)Math.Round(c.B * 255));

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Primary) return;
            int idx = HitHandle(e.Location);
            if (idx >= 0)
            {
                _dragIndex = idx;
                _dragOutOfBounds = false;
                e.Handled = true;
                return;
            }
            // Click below the strip but above the handles' bottom area adds a new stop.
            var strip = GetStripRect();
            if (e.Location.Y < strip.Y + strip.Height) return;
            var t = XToOffset(e.Location.X);
            var color = SampleAt(t);
            _stops.Add(new Stop { T = t, Color = color });
            _stops.Sort((a, b) => a.T.CompareTo(b.T));
            Invalidate();
            GradientChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragIndex >= 0)
            {
                _stops[_dragIndex].T = XToOffset(e.Location.X);
                // Drag away vertically to delete (matches the WPF behavior of
                // dragging a handle off the strip).
                var strip = GetStripRect();
                _dragOutOfBounds = e.Location.Y > strip.Y + strip.Height + HandleSize + DeleteDragDistance;
                Invalidate();
                GradientChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            int idx = HitHandle(e.Location);
            if (idx != _hoverIndex)
            {
                _hoverIndex = idx;
                Cursor = idx >= 0 ? Cursors.Pointer : Cursors.Default;
                Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_dragIndex < 0) return;
            if (_dragOutOfBounds && _stops.Count > 2)
            {
                _stops.RemoveAt(_dragIndex);
                GradientChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _stops.Sort((a, b) => a.T.CompareTo(b.T));
            }
            _dragIndex = -1;
            _dragOutOfBounds = false;
            Invalidate();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            int idx = HitHandle(e.Location);
            if (idx < 0) return;
            using var dlg = new ColorDialog();
            dlg.AllowAlpha = true;
            dlg.Color = ToEto(_stops[idx].Color);
            if (dlg.ShowDialog(this) == DialogResult.Ok)
            {
                _stops[idx].Color = FromEto(dlg.Color);
                Invalidate();
                GradientChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Linear-interpolate the current gradient at offset t for the
        /// "click to add a new stop" path — so the new stop visually
        /// matches the gradient where it was added.
        /// </summary>
        private System.Drawing.Color SampleAt(double t)
        {
            if (_stops.Count == 0) return System.Drawing.Color.Black;
            if (t <= _stops[0].T) return _stops[0].Color;
            if (t >= _stops[^1].T) return _stops[^1].Color;
            for (int i = 0; i < _stops.Count - 1; i++)
            {
                var a = _stops[i];
                var b = _stops[i + 1];
                if (t < a.T || t > b.T) continue;
                var f = (b.T - a.T) <= 0 ? 0.0 : (t - a.T) / (b.T - a.T);
                int Lerp(int x, int y) => (int)Math.Round(x + (y - x) * f);
                return System.Drawing.Color.FromArgb(
                    Lerp(a.Color.A, b.Color.A),
                    Lerp(a.Color.R, b.Color.R),
                    Lerp(a.Color.G, b.Color.G),
                    Lerp(a.Color.B, b.Color.B));
            }
            return _stops[^1].Color;
        }

        private sealed class Stop
        {
            public double T;
            public System.Drawing.Color Color;
        }
    }
}

namespace HumanUI.Components
{
    public class CreateGradientEditor_Component : GH_Component
    {
        public CreateGradientEditor_Component()
            : base("Create Gradient Editor", "Gradient",
                "Creates an editable gradient in the UI",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient(s)", "G", "The Gradient or collection of gradient presets to include.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddGenericParameter("Default Gradient", "D", "The default gradient to show — only used if the preset menu is enabled.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddBooleanParameter("Show Editor", "E", "Set to true to show an interactive gradient editor", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Presets", "P", "Set to true to show a pulldown menu of gradient presets", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient Editor", "GE", "The Gradient Editor Element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var gradientObjects = new List<object>();
            var defaultGradientObjects = new List<object>();
            bool showPresets = false;
            bool showEditor = true;
            DA.GetData("Show Presets", ref showPresets);
            DA.GetData("Show Editor", ref showEditor);
            DA.GetDataList("Gradient(s)", gradientObjects);
            DA.GetDataList("Default Gradient", defaultGradientObjects);
            bool hasDefault = Params.Input[1].SourceCount > 0;

            var ghGradients = new List<GH_Gradient>();
            ExtractGradients(ghGradients, gradientObjects);

            var presets = new List<HUI_Gradient>();
            if (hasDefault)
            {
                var defaultGradient = new List<GH_Gradient>();
                ExtractGradients(defaultGradient, defaultGradientObjects, 1);
                if (defaultGradient.Count > 0)
                    presets.Add(HUI_Gradient.FromGHGradient(defaultGradient[0]));
            }
            foreach (var gh in ghGradients)
            {
                var converted = HUI_Gradient.FromGHGradient(gh);
                if (presets.Any(p => p.ToString() == converted.ToString())) continue;
                presets.Add(converted);
            }

            var editor = new HUI_GradientEditor(showPresets, showEditor, presets);
            DA.SetData("Gradient Editor", new UIElement_Goo(editor, "Gradient Editor", InstanceGuid, DA.Iteration));
        }

        private void ExtractGradients(List<GH_Gradient> gradients, List<object> gradientObjects, int paramIndex = 0)
        {
            if (gradientObjects.Count > 0 && gradientObjects[0] is GH_ObjectWrapper)
            {
                foreach (GH_ObjectWrapper wrapper in gradientObjects.OfType<GH_ObjectWrapper>())
                {
                    if (wrapper.Value is GH_GradientControl editor) gradients.Add(editor.Gradient);
                }
            }
            else
            {
                var doc = OnPingDocument();
                if (doc == null) return;
                doc.Objects.OfType<GH_GradientControl>()
                    .Where(ao => Params.Input[paramIndex].DependsOn(ao))
                    .ToList()
                    .ForEach(g => gradients.Add(g.Gradient));
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GradientEditor;
        public override Guid ComponentGuid => new Guid("{DA8DD6A3-AD57-471E-B891-94902C8647ED}");
    }
}

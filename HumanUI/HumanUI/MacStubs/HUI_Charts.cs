// Mac implementation of the chart family — Create Chart, Create Multi Chart,
// Set Chart Contents, Set Multi Chart Contents, Chart Appearance. Windows
// keeps the MetroChart-backed WPF versions in Components/UI Graphs/; on Mac
// those source files are stripped from the compile and these Eto.Drawable
// implementations stand in. Parameter shapes / GUIDs / namespaces /
// subcategory ("UI Graphs + Charts") match Windows exactly so .gh files
// round-trip cleanly between platforms.
//
// Rendering scope: pie, doughnut, horizontal bar, vertical bar (column),
// and a simplified gauge for single-series. Clustered + stacked
// horizontal/vertical and pie (first series) for multi-series.

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

namespace HumanUI
{
    /// <summary>
    /// Mac chart drawable for single-series charts (Create Chart). Renders
    /// pie / doughnut / horizontal bar / vertical bar / gauge, palette-
    /// recolorable, with click selection. The shape is intentionally close
    /// to MetroChart's ChartBase surface — Title / SubTitle / chart-type /
    /// items / palette — so Set Chart Contents and Chart Appearance can
    /// mutate it the same way the Windows components mutate a ChartBase.
    /// </summary>
    public sealed class HUI_Chart : Drawable
    {
        public enum ChartKind { Pie = 0, HorizontalBar = 1, VerticalBar = 2, Doughnut = 3, Gauge = 4 }

        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public bool TitleVisible { get; set; } = true;
        public bool LegendVisible { get; set; } = true;
        public ChartKind Kind { get; set; } = ChartKind.Pie;
        public List<ChartItem> Items { get; } = new();
        public List<System.Drawing.Color> Palette { get; } = new(DefaultPalette);
        public string SelectedCategory { get; private set; }

        public event EventHandler SelectionChanged;

        private List<HitRegion> _hitRegions = new();

        public HUI_Chart()
        {
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            SizeChanged += (_, _) => Invalidate();
        }

        public void SetItems(IEnumerable<string> categories, IEnumerable<double> values)
        {
            Items.Clear();
            using var ce = categories.GetEnumerator();
            using var ve = values.GetEnumerator();
            while (ce.MoveNext() && ve.MoveNext())
                Items.Add(new ChartItem { Category = ce.Current ?? string.Empty, Value = ve.Current });
            Invalidate();
        }

        public void SetPalette(IEnumerable<System.Drawing.Color> colors)
        {
            Palette.Clear();
            Palette.AddRange(colors);
            if (Palette.Count == 0) Palette.AddRange(DefaultPalette);
            Invalidate();
        }

        private Color PaletteColor(int i) => ToEto(Palette[((i % Palette.Count) + Palette.Count) % Palette.Count]);

        private static readonly System.Drawing.Color[] DefaultPalette = new[]
        {
            System.Drawing.Color.FromArgb(0x41, 0x8C, 0xF0),
            System.Drawing.Color.FromArgb(0xFC, 0xB4, 0x41),
            System.Drawing.Color.FromArgb(0xDF, 0x3A, 0x02),
            System.Drawing.Color.FromArgb(0x05, 0x64, 0x92),
            System.Drawing.Color.FromArgb(0x65, 0x9D, 0x32),
            System.Drawing.Color.FromArgb(0xCB, 0x4B, 0x4B),
            System.Drawing.Color.FromArgb(0x4D, 0xA7, 0x4D),
            System.Drawing.Color.FromArgb(0x94, 0x40, 0xED),
        };

        private static Color ToEto(System.Drawing.Color c) => Color.FromArgb(c.R, c.G, c.B, c.A);

        private void OnPaint(object sender, PaintEventArgs e)
        {
            _hitRegions.Clear();
            var g = e.Graphics;
            g.AntiAlias = true;
            g.Clear(Colors.White);

            var bounds = new RectangleF(0, 0, Width, Height);
            var (titleRect, body) = SplitForTitle(bounds);
            DrawTitle(g, titleRect);

            if (Items.Count == 0)
            {
                using var f = SystemFonts.Default(10);
                g.DrawText(f, Colors.Gray, body.X + 6, body.Y + 6, "(no data)");
                return;
            }

            var (legendRect, plot) = LegendVisible && Kind != ChartKind.Gauge
                ? SplitForLegend(body)
                : (RectangleF.Empty, body);

            switch (Kind)
            {
                case ChartKind.Pie: PaintPieOrDoughnut(g, plot, 0f); break;
                case ChartKind.Doughnut: PaintPieOrDoughnut(g, plot, 0.55f); break;
                case ChartKind.HorizontalBar: PaintHorizontalBars(g, plot); break;
                case ChartKind.VerticalBar: PaintVerticalBars(g, plot); break;
                case ChartKind.Gauge: PaintGauge(g, body); break;
            }

            if (!legendRect.IsEmpty) DrawLegend(g, legendRect);
        }

        private (RectangleF title, RectangleF body) SplitForTitle(RectangleF bounds)
        {
            if (!TitleVisible || (string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(SubTitle)))
                return (RectangleF.Empty, bounds);
            float h = string.IsNullOrEmpty(SubTitle) ? 26f : 42f;
            return (new RectangleF(bounds.X, bounds.Y, bounds.Width, h),
                    new RectangleF(bounds.X, bounds.Y + h, bounds.Width, bounds.Height - h));
        }

        private void DrawTitle(Graphics g, RectangleF titleRect)
        {
            if (titleRect.IsEmpty) return;
            if (!string.IsNullOrEmpty(Title))
            {
                using var f = SystemFonts.Bold(14);
                var sz = g.MeasureString(f, Title);
                g.DrawText(f, Colors.Black, titleRect.X + (titleRect.Width - sz.Width) / 2f, titleRect.Y + 2, Title);
            }
            if (!string.IsNullOrEmpty(SubTitle))
            {
                using var f = SystemFonts.Default(10);
                var sz = g.MeasureString(f, SubTitle);
                g.DrawText(f, new Color(0.4f, 0.4f, 0.4f), titleRect.X + (titleRect.Width - sz.Width) / 2f, titleRect.Y + 22, SubTitle);
            }
        }

        private (RectangleF legend, RectangleF plot) SplitForLegend(RectangleF body)
        {
            // Legend on the right for pie/doughnut, on the bottom for bars/columns —
            // closer to MetroChart's default behavior.
            if (Kind == ChartKind.Pie || Kind == ChartKind.Doughnut)
            {
                float lw = Math.Min(140f, body.Width * 0.35f);
                if (body.Width - lw < 80) return (RectangleF.Empty, body);
                return (new RectangleF(body.X + body.Width - lw, body.Y, lw, body.Height),
                        new RectangleF(body.X, body.Y, body.Width - lw, body.Height));
            }
            float lh = Math.Min(28f, body.Height * 0.18f);
            if (body.Height - lh < 60) return (RectangleF.Empty, body);
            return (new RectangleF(body.X, body.Y + body.Height - lh, body.Width, lh),
                    new RectangleF(body.X, body.Y, body.Width, body.Height - lh));
        }

        private void DrawLegend(Graphics g, RectangleF legend)
        {
            using var f = SystemFonts.Default(9);
            if (Kind == ChartKind.Pie || Kind == ChartKind.Doughnut)
            {
                // Vertical legend: swatch + category text.
                float y = legend.Y + 4;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (y + 16 > legend.Y + legend.Height) break;
                    using var swatch = new SolidBrush(PaletteColor(i));
                    g.FillRectangle(swatch, new RectangleF(legend.X + 4, y + 2, 10, 10));
                    g.DrawText(f, Colors.Black, legend.X + 20, y, Items[i].Category ?? string.Empty);
                    y += 16;
                }
            }
            else
            {
                // Bars/columns: horizontal legend row of "value series" — only one
                // entry for single-series charts, but the swatch tracks the palette.
                using var swatch = new SolidBrush(PaletteColor(0));
                g.FillRectangle(swatch, new RectangleF(legend.X + 6, legend.Y + 8, 10, 10));
                var t = string.IsNullOrEmpty(Title) ? "Series 1" : Title;
                g.DrawText(f, Colors.Black, legend.X + 22, legend.Y + 6, t);
            }
        }

        private void PaintPieOrDoughnut(Graphics g, RectangleF plot, float innerFraction)
        {
            float pad = 8f;
            float diameter = Math.Min(plot.Width, plot.Height) - pad * 2;
            if (diameter <= 0) return;
            float cx = plot.X + plot.Width / 2f;
            float cy = plot.Y + plot.Height / 2f;
            float r = diameter / 2f;
            float innerR = r * innerFraction;

            double total = 0;
            foreach (var item in Items) total += Math.Max(0, item.Value);
            if (total <= 0) return;

            float start = -90f; // 12 o'clock
            for (int i = 0; i < Items.Count; i++)
            {
                float sweep = (float)(Math.Max(0, Items[i].Value) / total * 360.0);
                if (sweep <= 0) continue;
                var path = new GraphicsPath();
                if (innerR > 0.5f)
                {
                    var outerRect = new RectangleF(cx - r, cy - r, r * 2, r * 2);
                    var innerRect = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);
                    path.AddArc(outerRect, start, sweep);
                    path.AddArc(innerRect, start + sweep, -sweep);
                    path.CloseFigure();
                }
                else
                {
                    path.AddLines(new PointF(cx, cy));
                    var outerRect = new RectangleF(cx - r, cy - r, r * 2, r * 2);
                    path.AddArc(outerRect, start, sweep);
                    path.CloseFigure();
                }
                using var fill = new SolidBrush(PaletteColor(i));
                g.FillPath(fill, path);
                using var stroke = new Pen(Colors.White, 1.5f);
                g.DrawPath(stroke, path);
                _hitRegions.Add(new HitRegion { Kind = HitKind.Slice, Cx = cx, Cy = cy, R0 = innerR, R1 = r, A0 = start, A1 = start + sweep, Category = Items[i].Category });
                start += sweep;
            }
        }

        private void PaintHorizontalBars(Graphics g, RectangleF plot)
        {
            float pad = 8f;
            var area = new RectangleF(plot.X + 60, plot.Y + pad, plot.Width - 70, plot.Height - pad * 2);
            if (area.Width <= 0 || area.Height <= 0 || Items.Count == 0) return;

            double max = 0;
            foreach (var item in Items) max = Math.Max(max, Math.Abs(item.Value));
            if (max <= 0) max = 1;

            float rowH = area.Height / Items.Count;
            float barH = Math.Max(4, rowH * 0.7f);
            using var labelFont = SystemFonts.Default(9);
            using var axis = new Pen(new Color(0.7f, 0.7f, 0.7f), 1);
            g.DrawLine(axis, area.X, area.Y, area.X, area.Y + area.Height);

            for (int i = 0; i < Items.Count; i++)
            {
                float y = area.Y + i * rowH + (rowH - barH) / 2f;
                float w = (float)(Math.Abs(Items[i].Value) / max * area.Width);
                var barRect = new RectangleF(area.X, y, w, barH);
                using var fill = new SolidBrush(PaletteColor(i));
                g.FillRectangle(fill, barRect);
                _hitRegions.Add(new HitRegion { Kind = HitKind.Rect, Rect = barRect, Category = Items[i].Category });

                var cat = Items[i].Category ?? string.Empty;
                var sz = g.MeasureString(labelFont, cat);
                g.DrawText(labelFont, Colors.Black, area.X - 4 - sz.Width, y + (barH - sz.Height) / 2f, cat);

                var vs = Items[i].Value.ToString("0.##");
                g.DrawText(labelFont, Colors.Black, barRect.Right + 4, y + (barH - sz.Height) / 2f, vs);
            }
        }

        private void PaintVerticalBars(Graphics g, RectangleF plot)
        {
            float pad = 8f;
            var area = new RectangleF(plot.X + 30, plot.Y + pad, plot.Width - 40, plot.Height - pad * 2 - 18);
            if (area.Width <= 0 || area.Height <= 0 || Items.Count == 0) return;

            double max = 0;
            foreach (var item in Items) max = Math.Max(max, Math.Abs(item.Value));
            if (max <= 0) max = 1;

            float colW = area.Width / Items.Count;
            float barW = Math.Max(4, colW * 0.7f);
            using var labelFont = SystemFonts.Default(9);
            using var axis = new Pen(new Color(0.7f, 0.7f, 0.7f), 1);
            g.DrawLine(axis, area.X, area.Y + area.Height, area.X + area.Width, area.Y + area.Height);

            for (int i = 0; i < Items.Count; i++)
            {
                float h = (float)(Math.Abs(Items[i].Value) / max * area.Height);
                float x = area.X + i * colW + (colW - barW) / 2f;
                var barRect = new RectangleF(x, area.Y + area.Height - h, barW, h);
                using var fill = new SolidBrush(PaletteColor(i));
                g.FillRectangle(fill, barRect);
                _hitRegions.Add(new HitRegion { Kind = HitKind.Rect, Rect = barRect, Category = Items[i].Category });

                var cat = Items[i].Category ?? string.Empty;
                var sz = g.MeasureString(labelFont, cat);
                g.DrawText(labelFont, Colors.Black, x + (barW - sz.Width) / 2f, area.Y + area.Height + 2, cat);

                var vs = Items[i].Value.ToString("0.##");
                var vsz = g.MeasureString(labelFont, vs);
                g.DrawText(labelFont, Colors.Black, x + (barW - vsz.Width) / 2f, barRect.Y - vsz.Height - 1, vs);
            }
        }

        private void PaintGauge(Graphics g, RectangleF plot)
        {
            // Simplified: each item is rendered as a radial gauge slice of a
            // half-doughnut, value normalized to the max value across items.
            float pad = 8f;
            float diameter = Math.Min(plot.Width, plot.Height * 1.6f) - pad * 2;
            if (diameter <= 0) return;
            float r = diameter / 2f;
            float innerR = r * 0.55f;
            float cx = plot.X + plot.Width / 2f;
            float cy = plot.Y + plot.Height * 0.7f;

            double max = 0;
            foreach (var item in Items) max = Math.Max(max, Math.Abs(item.Value));
            if (max <= 0) max = 1;

            // Background half-ring.
            var outer = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            var inner = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);
            var ringPath = new GraphicsPath();
            ringPath.AddArc(outer, 180, 180);
            ringPath.AddArc(inner, 0, -180);
            ringPath.CloseFigure();
            using (var bg = new SolidBrush(new Color(0.92f, 0.92f, 0.92f))) g.FillPath(bg, ringPath);

            // Render only the first item as the "value" gauge — MetroChart's
            // RadialGaugeChart shows one needle/value per chart.
            if (Items.Count > 0)
            {
                float frac = (float)(Math.Abs(Items[0].Value) / max);
                if (frac < 0) frac = 0; if (frac > 1) frac = 1;
                float sweep = 180f * frac;
                var path = new GraphicsPath();
                path.AddArc(outer, 180, sweep);
                path.AddArc(inner, 180 + sweep, -sweep);
                path.CloseFigure();
                using var fill = new SolidBrush(PaletteColor(0));
                g.FillPath(fill, path);
                _hitRegions.Add(new HitRegion { Kind = HitKind.Slice, Cx = cx, Cy = cy, R0 = innerR, R1 = r, A0 = 180, A1 = 180 + sweep, Category = Items[0].Category });

                using var f = SystemFonts.Bold(14);
                var vs = Items[0].Value.ToString("0.##");
                var sz = g.MeasureString(f, vs);
                g.DrawText(f, Colors.Black, cx - sz.Width / 2f, cy - sz.Height - 4, vs);
                if (!string.IsNullOrEmpty(Items[0].Category))
                {
                    using var fs = SystemFonts.Default(10);
                    var csz = g.MeasureString(fs, Items[0].Category);
                    g.DrawText(fs, new Color(0.4f, 0.4f, 0.4f), cx - csz.Width / 2f, cy + 4, Items[0].Category);
                }
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Primary) return;
            string hit = HitTest(e.Location);
            if (hit == null) return;
            SelectedCategory = hit;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private string HitTest(PointF p)
        {
            // Iterate in reverse so foreground regions win — matters when bar
            // labels overlap the axis hit region (not currently an issue but
            // makes the search robust if regions accumulate later).
            for (int i = _hitRegions.Count - 1; i >= 0; i--)
            {
                var r = _hitRegions[i];
                if (r.Kind == HitKind.Rect && r.Rect.Contains(p)) return r.Category;
                if (r.Kind == HitKind.Slice)
                {
                    var dx = p.X - r.Cx;
                    var dy = p.Y - r.Cy;
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < r.R0 || dist > r.R1) continue;
                    var ang = NormalizeDegrees((float)(Math.Atan2(dy, dx) * 180.0 / Math.PI));
                    var a0 = NormalizeDegrees(r.A0);
                    var a1 = NormalizeDegrees(r.A1);
                    if (a0 <= a1)
                    {
                        if (ang >= a0 && ang <= a1) return r.Category;
                    }
                    else
                    {
                        if (ang >= a0 || ang <= a1) return r.Category;
                    }
                }
            }
            return null;
        }

        private static float NormalizeDegrees(float a)
        {
            while (a < 0) a += 360;
            while (a >= 360) a -= 360;
            return a;
        }

        public sealed class ChartItem
        {
            public string Category { get; set; }
            public double Value { get; set; }
        }

        private enum HitKind { Rect, Slice }
        private struct HitRegion
        {
            public HitKind Kind;
            public RectangleF Rect;
            public string Category;
            public float Cx, Cy, R0, R1, A0, A1;
        }
    }

    /// <summary>
    /// Multi-series chart drawable. Handles clustered/stacked horizontal &
    /// vertical bars, plus a pie/doughnut that flattens to the first series.
    /// The shape mirrors HUI_Chart (Title/SubTitle/Kind/Palette/Series) so
    /// SetMultiChart can mutate it cleanly.
    /// </summary>
    public sealed class HUI_MultiChart : Drawable
    {
        public enum MultiKind
        {
            HorizontalClusteredBar = 0,
            VerticalClusteredBar = 1,
            HorizontalStackedBar = 2,
            VerticalStackedBar = 3,
            Pie = 4,
        }

        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public bool TitleVisible { get; set; } = true;
        public bool LegendVisible { get; set; } = true;
        public MultiKind Kind { get; set; } = MultiKind.HorizontalClusteredBar;

        /// <summary>Category labels across the horizontal/category axis.</summary>
        public List<string> Names { get; } = new();

        /// <summary>One title per series (used for legend + tooltip).</summary>
        public List<string> ClusterTitles { get; } = new();

        /// <summary>Outer list = series, inner list = value per category.</summary>
        public List<List<double>> Series { get; } = new();

        public List<System.Drawing.Color> Palette { get; } = new(HUI_Chart_Palette.Default);

        public string SelectedCategory { get; private set; }
        public string SelectedSeries { get; private set; }

        public event EventHandler SelectionChanged;

        private List<HitRegion> _hitRegions = new();

        public HUI_MultiChart()
        {
            Paint += OnPaint;
            MouseDown += OnMouseDown;
            SizeChanged += (_, _) => Invalidate();
        }

        public void SetPalette(IEnumerable<System.Drawing.Color> colors)
        {
            Palette.Clear();
            Palette.AddRange(colors);
            if (Palette.Count == 0) Palette.AddRange(HUI_Chart_Palette.Default);
            Invalidate();
        }

        private Color PaletteColor(int i)
        {
            var c = Palette[((i % Palette.Count) + Palette.Count) % Palette.Count];
            return Color.FromArgb(c.R, c.G, c.B, c.A);
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            _hitRegions.Clear();
            var g = e.Graphics;
            g.AntiAlias = true;
            g.Clear(Colors.White);

            var bounds = new RectangleF(0, 0, Width, Height);
            var (titleRect, body) = SplitForTitle(bounds);
            DrawTitle(g, titleRect);

            if (Series.Count == 0 || Names.Count == 0)
            {
                using var f = SystemFonts.Default(10);
                g.DrawText(f, Colors.Gray, body.X + 6, body.Y + 6, "(no data)");
                return;
            }

            var (legendRect, plot) = LegendVisible ? SplitForLegend(body) : (RectangleF.Empty, body);

            switch (Kind)
            {
                case MultiKind.HorizontalClusteredBar: PaintHorizontal(g, plot, stacked: false); break;
                case MultiKind.VerticalClusteredBar: PaintVertical(g, plot, stacked: false); break;
                case MultiKind.HorizontalStackedBar: PaintHorizontal(g, plot, stacked: true); break;
                case MultiKind.VerticalStackedBar: PaintVertical(g, plot, stacked: true); break;
                case MultiKind.Pie: PaintPie(g, plot); break;
            }

            if (!legendRect.IsEmpty) DrawLegend(g, legendRect);
        }

        private (RectangleF, RectangleF) SplitForTitle(RectangleF bounds)
        {
            if (!TitleVisible || (string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(SubTitle)))
                return (RectangleF.Empty, bounds);
            float h = string.IsNullOrEmpty(SubTitle) ? 26f : 42f;
            return (new RectangleF(bounds.X, bounds.Y, bounds.Width, h),
                    new RectangleF(bounds.X, bounds.Y + h, bounds.Width, bounds.Height - h));
        }

        private void DrawTitle(Graphics g, RectangleF titleRect)
        {
            if (titleRect.IsEmpty) return;
            if (!string.IsNullOrEmpty(Title))
            {
                using var f = SystemFonts.Bold(14);
                var sz = g.MeasureString(f, Title);
                g.DrawText(f, Colors.Black, titleRect.X + (titleRect.Width - sz.Width) / 2f, titleRect.Y + 2, Title);
            }
            if (!string.IsNullOrEmpty(SubTitle))
            {
                using var f = SystemFonts.Default(10);
                var sz = g.MeasureString(f, SubTitle);
                g.DrawText(f, new Color(0.4f, 0.4f, 0.4f), titleRect.X + (titleRect.Width - sz.Width) / 2f, titleRect.Y + 22, SubTitle);
            }
        }

        private (RectangleF, RectangleF) SplitForLegend(RectangleF body)
        {
            float lw = Math.Min(140f, body.Width * 0.30f);
            if (body.Width - lw < 100) return (RectangleF.Empty, body);
            return (new RectangleF(body.X + body.Width - lw, body.Y, lw, body.Height),
                    new RectangleF(body.X, body.Y, body.Width - lw, body.Height));
        }

        private void DrawLegend(Graphics g, RectangleF legend)
        {
            using var f = SystemFonts.Default(9);
            float y = legend.Y + 4;
            int seriesCount = Series.Count;
            for (int i = 0; i < seriesCount; i++)
            {
                if (y + 16 > legend.Y + legend.Height) break;
                using var swatch = new SolidBrush(PaletteColor(i));
                g.FillRectangle(swatch, new RectangleF(legend.X + 4, y + 2, 10, 10));
                var label = i < ClusterTitles.Count && !string.IsNullOrEmpty(ClusterTitles[i])
                    ? ClusterTitles[i]
                    : $"Series {i + 1}";
                g.DrawText(f, Colors.Black, legend.X + 20, y, label);
                y += 16;
            }
        }

        private double MaxBarValue(bool stacked)
        {
            double max = 0;
            for (int c = 0; c < Names.Count; c++)
            {
                if (stacked)
                {
                    double sum = 0;
                    foreach (var s in Series) if (c < s.Count) sum += Math.Max(0, s[c]);
                    if (sum > max) max = sum;
                }
                else
                {
                    foreach (var s in Series)
                    {
                        if (c >= s.Count) continue;
                        var v = Math.Abs(s[c]);
                        if (v > max) max = v;
                    }
                }
            }
            return max > 0 ? max : 1;
        }

        private void PaintHorizontal(Graphics g, RectangleF plot, bool stacked)
        {
            float pad = 8f;
            var area = new RectangleF(plot.X + 60, plot.Y + pad, plot.Width - 70, plot.Height - pad * 2);
            if (area.Width <= 0 || area.Height <= 0) return;

            double max = MaxBarValue(stacked);
            int catCount = Names.Count;
            int seriesCount = Series.Count;
            float rowH = area.Height / catCount;
            using var labelFont = SystemFonts.Default(9);
            using var axis = new Pen(new Color(0.7f, 0.7f, 0.7f), 1);
            g.DrawLine(axis, area.X, area.Y, area.X, area.Y + area.Height);

            for (int c = 0; c < catCount; c++)
            {
                float y = area.Y + c * rowH;
                if (stacked)
                {
                    float barH = Math.Max(4, rowH * 0.7f);
                    float yc = y + (rowH - barH) / 2f;
                    float x = area.X;
                    for (int s = 0; s < seriesCount; s++)
                    {
                        if (c >= Series[s].Count) continue;
                        float w = (float)(Math.Max(0, Series[s][c]) / max * area.Width);
                        if (w <= 0) continue;
                        var rect = new RectangleF(x, yc, w, barH);
                        using var fill = new SolidBrush(PaletteColor(s));
                        g.FillRectangle(fill, rect);
                        _hitRegions.Add(new HitRegion { Rect = rect, Category = Names[c], Series = SeriesLabel(s) });
                        x += w;
                    }
                }
                else
                {
                    float barH = Math.Max(2, (rowH * 0.85f) / seriesCount);
                    float yc = y + (rowH - barH * seriesCount) / 2f;
                    for (int s = 0; s < seriesCount; s++)
                    {
                        if (c >= Series[s].Count) continue;
                        float w = (float)(Math.Abs(Series[s][c]) / max * area.Width);
                        var rect = new RectangleF(area.X, yc + s * barH, w, barH);
                        using var fill = new SolidBrush(PaletteColor(s));
                        g.FillRectangle(fill, rect);
                        _hitRegions.Add(new HitRegion { Rect = rect, Category = Names[c], Series = SeriesLabel(s) });
                    }
                }

                var name = Names[c] ?? string.Empty;
                var sz = g.MeasureString(labelFont, name);
                g.DrawText(labelFont, Colors.Black, area.X - 4 - sz.Width, y + (rowH - sz.Height) / 2f, name);
            }
        }

        private void PaintVertical(Graphics g, RectangleF plot, bool stacked)
        {
            float pad = 8f;
            var area = new RectangleF(plot.X + 30, plot.Y + pad, plot.Width - 40, plot.Height - pad * 2 - 18);
            if (area.Width <= 0 || area.Height <= 0) return;

            double max = MaxBarValue(stacked);
            int catCount = Names.Count;
            int seriesCount = Series.Count;
            float colW = area.Width / catCount;
            using var labelFont = SystemFonts.Default(9);
            using var axis = new Pen(new Color(0.7f, 0.7f, 0.7f), 1);
            g.DrawLine(axis, area.X, area.Y + area.Height, area.X + area.Width, area.Y + area.Height);

            for (int c = 0; c < catCount; c++)
            {
                float x = area.X + c * colW;
                if (stacked)
                {
                    float barW = Math.Max(4, colW * 0.7f);
                    float xc = x + (colW - barW) / 2f;
                    float yBottom = area.Y + area.Height;
                    for (int s = 0; s < seriesCount; s++)
                    {
                        if (c >= Series[s].Count) continue;
                        float h = (float)(Math.Max(0, Series[s][c]) / max * area.Height);
                        if (h <= 0) continue;
                        var rect = new RectangleF(xc, yBottom - h, barW, h);
                        using var fill = new SolidBrush(PaletteColor(s));
                        g.FillRectangle(fill, rect);
                        _hitRegions.Add(new HitRegion { Rect = rect, Category = Names[c], Series = SeriesLabel(s) });
                        yBottom -= h;
                    }
                }
                else
                {
                    float barW = Math.Max(2, (colW * 0.85f) / seriesCount);
                    float xc = x + (colW - barW * seriesCount) / 2f;
                    for (int s = 0; s < seriesCount; s++)
                    {
                        if (c >= Series[s].Count) continue;
                        float h = (float)(Math.Abs(Series[s][c]) / max * area.Height);
                        var rect = new RectangleF(xc + s * barW, area.Y + area.Height - h, barW, h);
                        using var fill = new SolidBrush(PaletteColor(s));
                        g.FillRectangle(fill, rect);
                        _hitRegions.Add(new HitRegion { Rect = rect, Category = Names[c], Series = SeriesLabel(s) });
                    }
                }

                var name = Names[c] ?? string.Empty;
                var sz = g.MeasureString(labelFont, name);
                g.DrawText(labelFont, Colors.Black, x + (colW - sz.Width) / 2f, area.Y + area.Height + 2, name);
            }
        }

        private void PaintPie(Graphics g, RectangleF plot)
        {
            // Multi-chart pie = first series only (matches the Windows
            // behavior where Series[0] is the data context for the pie).
            if (Series.Count == 0) return;
            var s = Series[0];
            double total = 0;
            for (int i = 0; i < s.Count && i < Names.Count; i++) total += Math.Max(0, s[i]);
            if (total <= 0) return;

            float pad = 8f;
            float diameter = Math.Min(plot.Width, plot.Height) - pad * 2;
            if (diameter <= 0) return;
            float cx = plot.X + plot.Width / 2f;
            float cy = plot.Y + plot.Height / 2f;
            float r = diameter / 2f;

            float start = -90f;
            for (int i = 0; i < s.Count && i < Names.Count; i++)
            {
                float sweep = (float)(Math.Max(0, s[i]) / total * 360.0);
                if (sweep <= 0) continue;
                var path = new GraphicsPath();
                path.AddLines(new PointF(cx, cy));
                path.AddArc(new RectangleF(cx - r, cy - r, r * 2, r * 2), start, sweep);
                path.CloseFigure();
                using var fill = new SolidBrush(PaletteColor(i));
                g.FillPath(fill, path);
                using var stroke = new Pen(Colors.White, 1.5f);
                g.DrawPath(stroke, path);
                _hitRegions.Add(new HitRegion { Rect = RectangleF.Empty, Category = Names[i], Series = SeriesLabel(0) });
                start += sweep;
            }
        }

        private string SeriesLabel(int i) => i < ClusterTitles.Count && !string.IsNullOrEmpty(ClusterTitles[i])
            ? ClusterTitles[i]
            : $"Series {i + 1}";

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Buttons != MouseButtons.Primary) return;
            foreach (var r in _hitRegions)
            {
                if (r.Rect.IsEmpty || !r.Rect.Contains(e.Location)) continue;
                SelectedCategory = r.Category;
                SelectedSeries = r.Series;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                return;
            }
        }

        private struct HitRegion
        {
            public RectangleF Rect;
            public string Category;
            public string Series;
        }
    }

    /// <summary>Shared default palette used by both HUI_Chart and HUI_MultiChart.</summary>
    internal static class HUI_Chart_Palette
    {
        public static readonly System.Drawing.Color[] Default = new[]
        {
            System.Drawing.Color.FromArgb(0x41, 0x8C, 0xF0),
            System.Drawing.Color.FromArgb(0xFC, 0xB4, 0x41),
            System.Drawing.Color.FromArgb(0xDF, 0x3A, 0x02),
            System.Drawing.Color.FromArgb(0x05, 0x64, 0x92),
            System.Drawing.Color.FromArgb(0x65, 0x9D, 0x32),
            System.Drawing.Color.FromArgb(0xCB, 0x4B, 0x4B),
            System.Drawing.Color.FromArgb(0x4D, 0xA7, 0x4D),
            System.Drawing.Color.FromArgb(0x94, 0x40, 0xED),
        };
    }
}

namespace HumanUI.Components
{
    public class CreateChart_Component : GH_Component
    {
        public CreateChart_Component()
            : base("Create Chart", " Chart",
                "Creates a Chart from Data and Categories.",
                "Human UI", "UI Graphs + Charts")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Data", "D", "The list of values to be charted.", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "The names of the data items to be charted", GH_ParamAccess.list);
            pManager.AddTextParameter("Title", "T", "The title of the Chart", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddTextParameter("SubTitle", "sT", "The subtitle of the Chart", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddIntegerParameter("Chart Type", "CT", "The type of Chart to create", GH_ParamAccess.item, 0);
            var chartTypes = (Param_Integer)pManager[4];
            chartTypes.AddNamedValue("Pie Chart", 0);
            chartTypes.AddNamedValue("Horizontal Bar Chart", 1);
            chartTypes.AddNamedValue("Vertical Bar Chart", 2);
            chartTypes.AddNamedValue("Doughnut Chart", 3);
            chartTypes.AddNamedValue("Gauge Chart", 4);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart", "C", "The Chart object", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var values = new List<double>();
            var names = new List<string>();
            string title = "";
            string subTitle = "";
            int chartType = 0;

            DA.GetDataList("Data", values);
            DA.GetDataList("Names", names);
            bool hasTitle = DA.GetData("Title", ref title);
            DA.GetData("SubTitle", ref subTitle);
            DA.GetData("Chart Type", ref chartType);

            var chart = new HUI_Chart
            {
                Title = title ?? string.Empty,
                SubTitle = subTitle ?? string.Empty,
                TitleVisible = hasTitle,
                Kind = (HUI_Chart.ChartKind)Math.Clamp(chartType, 0, 4),
                Width = 320,
                Height = 240,
            };
            chart.SetItems(names, values);
            DA.SetData("Chart", new UIElement_Goo(chart, "Chart Elem", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateChart;
        public override Guid ComponentGuid => new Guid("{1A96F054-26DD-45C6-B09D-2760B496BB0A}");
    }

    public class CreateMultiChart_Component : GH_Component
    {
        public CreateMultiChart_Component()
            : base("Create Multi Chart", "MultiChart",
                "Creates a Multi Chart from sets of Data and Categories.",
                "Human UI", "UI Graphs + Charts")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Data", "D", "The list of values to be charted.", GH_ParamAccess.tree);
            pManager.AddTextParameter("Names", "N", "The names of the data items to be charted", GH_ParamAccess.list);
            pManager.AddTextParameter("Title", "T", "The title of the chart", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddTextParameter("SubTitle", "sT", "The subtitle of the chart", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddTextParameter("ClusterTitle", "cT", "The title of each chart cluster", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Chart Type", "CT", "The type of chart to create", GH_ParamAccess.item, 0);
            var chartTypes = (Param_Integer)pManager[5];
            chartTypes.AddNamedValue("Horizontal Cluster Bar Chart", 0);
            chartTypes.AddNamedValue("Vertical Cluster Bar Chart", 1);
            chartTypes.AddNamedValue("Horizontal Stacked Bar Chart", 2);
            chartTypes.AddNamedValue("Vertical Stacked Bar Chart", 3);
            chartTypes.AddNamedValue("Pie Chart", 4);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MultiChart", "MC", "The MultiChart object", GH_ParamAccess.list);
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> tree;
            var names = new List<string>();
            var clusterTitles = new List<string>();
            string title = "";
            string subTitle = "";
            int chartType = 0;

            bool hasTitle = DA.GetData("Title", ref title);
            DA.GetData("SubTitle", ref subTitle);
            DA.GetDataList("ClusterTitle", clusterTitles);
            DA.GetDataTree("Data", out tree);
            DA.GetDataList("Names", names);
            DA.GetData("Chart Type", ref chartType);

            var chart = new HUI_MultiChart
            {
                Title = title ?? string.Empty,
                SubTitle = subTitle ?? string.Empty,
                TitleVisible = hasTitle,
                Kind = (HUI_MultiChart.MultiKind)Math.Clamp(chartType, 0, 4),
                Width = 360,
                Height = 240,
            };
            chart.Names.AddRange(names);
            chart.ClusterTitles.AddRange(clusterTitles);
            foreach (var branch in tree.Branches)
            {
                var s = new List<double>();
                foreach (var n in branch) s.Add(n?.Value ?? 0);
                chart.Series.Add(s);
            }
            DA.SetData("MultiChart", new UIElement_Goo(chart, "Chart Elem", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateMultiChart;
        public override Guid ComponentGuid => new Guid("{66FA84E1-D224-4B4B-8DA4-E3E40CA815D5}");
    }

    public class SetChart_LiveUpdate : GH_Component
    {
        public SetChart_LiveUpdate()
            : base("Set Chart Contents", "SetChart",
                "Use this to set the contents of a Chart",
                "Human UI", "UI Graphs + Charts")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart to modify", "C", "The Chart object to modify", GH_ParamAccess.item);
            pManager.AddNumberParameter("New Chart Values", "V", "The new values to include in the Chart", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddTextParameter("New Chart Names", "N", "The names of the data items to be charted", GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddTextParameter("Title", "T", "The title of the Chart", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddTextParameter("SubTitle", "sT", "The subtitle of the Chart", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object chartObject = null;
            var values = new List<double>();
            var names = new List<string>();
            string title = null;
            string subTitle = null;

            if (!DA.GetData("Chart to modify", ref chartObject)) return;
            var chart = HUI_Util.GetUIElement<HUI_Chart>(chartObject);
            if (chart == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input is not a single-series Chart.");
                return;
            }

            if (DA.GetData("Title", ref title))
            {
                chart.TitleVisible = true;
                chart.Title = title;
            }
            if (DA.GetData("SubTitle", ref subTitle)) chart.SubTitle = subTitle;

            bool valuesSupplied = DA.GetDataList("New Chart Values", values);
            bool namesSupplied = DA.GetDataList("New Chart Names", names);
            if (valuesSupplied && namesSupplied && values.Count != names.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Different number of names and values supplied.");
                return;
            }

            // Mirror the Windows blend: replace existing in place, append /
            // truncate to match the new length.
            if (valuesSupplied)
            {
                for (int i = 0; i < chart.Items.Count && i < values.Count; i++) chart.Items[i].Value = values[i];
                for (int i = chart.Items.Count; i < values.Count; i++)
                    chart.Items.Add(new HUI_Chart.ChartItem { Category = "UNSET", Value = values[i] });
                if (values.Count < chart.Items.Count) chart.Items.RemoveRange(values.Count, chart.Items.Count - values.Count);
            }
            if (namesSupplied)
            {
                for (int i = 0; i < chart.Items.Count && i < names.Count; i++) chart.Items[i].Category = names[i];
                for (int i = chart.Items.Count; i < names.Count; i++)
                    chart.Items.Add(new HUI_Chart.ChartItem { Category = names[i], Value = double.NaN });
                if (names.Count < chart.Items.Count) chart.Items.RemoveRange(names.Count, chart.Items.Count - names.Count);
            }
            chart.Invalidate();
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetChart;
        public override Guid ComponentGuid => new Guid("{1C4AC2EC-3090-4D03-8D17-923417129692}");
    }

    public class SetMultiChart_Component : GH_Component
    {
        public SetMultiChart_Component()
            : base("Set Multi Chart Contents", "SetMultiChart",
                "Use this to set the contents of a MultiChart",
                "Human UI", "UI Graphs + Charts")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart to modify", "G", "The Multi Chart object to modify", GH_ParamAccess.item);
            pManager.AddNumberParameter("New Chart Values", "D", "The new values to include in the Multi Chart", GH_ParamAccess.tree);
            pManager.AddTextParameter("New Chart Names", "N", "The names of the data items to be Charted", GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddTextParameter("Title", "T", "The title of the Chart", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddTextParameter("SubTitle", "sT", "The subtitle of the Chart", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddTextParameter("ClusterTitle", "cT", "The title of each bar cluster", GH_ParamAccess.list);
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object chartObject = null;
            GH_Structure<GH_Number> tree;
            var names = new List<string>();
            var clusterTitles = new List<string>();
            string title = "";
            string subTitle = "";

            if (!DA.GetData("Chart to modify", ref chartObject)) return;
            var chart = HUI_Util.GetUIElement<HUI_MultiChart>(chartObject);
            if (chart == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input is not a Multi Chart.");
                return;
            }

            if (DA.GetData("Title", ref title))
            {
                chart.TitleVisible = true;
                chart.Title = title;
            }
            if (DA.GetData("SubTitle", ref subTitle)) chart.SubTitle = subTitle;

            bool clusterSupplied = DA.GetDataList("ClusterTitle", clusterTitles);
            bool valuesSupplied = DA.GetDataTree("New Chart Values", out tree);
            bool namesSupplied = DA.GetDataList("New Chart Names", names);

            if (clusterSupplied)
            {
                chart.ClusterTitles.Clear();
                chart.ClusterTitles.AddRange(clusterTitles);
            }
            if (namesSupplied)
            {
                chart.Names.Clear();
                chart.Names.AddRange(names);
            }
            if (valuesSupplied)
            {
                chart.Series.Clear();
                foreach (var branch in tree.Branches)
                {
                    var s = new List<double>();
                    foreach (var n in branch) s.Add(n?.Value ?? 0);
                    chart.Series.Add(s);
                }
            }
            chart.Invalidate();
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetMultiChart;
        public override Guid ComponentGuid => new Guid("{12A5A354-FC1B-4CEA-394A-BBEB71A23DB5}");
    }

    public class SetChartAppearance_Component : GH_Component
    {
        public SetChartAppearance_Component()
            : base("Chart Appearance", "ChartAppearance",
                "Use this to set the appearance of a Chart",
                "Human UI", "UI Graphs + Charts")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart to modify", "C", "The Chart object to modify", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "Co", "The Chart colors", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddBooleanParameter("Legend", "L", "Legend on?", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddBooleanParameter("Title", "T", "Title on?", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object chartObject = null;
            var colors = new List<System.Drawing.Color>();
            bool legend = true;
            bool titleVisible = true;

            if (!DA.GetData("Chart to modify", ref chartObject)) return;
            bool hasLegend = DA.GetData("Legend", ref legend);
            bool hasTitle = DA.GetData("Title", ref titleVisible);
            bool hasColors = DA.GetDataList("Colors", colors);

            var single = HUI_Util.GetUIElement<HUI_Chart>(chartObject);
            var multi = HUI_Util.GetUIElement<HUI_MultiChart>(chartObject);
            if (single == null && multi == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input is not a Chart.");
                return;
            }

            if (hasColors && colors.Count > 0)
            {
                single?.SetPalette(colors);
                multi?.SetPalette(colors);
            }
            if (hasLegend)
            {
                if (single != null) single.LegendVisible = legend;
                if (multi != null) multi.LegendVisible = legend;
            }
            if (hasTitle)
            {
                if (single != null) single.TitleVisible = titleVisible;
                if (multi != null) multi.TitleVisible = titleVisible;
            }
            single?.Invalidate();
            multi?.Invalidate();
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ChartAppearance;
        public override Guid ComponentGuid => new Guid("{12A5A354-FC1B-4CEA-894A-BBEB99A23DB5}");
    }
}

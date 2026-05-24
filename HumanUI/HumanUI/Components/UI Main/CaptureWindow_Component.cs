using System;
using System.IO;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Save a HUI window or individual element to a PNG. Implementation
    /// uses System.Drawing screen capture under HUI_WINDOWS — much simpler
    /// than the original WPF VisualBrush + RenderTargetBitmap path, and it
    /// captures whatever pixels are actually on screen (so user theming
    /// and per-platform native controls are preserved). The Mac path is a
    /// warn-and-skip stub for now; a proper Mac implementation needs
    /// Cocoa NSBitmapImageRep interop and hasn't been done yet.
    /// </summary>
    public class CaptureWindow_Component : GH_Component
    {
        public CaptureWindow_Component()
            : base("Capture Window or Element to File", "Capture",
                "Capture a HUI Window or individual element to an image",
                "Human UI", "UI Main")
        { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Window or Element", "W", "The Window to Capture", GH_ParamAccess.item);
            pManager.AddTextParameter("File Path", "F", "The file path where the image should be saved", GH_ParamAccess.item);
            pManager.AddNumberParameter("Scale Factor", "S", "A scale factor applied to the size of the window or element for hi-resolution capture", GH_ParamAccess.item, 1.0);
            pManager.AddBooleanParameter("Run", "R", "Set to true to activate the capture.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Capture Entire Window", "EW",
                "If a window is supplied, capture the entire window contents inside the scrollable area. Title bar will not show. This parameter is ignored if supplying a specific element.",
                GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object o = null;
            string filePath = string.Empty;
            double scaleFactor = 1.0;
            bool run = false;
            bool entireWindow = false;

            if (!DA.GetData("Window or Element", ref o)) return;
            if (!DA.GetData("File Path", ref filePath)) return;
            if (!DA.GetData("Scale Factor", ref scaleFactor)) return;
            if (!DA.GetData("Run", ref run)) return;
            if (!DA.GetData("Capture Entire Window", ref entireWindow)) return;

            if (!run) return;

            // Resolve the input to either a MainWindow or a Control. Same
            // disambiguation pattern as the WPF version — element flows in
            // wrapped in UIElement_Goo, window in GH_ObjectWrapper.
            MainWindow window = null;
            Control element = null;
            switch (o)
            {
                case UIElement_Goo goo: element = goo.element; break;
                case GH_ObjectWrapper wrapper when wrapper.Value is MainWindow mw: window = mw; break;
                case MainWindow mw: window = mw; break;
                case Control c: element = c; break;
            }

            if (window == null && element == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There was a problem processing this element/Window");
                return;
            }

#if HUI_WINDOWS
            Eto.Drawing.Point origin;
            Eto.Drawing.Size size;
            if (window != null)
            {
                if (entireWindow)
                {
                    // Capture the entire ClientSize, no title-bar chrome.
                    origin = ToPoint(window.PointToScreen(Eto.Drawing.PointF.Empty));
                    size = window.ClientSize;
                }
                else
                {
                    origin = window.Location;
                    size = window.Size;
                }
            }
            else
            {
                origin = ToPoint(element.PointToScreen(Eto.Drawing.PointF.Empty));
                size = element.Size;
            }

            if (size.Width <= 0 || size.Height <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Element has zero or negative dimensions — nothing to capture.");
                return;
            }

            int captureW = Math.Max(1, (int)Math.Round(size.Width * scaleFactor));
            int captureH = Math.Max(1, (int)Math.Round(size.Height * scaleFactor));

            using var raw = new System.Drawing.Bitmap(size.Width, size.Height);
            using (var g = System.Drawing.Graphics.FromImage(raw))
            {
                g.CopyFromScreen(origin.X, origin.Y, 0, 0, new System.Drawing.Size(size.Width, size.Height));
            }

            if (Math.Abs(scaleFactor - 1.0) > 1e-6)
            {
                using var scaled = new System.Drawing.Bitmap(captureW, captureH);
                using (var g = System.Drawing.Graphics.FromImage(scaled))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(raw, 0, 0, captureW, captureH);
                }
                EnsureDir(filePath);
                scaled.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
            else
            {
                EnsureDir(filePath);
                raw.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
#else
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Capture Window is not yet supported on Mac. The Windows path uses System.Drawing.Graphics.CopyFromScreen; a Mac port needs Cocoa NSBitmapImageRep interop.");
#endif
        }

        private static Eto.Drawing.Point ToPoint(Eto.Drawing.PointF p)
            => new Eto.Drawing.Point((int)Math.Round(p.X), (int)Math.Round(p.Y));

        private static void EnsureDir(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.captureWindow;
        public override Guid ComponentGuid => new Guid("{900FCBA9-1B83-403E-B909-9293146469D8}");
    }
}

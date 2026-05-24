using System;
using System.Collections.Generic;
#if HUI_WINDOWS
using System.Windows.Forms;
using Grasshopper.Kernel.Special;
#endif
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using HumanUIBaseApp;
using Rhino.Geometry;

namespace HumanUI.Components
{
    /// <summary>
    /// Mutates display properties on a HUI MainWindow: starting location,
    /// title-bar visibility, background color, scale factor. The WPF
    /// version's MahApps theme switch (Light/Dark + accent color) is no
    /// longer portable — Eto.Forms has no theming concept — so those
    /// inputs are accepted but only the legacy accent-list context menu
    /// item is kept for backward compatibility; the theme/accent values
    /// emit a warning instead of changing anything.
    /// </summary>
    public class SetWindowProperties_Component : GH_Component
    {
        // Historical MahApps accent list — kept for the "Create Accent List"
        // value-list shortcut so old .gh files reading from it still work.
        private static readonly string[] ACCENT_COLORS = new[]
        {
            "Amber", "Blue", "Brown", "Cobalt", "Crimson", "Cyan", "Emerald", "Green",
            "Indigo", "Lime", "Magenta", "Mauve", "Olive", "Orange", "Pink", "Purple",
            "Red", "Sienna", "Steel", "Taupe", "Teal", "Violet", "Yellow"
        };

        public SetWindowProperties_Component()
            : base("Set Window Properties", "WinProps",
                "Modify various properties of a Window.",
                "Human UI", "UI Main")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Window", "W", "The window to modify", GH_ParamAccess.item);
            pManager.AddPointParameter("Starting Location", "L", "The point (screen coordinates, so 0,0 is upper left) \nat which to locate the window's upper left corner.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddIntegerParameter("Theme", "T", "The base theme for the window. (Eto port: not supported — input is accepted for .gh-file compatibility but ignored with a warning.)", GH_ParamAccess.item);
            pManager[2].Optional = true;
            var themeParam = (Param_Integer)pManager[2];
            themeParam.AddNamedValue("Light", 0);
            themeParam.AddNamedValue("Dark", 1);
            pManager.AddTextParameter("Accent Color", "A", "The color accent for the window. (Eto port: not supported.)", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddBooleanParameter("Show Title Bar", "TB", "Set to false to hide the window's title bar.", GH_ParamAccess.item, true);
            pManager[4].Optional = true;
            pManager.AddColourParameter("Background Color", "BG", "Set the background color of the window.", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddNumberParameter("Scale Factor", "SF", "An absolute scaling factor used to modify the size of the window. (Eto port: applied as a ClientSize multiplier on each solve.)", GH_ParamAccess.item, 1.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            MainWindow mw = null;
            if (!DA.GetData("Window", ref mw)) return;

            Point3d startLoc = Point3d.Unset;
            if (DA.GetData("Starting Location", ref startLoc))
            {
                mw.Location = new Eto.Drawing.Point((int)startLoc.X, (int)startLoc.Y);
            }

            int theme = 0;
            if (DA.GetData("Theme", ref theme))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Theme switching (Light/Dark via MahApps) is not supported in the Eto port — value ignored. Use the Background Color input to set a window color.");
            }

            string colorName = null;
            if (DA.GetData("Accent Color", ref colorName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Accent colors (MahApps) are not supported in the Eto port — value ignored.");
            }

            bool showTitleBar = true;
            DA.GetData("Show Title Bar", ref showTitleBar);
            // Eto.Forms.WindowStyle.None drops the OS chrome entirely
            // (closest approximation of the WPF MetroWindow with its title-bar
            // buttons toggled off). Default keeps the standard frame.
            mw.WindowStyle = showTitleBar ? WindowStyle.Default : WindowStyle.None;

            System.Drawing.Color bg = System.Drawing.Color.Transparent;
            if (DA.GetData("Background Color", ref bg))
            {
                mw.BackgroundColor = Color.FromArgb(bg.R, bg.G, bg.B, bg.A);
            }

            double scaleFactor = 1.0;
            DA.GetData("Scale Factor", ref scaleFactor);
            if (Math.Abs(scaleFactor - 1.0) > 1e-6)
            {
                // The WPF version wrapped the masterGrid in a ScaleTransform —
                // Eto has no equivalent transform that applies to a Container
                // subtree, so we approximate the scale by enlarging
                // ClientSize. Fonts and child sizes don't grow, only the
                // overall canvas, but it's the closest behavior we can deliver
                // without a layout pass.
                mw.ClientSize = new Size(
                    (int)Math.Round(mw.ClientSize.Width * scaleFactor),
                    (int)Math.Round(mw.ClientSize.Height * scaleFactor));
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetWindowProperties;

        public override Guid ComponentGuid => new Guid("{14A1EE78-6536-43B2-B6D8-4B26A736F0A9}");

#if HUI_WINDOWS
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            var item = GH_DocumentObject.Menu_AppendItem(menu, "Create Accent List", CreateAccentList);
            item.ToolTipText = "Click this to create a pre-populated list of the historical MahApps accent color names.";
            base.AppendAdditionalMenuItems(menu);
        }

        private void CreateAccentList(object sender, EventArgs e)
        {
            var docIO = new GH_DocumentIO { Document = new GH_Document() };
            var vl = new GH_ValueList();
            vl.ListItems.Clear();
            foreach (var color in ACCENT_COLORS)
                vl.ListItems.Add(new GH_ValueListItem(color, $"\"{color}\""));
            vl.NickName = "Accent Colors";

            var doc = OnPingDocument();
            if (docIO.Document == null || doc == null) return;
            docIO.Document.AddObject(vl, false, 1);
            var pivot = Params.Input[3].Attributes.Pivot;
            vl.Attributes.Pivot = new System.Drawing.PointF(pivot.X - 120, pivot.Y - 11);
            docIO.Document.SelectAll();
            docIO.Document.ExpireSolution();
            docIO.Document.MutateAllIds();
            var objs = docIO.Document.Objects;
            doc.DeselectAll();
            doc.UndoUtil.RecordAddObjectEvent("Create Accent List", objs);
            doc.MergeDocument(docIO.Document);
        }
#endif
    }
}

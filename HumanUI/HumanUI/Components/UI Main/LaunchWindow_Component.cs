using System;
using System.ComponentModel;
#if HUI_WINDOWS
using System.Windows.Forms;
#endif
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Ownership status of a Human UI window.
    /// </summary>
    internal enum childStatus { ChildOfGH, ChildOfRhino, AlwaysOnTop }

    /// <summary>
    /// Launches an empty Human UI window. The component preserves the original GUID and
    /// parameter shape so existing .gh files load unchanged. The internal implementation
    /// is Eto.Forms in the Eto migration; the child-of-Rhino owner relationship is left
    /// as a TopMost fallback until phase 5 wires the platform-specific HWND/NSWindow owner.
    /// </summary>
    public class LaunchWindow_Component : GH_Component
    {
        private childStatus winChildStatus = childStatus.ChildOfGH;

        protected MainWindow mw;
        bool shouldBeVisible = true;
        bool enableHorizScroll = false;
        // True only while we are tearing the window down ourselves; the Closing handler
        // uses this to distinguish a user X-click (which should hide) from a programmatic
        // close (which should let the close go through).
        bool _allowProgrammaticClose = false;

        public LaunchWindow_Component()
            : base("Launch Window", "LaunchWin", "This component launches a new blank control window.", "Human UI", "UI Main")
        {
            UpdateMenu();
        }

        public LaunchWindow_Component(string name, string nickname, string description, string category, string subcategory)
            : base(name, nickname, description, category, subcategory)
        {
            UpdateMenu();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Show", "S", "Set this boolean to true to display the control window.", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "The name of the window to display.", GH_ParamAccess.item, "Control Window");
            pManager.AddIntegerParameter("Width", "W", "Starting Width of the window.", GH_ParamAccess.item, 370);
            pManager.AddIntegerParameter("Height", "H", "Starting Height of the window.", GH_ParamAccess.item, 400);
            pManager.AddTextParameter("Font Family", "F", "Optional Font family for UI elements in this window.", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Window Object", "W", "The window object. Other components can access this to add controls or gather data from the window.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool show = false;
            int width = 370;
            int height = 400;
            string font = "Segoe UI";
            string windowName = "Control Window";
            if (!DA.GetData("Show", ref show)) return;
            DA.GetData("Name", ref windowName);
            DA.GetData("Width", ref width);
            DA.GetData("Height", ref height);

            if (mw == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Window failed to initialize. See Rhino command line for details.");
                return;
            }

            mw.Title = windowName;
            mw.ClientSize = new Size(width, height);
            mw.HorizontalScrollingEnabled = enableHorizScroll;

            if (show)
            {
                shouldBeVisible = true;
                mw.Show();
                // With Owner set via ApplyChildStatus the parent-child z-order is
                // enforced by the OS — the window stays above the owner without
                // needing the previous Topmost flicker hack. We still
                // BringToFront once so the initial appearance is on top even
                // when GH had focus on the same Owner level.
                mw.BringToFront();
            }
            else
            {
                shouldBeVisible = false;
                mw.Visible = false;
            }

            if (DA.GetData("Font Family", ref font))
            {
                try { mw.setFont(font); }
                catch { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Font not recognized"); }
            }

            DA.SetData("Window Object", mw);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.LaunchWindow;

        public override Guid ComponentGuid => new Guid("{0A6B8A40-57A4-4D8D-9F09-F34869655D1E}");

        protected override void BeforeSolveInstance()
        {
            if (mw == null)
            {
                SetupWin();
            }
            base.BeforeSolveInstance();
        }

        private void SetupWin()
        {
            // Carry position/size across an internal recreate (menu change, etc.) so the
            // user's hand-placed location isn't lost. Eto Form exposes Location and
            // ClientSize; we read both before tearing down.
            Point? carryLocation = null;
            Size? carrySize = null;
            if (mw != null && mw.Visible)
            {
                carryLocation = mw.Location;
                carrySize = mw.ClientSize;
            }

            try
            {
                _allowProgrammaticClose = true;
                mw?.Close();
            }
            catch { }
            finally { _allowProgrammaticClose = false; }

            mw = new MainWindow();
            if (carryLocation.HasValue) mw.Location = carryLocation.Value;
            if (carrySize.HasValue) mw.ClientSize = carrySize.Value;

            // Closing intercepts a user X-click so we hide instead of destroying. Destroy
            // would lose all wired element state and force a full window rebuild.
            mw.Closing += mw_Closing;
            mw.Closed += mw_Closed;

            ApplyChildStatus(mw, winChildStatus);

#if HUI_WINDOWS
            // Grasshopper.Instances.ActiveCanvas is a WinForms Control on
            // Windows; touching it from net7.0 (no -windows) won't resolve.
            // On Mac the auto-hide-on-document-change behaviour is deferred —
            // GH on Mac uses Eto for the canvas, hookup TBD.
            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas != null)
            {
                canvas.DocumentChanged -= HideWindow;
                canvas.DocumentChanged += HideWindow;
            }
#endif
        }

        /// <summary>
        /// Apply the parent-window relationship that controls how the HumanUI window
        /// orders against Rhino / Grasshopper / other apps.
        ///
        ///   Windows:
        ///     ChildOfRhino  — Eto.Forms.Window.Owner = Rhino's main window.
        ///     ChildOfGH     — Owner = GH window if reachable via Eto; otherwise
        ///                     falls back to Rhino main. (On Win, GH is WinForms
        ///                     and isn't reachable as an Eto.Forms.Window.)
        ///     AlwaysOnTop   — Owner = null, Topmost = true.
        ///
        ///   Mac:
        ///     Rhino.UI.RhinoEtoApp.MainWindow returns null and GH's canvas
        ///     isn't enumerated by Eto.Forms.Application.Instance.Windows
        ///     either (verified by diagnostic in 0.9.29). The Eto Owner
        ///     mechanism therefore has no parent to attach to. ChildOfRhino
        ///     and ChildOfGH both fall back to Topmost = true — keeps the
        ///     window above GH but loses the "follows GH minimize/restore"
        ///     behavior. Proper Mac child-window semantics need
        ///     NSWindow.AddChildWindow via AppKit, which requires either
        ///     a net7.0-macos TFM or Eto.Mac ObjC interop — Phase 7 work.
        /// </summary>
        internal static void ApplyChildStatus(MainWindow mw, childStatus status)
        {
            var rhinoMain = Rhino.UI.RhinoEtoApp.MainWindow;
            var gh = FindGrasshopperWindow();

            switch (status)
            {
                case childStatus.AlwaysOnTop:
                    mw.Owner = null;
                    mw.Topmost = true;
                    return;
                case childStatus.ChildOfGH:
#if HUI_WINDOWS
                    mw.Owner = gh ?? rhinoMain;
                    mw.Topmost = false;
#else
                    // Mac: no reachable parent NSWindow via Eto. Topmost is
                    // the only mechanism that keeps the window above GH.
                    mw.Owner = gh ?? rhinoMain;
                    mw.Topmost = true;
#endif
                    return;
                case childStatus.ChildOfRhino:
                default:
#if HUI_WINDOWS
                    mw.Owner = rhinoMain;
                    mw.Topmost = false;
#else
                    mw.Owner = rhinoMain;
                    mw.Topmost = true;
#endif
                    return;
            }
        }

        /// <summary>
        /// Best-effort lookup of the Grasshopper canvas window as an Eto window.
        /// Returns null on Mac (GH's canvas isn't an Eto.Forms.Window the way
        /// we'd hoped) and on Windows (GH is WinForms). Title-matching against
        /// Application.Instance.Windows is kept against the day a future
        /// Rhino/GH version exposes it.
        /// </summary>
        private static Eto.Forms.Window FindGrasshopperWindow()
        {
            try
            {
                foreach (var w in Eto.Forms.Application.Instance.Windows)
                {
                    if (w?.Title != null && w.Title.IndexOf("grasshopper", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return w;
                }
            }
            catch { }
            return null;
        }

        void mw_Closing(object sender, CancelEventArgs e)
        {
            if (_allowProgrammaticClose) return;
            e.Cancel = true;
            shouldBeVisible = false;
            mw.Visible = false;
        }

        void mw_Closed(object sender, EventArgs e)
        {
            mw.Closing -= mw_Closing;
            mw.Closed -= mw_Closed;
            SetupWin();
        }

        private void HideWindow(object sender, Grasshopper.GUI.Canvas.GH_CanvasDocumentChangedEventArgs e)
        {
            if (mw == null) return;
            if (e.NewDocument == OnPingDocument() && shouldBeVisible)
            {
                try { mw.Visible = true; } catch { }
            }
            else
            {
                try { mw.Visible = false; } catch { }
            }
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Child of Grasshopper", menu_makeChildofGH, true, winChildStatus == childStatus.ChildOfGH)
                .ToolTipText = "When selected, the window is made a child of the Grasshopper window.";
            GH_DocumentObject.Menu_AppendItem(menu, "Child of Rhino", menu_makeChildofRhino, true, winChildStatus == childStatus.ChildOfRhino)
                .ToolTipText = "When selected, the window is made a child of the Rhino window.";
            GH_DocumentObject.Menu_AppendItem(menu, "Always On Top", menu_makeAlwaysOnTop, true, winChildStatus == childStatus.AlwaysOnTop)
                .ToolTipText = "When selected, the window is always on top, floating above other apps.";
            GH_DocumentObject.Menu_AppendSeparator(menu);
            GH_DocumentObject.Menu_AppendItem(menu, "Enable Horizontal Scrolling", menu_toggleHorizScroll, true, enableHorizScroll)
                .ToolTipText = "When enabled, the window will show a scroll bar when content exceeds the window width.";
        }
#endif

        private void menu_toggleHorizScroll(object sender, EventArgs e)
        {
            RecordUndoEvent("Horizontal Scrolling Toggle");
            enableHorizScroll = !enableHorizScroll;
            SetupWin();
            ExpireSolution(true);
        }

        private void menu_makeChildofGH(object sender, EventArgs e)
        {
            RecordUndoEvent("Child Window Status Change");
            winChildStatus = childStatus.ChildOfGH;
            UpdateMenu();
            SetupWin();
            ExpireSolution(true);
        }

        private void menu_makeChildofRhino(object sender, EventArgs e)
        {
            RecordUndoEvent("Child Window Status Change");
            winChildStatus = childStatus.ChildOfRhino;
            UpdateMenu();
            SetupWin();
            ExpireSolution(true);
        }

        private void menu_makeAlwaysOnTop(object sender, EventArgs e)
        {
            RecordUndoEvent("Child Window Status Change");
            winChildStatus = childStatus.AlwaysOnTop;
            UpdateMenu();
            SetupWin();
            ExpireSolution(true);
        }

        private void UpdateMenu()
        {
            switch (winChildStatus)
            {
                case childStatus.ChildOfGH: Message = "Child of GH"; break;
                case childStatus.AlwaysOnTop: Message = "Always On Top"; break;
                case childStatus.ChildOfRhino: Message = "Child of Rhino"; break;
            }
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            try
            {
                _allowProgrammaticClose = true;
                mw?.Close();
            }
            catch { }
            finally { _allowProgrammaticClose = false; }
            base.RemovedFromDocument(document);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ChildStatus", (int)winChildStatus);
            writer.SetBoolean("EnableHorizScroll", enableHorizScroll);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int readVal = -1;
            reader.TryGetInt32("ChildStatus", ref readVal);

            bool savedHorizScroll = false;
            reader.TryGetBoolean("EnableHorizScroll", ref savedHorizScroll);

            winChildStatus = (childStatus)readVal;
            enableHorizScroll = savedHorizScroll;
            UpdateMenu();
            return base.Read(reader);
        }
    }
}

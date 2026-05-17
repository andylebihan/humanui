using System;
using System.ComponentModel;
using System.Windows.Forms;
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
                // Eto's Show() does not always pull a window forward when another HWND has
                // focus (the canvas the user just clicked on). BringToFront fixes the
                // "appears behind Rhino" complaint on first toggle.
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

            var canvas = Grasshopper.Instances.ActiveCanvas;
            if (canvas != null)
            {
                canvas.DocumentChanged -= HideWindow;
                canvas.DocumentChanged += HideWindow;
            }
        }

        /// <summary>
        /// Apply the AlwaysOnTop semantics. Owner-handle parenting (ChildOfGH / ChildOfRhino)
        /// is platform-specific and is deferred until phase 5; for now ChildOf* falls back
        /// to default placement and AlwaysOnTop still works via Topmost.
        /// </summary>
        internal static void ApplyChildStatus(MainWindow mw, childStatus status)
        {
            mw.Topmost = status == childStatus.AlwaysOnTop;
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

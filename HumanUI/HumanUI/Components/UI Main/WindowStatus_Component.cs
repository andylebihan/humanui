using System;
using Eto.Forms;
using Grasshopper.Kernel;
using HumanUIBaseApp;

namespace HumanUI.Components
{
    /// <summary>
    /// Returns "Hidden" / "Minimized" / "Normal" for a HUI MainWindow. Hooks
    /// the window's Closed + WindowStateChanged events so the listener
    /// component re-fires when the user closes / minimizes the window.
    /// </summary>
    public class WindowStatus_Component : GH_Component
    {
        public WindowStatus_Component()
            : base("Window Status", "WinStat",
                "Gets the current status of the specified Window",
                "Human UI", "UI Main")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Window", "W", "The window to check", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "The status of the window", GH_ParamAccess.item);
        }

        private MainWindow _listened;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (_listened != null) RemoveEvents(_listened);
            MainWindow window = null;
            if (!DA.GetData("Window", ref window)) return;
            _listened = window;
            AddEvents(window);

            string status;
            if (!window.Visible) status = "Hidden";
            else if (window.WindowState == WindowState.Minimized) status = "Minimized";
            else status = "Normal";
            DA.SetData("Status", status);
        }

        private void AddEvents(MainWindow window)
        {
            window.Closed += ExpireThis;
            window.WindowStateChanged += ExpireThis;
        }

        private void RemoveEvents(MainWindow window)
        {
            window.Closed -= ExpireThis;
            window.WindowStateChanged -= ExpireThis;
        }

        private void ExpireThis(object sender, EventArgs e) => ExpireSolution(true);

        protected override System.Drawing.Bitmap Icon => Properties.Resources.WindowStatus;
        public override Guid ComponentGuid => new Guid("{a9ec5ddf-0ed9-4c67-90ac-ae5586390f12}");
    }
}

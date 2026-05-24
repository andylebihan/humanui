using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Set one Human UI window as a child of another by establishing the
    /// Eto.Forms.Window.Owner relationship. The optional "Default Option"
    /// re-runs the parent-status logic from LaunchWindow_Component for cases
    /// where you want to break the parent link and revert to Child of GH /
    /// Child of Rhino / Always on Top.
    /// </summary>
    public class MakeChildWindow_Component : GH_Component
    {
        public MakeChildWindow_Component()
            : base("Make Child Window", "ChildWin",
                "Make one window a child of another",
                "Human UI", "UI Main")
        { }

        public override Guid ComponentGuid => new Guid("{A067CEBE-045C-4F4A-8D77-CD4FB0075A5A}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parent Window", "P", "The parent window", GH_ParamAccess.item);
            pManager.AddGenericParameter("Child Window", "C", "The child window", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Default Option", "D", "Use this if you want to reset the child window to one of the default modes and break its relationship with the parent.", GH_ParamAccess.item);
            pManager[2].Optional = true;
            var defaultOptionParam = (Param_Integer)pManager[2];
            defaultOptionParam.AddNamedValue("Child of GH", 0);
            defaultOptionParam.AddNamedValue("Child of Rhino", 1);
            defaultOptionParam.AddNamedValue("Always on Top", 2);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            MainWindow parent = null;
            MainWindow child = null;
            int defaultOption = -1;

            if (!DA.GetData("Parent Window", ref parent)) return;
            if (!DA.GetData("Child Window", ref child)) return;
            bool hasDefaultOption = DA.GetData("Default Option", ref defaultOption);

            if (hasDefaultOption)
            {
                if (!Enum.IsDefined(typeof(childStatus), defaultOption))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid value for default option. Nothing will happen!");
                    return;
                }
                LaunchWindow_Component.ApplyChildStatus(child, (childStatus)defaultOption);
                return;
            }

            // Establish parent/child ownership. Eto enforces the z-order via
            // the OS — same mechanism LaunchWindow uses for ChildOfGH /
            // ChildOfRhino. Topmost gets cleared so the child stays under
            // the parent's z-axis rather than floating above everything.
            child.Topmost = false;
            child.Owner = parent;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MakeChildWindow;
    }
}

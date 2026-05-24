using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Returns the logical width / height of the primary screen. The Eto.Forms
    /// Screen API already returns logical (DPI-independent) units so the WPF
    /// version's DPI multiplier is unnecessary — Eto's Bounds match what
    /// MainWindow.ClientSize uses for positioning.
    /// </summary>
    public class GetScreenDimensions_Component : GH_Component
    {
        public GetScreenDimensions_Component()
            : base("Get Screen Dimensions", "GetScreen",
                "Gets the dimensions of the current screen",
                "Human UI", "UI Main")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) { }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            DA.SetData("Width", bounds.Width);
            DA.SetData("Height", bounds.Height);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.screenDimensions;
        public override Guid ComponentGuid => new Guid("{415bcdd1-11f0-4eae-ba0b-c48b50037de3}");
    }
}

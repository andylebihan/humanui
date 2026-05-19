using System;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Creates a two-thumb range slider over the given Interval. The Eto port
    /// builds the control out of two synchronized HUI_FloatSliders (see
    /// HUI_RangeSlider) because Eto.Forms doesn't ship a native dual-handle
    /// slider. ValueListener returns the current range as an Interval.
    /// </summary>
    public class CreateRangeSlider : GH_Component
    {
        public CreateRangeSlider()
            : base("Create Range Slider", "RangeSlider",
                "Creates a double-slider that describes a range",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntervalParameter("Slider Range", "R", "The range that defines the min and max of the slider extents", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Starting Range", "SR", "The initial value selected on the slider", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Range Slider", "RS", "The Range Slider Element.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Interval sliderRange = Interval.Unset;
            Interval startingRange = Interval.Unset;
            if (!DA.GetData("Slider Range", ref sliderRange)) return;
            if (!DA.GetData("Starting Range", ref startingRange)) return;

            var rs = new HUI_RangeSlider(sliderRange.Min, sliderRange.Max, startingRange.Min, startingRange.Max);
            DA.SetData("Range Slider", new UIElement_Goo(rs, "Range Slider", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateRangeSlider;

        public override Guid ComponentGuid => new Guid("{FF8EBC79-B1CE-430B-8734-E1993F7E477F}");
    }
}

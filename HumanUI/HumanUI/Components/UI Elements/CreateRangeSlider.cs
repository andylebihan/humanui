using System;
using Eto.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Phase 3 stub. Eto.Forms has no native dual-handle range slider; the real
    /// port is Phase 4 (composite of two synced Sliders with shared painted
    /// track). Until then we emit a Label so .gh files that reference this
    /// component still load.
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

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Range Slider is a Phase 4 stub in this Eto preview.");
            var placeholder = new Label
            {
                Text = $"[Range Slider stub: {startingRange.Min:0.##} – {startingRange.Max:0.##}]",
            };
            DA.SetData("Range Slider", new UIElement_Goo(placeholder, "Range Slider", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateRangeSlider;

        public override Guid ComponentGuid => new Guid("{FF8EBC79-B1CE-430B-8734-E1993F7E477F}");
    }
}

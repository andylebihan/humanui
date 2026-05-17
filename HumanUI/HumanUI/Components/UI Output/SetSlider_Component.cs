using System;
using Eto.Forms;
using Grasshopper.Kernel;
using HumanUI.Components.UI_Elements;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Update the range and value of an existing slider produced by CreateSlider.
    /// </summary>
    public class SetSlider_Component : GH_Component
    {
        public SetSlider_Component()
            : base("Set Slider", "SetSlider",
                "Modify the range and value of a slider.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Slider", "S", "The UI Slider to update", GH_ParamAccess.item);
            pManager.AddNumberParameter("Value", "V", "The value to set the slider to", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Range", "R", "the new slider range", GH_ParamAccess.item);
            for (int i = 1; i < 3; i++) pManager[i].Optional = true;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object sliderObject = null;
            Interval range = Interval.Unset;
            double newValue = double.NaN;
            if (!DA.GetData("UI Slider", ref sliderObject)) return;
            bool hasRange = DA.GetData("Range", ref range);
            bool hasValue = DA.GetData("Value", ref newValue);

            var container = HUI_Util.GetUIElement<Control>(sliderObject);
            var slider = HUI_Util.findSlider(container);
            if (slider == null) return;

            if (hasRange)
            {
                slider.Configure(range.Min, range.Max, slider.FloatValue, slider.DecimalPlaces, slider.IntegerSlider);
            }
            if (hasValue)
            {
                slider.FloatValue = newValue;
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetSlider;

        public override Guid ComponentGuid => new Guid("{B412D7D3-02E2-4A8E-BDCC-2E1F8B2A8834}");
    }
}

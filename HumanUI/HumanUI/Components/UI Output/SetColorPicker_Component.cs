using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using SysColor = System.Drawing.Color;

namespace HumanUI.Components.UI_Output
{
    public class SetColorPicker_Component : GH_Component
    {
        public SetColorPicker_Component()
          : base("Set Color Picker", "SetColorPicker",
                "Use this to set the values of a color picker",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Color picker to modify", "CP", "The color picker object to modify", GH_ParamAccess.item);
            pManager.AddColourParameter("Default Color", "D", "The color displayed on the picker. Optional.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddColourParameter("Available Colors", "C", "An optional list of possible colors to limit the user's selection. \nIf left blank, the existing set of available colors will not be changed.", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object myUIElement = null;
            SysColor defaultCol = SysColor.Transparent;
            var availableCols = new List<SysColor>();

            if (!DA.GetData(0, ref myUIElement)) return;
            var picker = HUI_Util.GetUIElement<ColorPicker>(myUIElement);
            if (picker == null) return;

            if (DA.GetData("Default Color", ref defaultCol))
            {
                // Same alpha-normalisation as CreateColorPicker — treat A=0 as
                // unspecified rather than "fully transparent" so an upstream
                // Color.Empty doesn't silently make the picker (and any
                // downstream consumers like Set 3D View) render transparent.
                byte alpha = defaultCol.A == 0 ? (byte)255 : defaultCol.A;
                picker.Value = Eto.Drawing.Color.FromArgb(defaultCol.R, defaultCol.G, defaultCol.B, alpha);
            }
            DA.GetDataList("Available Colors", availableCols);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetColorPicker;

        public override Guid ComponentGuid => new Guid("77ec992b-a0d8-440c-b9ad-91098a40cd78");
    }
}

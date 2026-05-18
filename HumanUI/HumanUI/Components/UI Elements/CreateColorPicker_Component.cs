using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using SysColor = System.Drawing.Color;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Colour picker backed by Eto.Forms.ColorPicker. Eto's picker doesn't expose a
    /// curated palette like the old Xceed picker did, so the "Available Colors" input is
    /// recorded but no longer constrains the picker; users still get the platform
    /// colour dialog.
    /// </summary>
    public class CreateColorPicker_Component : GH_Component
    {
        public CreateColorPicker_Component()
            : base("Create Color Picker", "ColorPicker",
                "Creates an interactive color picker, with an optionally supplied set of colors",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Default Color", "D", "The color displayed on the picker at load. Optional.", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager.AddColourParameter("Available Colors", "C", "An optional list of possible colors to limit the user's selection. \nIf left blank, the standard set of colors will display.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Color Picker", "P", "The Color Picker UI element. Use in conjunction with an \"Add Elements\" component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            SysColor defaultCol = SysColor.Transparent;
            var availableCols = new List<SysColor>();

            var picker = new ColorPicker();

            if (DA.GetData("Default Color", ref defaultCol))
            {
                picker.Value = Eto.Drawing.Color.FromArgb(defaultCol.R, defaultCol.G, defaultCol.B, defaultCol.A);
            }
            DA.GetDataList("Available Colors", availableCols);

            DA.SetData("Color Picker", new UIElement_Goo(picker, "Color Picker", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ColorPicker;

        public override Guid ComponentGuid => new Guid("{0c914dd3-ed91-4255-9c47-0148c126ea25}");
    }
}

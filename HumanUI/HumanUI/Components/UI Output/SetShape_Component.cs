using System;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Stub for the Eto migration. Pairs with the CreateShape stub; no-ops at runtime
    /// and emits a remark so users can see the gap.
    /// </summary>
    public class SetShape_Component : GH_Component
    {
        public SetShape_Component()
            : base("Set Shape", "SetShape",
                "Modify an existing shape (Eto preview: not yet ported).",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "The shape element to modify", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curve", "SC", "The new curve outline", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddColourParameter("Fill Color", "FC", "Fill color", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddNumberParameter("Stroke Weight", "SW", "Stroke weight", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddColourParameter("Stroke Color", "SC", "Stroke color", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddNumberParameter("Scale", "Scl", "Scale factor", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddIntegerParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager[6].Optional = true;
            pManager.AddIntegerParameter("Height", "H", "Height", GH_ParamAccess.item);
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Set Shape is not yet ported to Eto.");
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetShape;

        public override Guid ComponentGuid => new Guid("{f6881435-7de3-4098-ada8-f3068ed7331c}");
    }
}

using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Stub for the Eto migration. The original component used a custom WPF
    /// ClickableShapeGrid; the Eto reimplementation is queued for Phase 5. Today the
    /// GUID and parameter signature are preserved so existing .gh files load, and the
    /// component emits a placeholder Label.
    /// </summary>
    public class CreateShape_Component : GH_Component
    {
        public CreateShape_Component()
            : base("Create Shape", "Shape",
                "Creates a simple shape from a polyline (Eto preview: placeholder).",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shape", "S", "The curve outlines for the shape.", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "Fill color", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddNumberParameter("Stroke Weight", "SW", "Stroke weight", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddColourParameter("Stroke Color", "SC", "Stroke color", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddNumberParameter("Scale", "Scl", "Scale factor", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddIntegerParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddIntegerParameter("Height", "H", "Height", GH_ParamAccess.item);
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The shape element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Shape is not yet ported to Eto. Placeholder shown.");
            var placeholder = new Label { Text = "[Shape - not yet ported]" };
            DA.SetData("Shape", new UIElement_Goo(placeholder, "Shape (stub)", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShape;

        public override Guid ComponentGuid => new Guid("{0ab1c8a7-4182-4a7b-bda3-67c24677182c}");
    }
}

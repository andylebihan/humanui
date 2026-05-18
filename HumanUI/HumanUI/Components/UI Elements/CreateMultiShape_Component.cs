using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Phase 5 stub. The original WPF component depended on
    /// System.Windows.Shapes.Path and a click-tracking ClickableShapeGrid; both
    /// are part of the "Hard 5" deferred to the WPF-on-Windows + Mac stub plan
    /// in Phase 5. This stub preserves the parameter shape so .gh files load.
    /// </summary>
    public class CreateMultiShape_Component : GH_Component
    {
        public CreateMultiShape_Component()
            : base("Create Shapes", "Shapes",
                "Creates shapes from a polylines",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shapes", "S", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill colors. Leave empty for no fill", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weights. Leave empty or set to 0 for no stroke.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Optional output width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Optional output height.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curves = new List<Curve>();
            DA.GetDataList("Shapes", curves);
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Create Shapes is a Phase 5 stub in this Eto preview.");
            var placeholder = new Label { Text = $"[Shapes stub: {curves.Count} curve(s)]" };
            DA.SetData("Shape", new UIElement_Goo(placeholder, "Shapes", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShapes;

        public override Guid ComponentGuid => new Guid("{94288CED-76F6-438A-9216-E60C8290F640}");
    }
}

using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Stub for the Eto migration. The original component used HelixToolkit.Wpf for an
    /// embedded 3D viewport; this is one of the "hard 5" deferred to Phase 5 where the
    /// Windows build will keep the HelixToolkit.Wpf control wrapped in an Eto host, and
    /// the Mac build will continue to surface this placeholder.
    /// </summary>
    public class Create3DView_Component : GH_Component
    {
        public Create3DView_Component()
            : base("Create 3D View", "3DView",
                "Creates an orbitable 3d viewport with a custom-defined mesh (Eto preview: placeholder).",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh to display", "M", "The meshes to show in the viewport", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The colours for each mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("View Width", "W", "Viewport width in pixels", GH_ParamAccess.item);
            pManager.AddNumberParameter("View Height", "H", "Viewport height in pixels", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("3DView", "V", "The 3D view element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "3D View is not yet ported to Eto. Placeholder shown.");
            var placeholder = new Label { Text = "[3D View - not yet ported]" };
            DA.SetData("3DView", new UIElement_Goo(placeholder, "3DView (stub)", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Create3dView;

        public override Guid ComponentGuid => new Guid("{5d84c99e-fe9c-4546-8cb0-9f6fe58e011d}");
    }
}

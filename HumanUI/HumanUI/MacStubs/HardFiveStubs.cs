// Mac-only stubs for the Hard 5 components. Compiled only when TargetFramework=net7.0
// (selected via the conditional ItemGroup in HumanUI.csproj). The Windows TFM gets the
// real WPF-backed versions; on Mac we surface a placeholder Label and a runtime remark
// so .gh files load without "Unrecognized Object" dialogs and downstream Set components
// still resolve their GUIDs. Param signatures exactly mirror the Windows ports.

using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    public class Create3DView_Component : GH_Component
    {
        public Create3DView_Component()
            : base("Create 3D View", "3DView",
                "Creates an orbitable 3d viewport (Mac stub: not supported, placeholder shown).",
                "Human UI", "UI Elements") { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh to display", "M", "The mesh(es) to display in the viewport", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The color with which to display the mesh.", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager.AddNumberParameter("View Width", "W", "The width of the 3d viewport", GH_ParamAccess.item, 300);
            pManager.AddNumberParameter("View Height", "H", "The height of the 3d viewport", GH_ParamAccess.item, 300);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("3DView", "V", "The 3D view containing your mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "3D View not supported on Mac (Phase 6 stub).");
            var placeholder = new Label { Text = "[3D View - Mac stub]" };
            DA.SetData("3DView", new UIElement_Goo(placeholder, "3D View", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Create3dView;
        public override Guid ComponentGuid => new Guid("{5d84c99e-fe9c-4546-8cb0-9f6fe58e011d}");
    }

    // CreateShape_Component lives in MacStubs/HUI_Shape.cs — real
    // Eto.Drawable-backed implementation rather than a label placeholder.

    // CreateMultiShape_Component lives in MacStubs/HUI_MultiShape.cs — real
    // Eto.Drawable-backed implementation with hit-testing and click modes.

    // CreateGraphMapper_Component lives in MacStubs/HUI_GraphMapper.cs —
    // real Eto.Drawable-backed bezier editor with four draggable handles.

    // CreateGradientEditor_Component lives in MacStubs/HUI_GradientEditor.cs
    // (real Eto.Drawable-backed gradient strip with draggable stops). Note
    // its namespace is HumanUI.Components (not HumanUI.Components.UI_Elements)
    // — matches the Windows component's namespace so GUIDs land cleanly.
}

namespace HumanUI.Components.UI_Output
{
    public class Set3DView_Component : GH_Component
    {
        public Set3DView_Component()
            : base("Set 3D View", "Set3DView", "Modify a 3D View (Mac stub).", "Human UI", "UI Output") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("3D View", "V", "The 3D View to modify", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh to display", "M", "The new meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The new mesh colours", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Set 3D View not supported on Mac.");
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Set3dView;
        public override Guid ComponentGuid => new Guid("{3472130d-fc0e-409d-9295-f93ecaf1afb5}");
    }

    // SetShape_Component lives in MacStubs/HUI_Shape.cs alongside the
    // real CreateShape implementation.

    // SetShapes_Component lives in MacStubs/HUI_MultiShape.cs alongside the
    // real CreateShapes implementation.
}

// Create/SetChart, Create/SetMultiChart, and SetChartAppearance live in
// MacStubs/HUI_Charts.cs — real Eto.Drawable-backed chart rendering. They
// share namespace HumanUI.Components with the Windows components.

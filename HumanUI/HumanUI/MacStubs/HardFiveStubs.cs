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

    public class CreateMultiShape_Component : GH_Component
    {
        public CreateMultiShape_Component()
            : base("Create Shapes", "Shapes",
                "Creates shapes from polylines (Mac stub).",
                "Human UI", "UI Elements") { }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shapes", "S", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill colors.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weights.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height.", GH_ParamAccess.item);
            for (int i = 1; i < 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Create Shapes not supported on Mac (ClickableShapeGrid stub).");
            DA.SetData("Shape", new UIElement_Goo(new Label { Text = "[Shapes - Mac stub]" }, "Shapes", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShapes;
        public override Guid ComponentGuid => new Guid("{94288CED-76F6-438A-9216-E60C8290F640}");
    }

    public class CreateGraphMapper_Component : GH_Component
    {
        public CreateGraphMapper_Component()
            : base("Create Graph Mapper", "GraphMapper",
                "Creates a Bezier Graph Mapper (Mac stub).",
                "Human UI", "UI Elements") { }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Graph Mapper", "G", "Starting configuration", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Graph Mapper", "G", "The Graph Mapper UI element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Graph Mapper not supported on Mac (WPF bezier UserControl stub).");
            DA.SetData("Graph Mapper", new UIElement_Goo(new Label { Text = "[Graph Mapper - Mac stub]" }, "Graph Mapper", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GraphMapper;
        public override Guid ComponentGuid => new Guid("{bc47947b-f83f-4cdd-b7d8-abc546a26c5e}");
    }

    public class CreateGradientEditor_Component : GH_Component
    {
        public CreateGradientEditor_Component()
            : base("Create Gradient Editor", "Gradient",
                "Creates an editable gradient (Mac stub).",
                "Human UI", "UI Elements") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient(s)", "G", "Gradient or presets.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddGenericParameter("Default Gradient", "D", "Default gradient.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddBooleanParameter("Show Editor", "E", "Show editor.", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Show Presets", "P", "Show preset menu.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Gradient Editor", "GE", "The Gradient Editor Element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Gradient Editor not supported on Mac (Xceed.Wpf.Toolkit stub).");
            DA.SetData("Gradient Editor", new UIElement_Goo(new Label { Text = "[Gradient Editor - Mac stub]" }, "Gradient Editor", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GradientEditor;
        public override Guid ComponentGuid => new Guid("{DA8DD6A3-AD57-471E-B891-94902C8647ED}");
    }
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

    public class SetShapes_Component : GH_Component
    {
        public SetShapes_Component()
            : base("Set Shapes", "SetShapes", "Replace shapes (Mac stub).", "Human UI", "UI Output") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "Shapes container", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curves", "SC", "Curves", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Colors", "FC", "Fills", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weights", "SW", "Strokes", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Colors", "SC", "Stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Scale", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height", GH_ParamAccess.item);
            for (int i = 2; i < pManager.ParamCount; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Set Shapes not supported on Mac.");
        }

        public override Guid ComponentGuid => new Guid("{EDC4A536-7412-46F2-B56F-6D8668D6B983}");
    }
}

namespace HumanUI.Components
{
    /// <summary>Mac stub for the Charts components (MetroChart-backed on Windows).</summary>
    public class CreateChart_Component : GH_Component
    {
        public CreateChart_Component()
            : base("Create Chart", "Chart", "Creates a chart (Mac stub).", "Human UI", "UI Graphs") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Categories", "C", "Category labels", GH_ParamAccess.list);
            pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.list);
            pManager.AddTextParameter("Series Title", "T", "Title", GH_ParamAccess.item, "");
            pManager[2].Optional = true;
            pManager.AddIntegerParameter("Chart Type", "CT", "Chart Type", GH_ParamAccess.item, 0);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart", "Ch", "The chart element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Chart not supported on Mac (MetroChart stub).");
            DA.SetData("Chart", new UIElement_Goo(new Label { Text = "[Chart - Mac stub]" }, "Chart Elem", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateChart;
        public override Guid ComponentGuid => new Guid("{1A96F054-26DD-45C6-B09D-2760B496BB0A}");
    }

    public class CreateMultiChart_Component : GH_Component
    {
        public CreateMultiChart_Component()
            : base("Create Multi Chart", "MultiChart", "Creates a multi-series chart (Mac stub).", "Human UI", "UI Graphs") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Categories", "C", "Category labels", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.tree);
            pManager.AddTextParameter("Series Titles", "T", "Series Titles", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Chart Type", "CT", "Chart Type", GH_ParamAccess.item, 0);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MultiChart", "MC", "The multi chart element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Multi Chart not supported on Mac.");
            DA.SetData("MultiChart", new UIElement_Goo(new Label { Text = "[Multi Chart - Mac stub]" }, "Chart Elem", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateMultiChart;
        public override Guid ComponentGuid => new Guid("{66FA84E1-D224-4B4B-8DA4-E3E40CA815D5}");
    }
}

namespace HumanUI.Components.UI_Output
{
    public class SetChart_Component : GH_Component
    {
        public SetChart_Component() : base("Set Chart", "SetChart", "Set chart contents (Mac stub).", "Human UI", "UI Output") { }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart", "Ch", "Chart element", GH_ParamAccess.item);
            pManager.AddTextParameter("Categories", "C", "Categories", GH_ParamAccess.list);
            pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.list);
            pManager.AddTextParameter("Series Title", "T", "Title", GH_ParamAccess.item, "");
            pManager[3].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Set Chart not supported on Mac."); }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetChart;
        public override Guid ComponentGuid => new Guid("{1C4AC2EC-3090-4D03-8D17-923417129692}");
    }

    public class SetMultiChart_Component : GH_Component
    {
        public SetMultiChart_Component() : base("Set Multi Chart", "SetMultiChart", "Set multi-chart contents (Mac stub).", "Human UI", "UI Output") { }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Multi Chart", "MC", "Multi chart element", GH_ParamAccess.item);
            pManager.AddTextParameter("Categories", "C", "Categories", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Values", "V", "Values", GH_ParamAccess.tree);
            pManager.AddTextParameter("Series Titles", "T", "Series Titles", GH_ParamAccess.list);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Set Multi Chart not supported on Mac."); }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetMultiChart;
        public override Guid ComponentGuid => new Guid("{12A5A354-FC1B-4CEA-394A-BBEB71A23DB5}");
    }

    public class SetChart_Appearance : GH_Component
    {
        public SetChart_Appearance() : base("Set Chart Appearance", "SetChartLook", "Set chart appearance (Mac stub).", "Human UI", "UI Output") { }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Chart", "Ch", "Chart element", GH_ParamAccess.item);
            pManager.AddTextParameter("Title", "T", "Title", GH_ParamAccess.item, "");
            pManager[1].Optional = true;
            pManager.AddTextParameter("Subtitle", "S", "Subtitle", GH_ParamAccess.item, "");
            pManager[2].Optional = true;
            pManager.AddColourParameter("Background", "Bg", "Background", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddColourParameter("Foreground", "Fg", "Foreground", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddColourParameter("Series Colors", "C", "Series colors", GH_ParamAccess.list);
            pManager[5].Optional = true;
        }
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
        protected override void SolveInstance(IGH_DataAccess DA) { AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Set Chart Appearance not supported on Mac."); }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.ChartAppearance;
        public override Guid ComponentGuid => new Guid("{12A5A354-FC1B-4CEA-894A-BBEB99A23DB5}");
    }
}

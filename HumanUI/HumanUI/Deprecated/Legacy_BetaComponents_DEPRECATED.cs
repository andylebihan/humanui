using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Deprecated
{
    /// <summary>
    /// Hidden / Obsolete shims for HumanUI Beta 0.6.x components so old .gh files
    /// load without the "Unrecognized Objects" dialog. Each preserves the legacy
    /// GUID and parameter shape; SolveInstance either emits a placeholder element
    /// or delegates to the closest current component. Phase 4/5 will replace
    /// these stubs with first-class ports.
    /// </summary>
    internal static class LegacyStubHelpers
    {
        public static UIElement_Goo PlaceholderGoo(string name, Guid instanceGuid, int iteration, string note)
        {
            var l = new Label { Text = $"[{name} stub: {note}]" };
            return new UIElement_Goo(l, name, instanceGuid, iteration);
        }
    }

    /// <summary>Beta 0.6.x Create Text Box. Upgraded to current via Upgrade_TextBoxComponent.</summary>
    public class CreateTextBox_Beta_DEPRECATED : GH_Component
    {
        public CreateTextBox_Beta_DEPRECATED()
            : base("Create Text Box", "TextBox", "Creates a text box for entering text.", "Human UI", "UI Elements") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Label", "L", "The label for the text box.", GH_ParamAccess.item);
            pManager.AddTextParameter("Default Text", "D", "The default text in the text box.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Update Button", "U", "Show update button.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Box", "TB", "The created text box", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string label = "", defaultText = "";
            DA.GetData("Label", ref label);
            DA.GetData("Default Text", ref defaultText);
            var tb = new TextBox { Text = defaultText };
            tb.ID = "GH_TextBox_NoButton";
            DA.SetData("Text Box", new UIElement_Goo(tb, $"Text Box: {label}", InstanceGuid, DA.Iteration));
        }

        public override Guid ComponentGuid => new Guid("{c8d203fe-7e84-416a-b93e-d1bd746f3f66}");
    }

    /// <summary>Beta 0.6.x Create Checklist. Phase 4 port pending.</summary>
    public class CreateChecklist_Beta_DEPRECATED : GH_Component
    {
        public CreateChecklist_Beta_DEPRECATED()
            : base("Create Checklist", "Checklist", "Creates a checklist of items.", "Human UI", "UI Elements") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Checklist Items", "L", "The initial list of options to display in the checklist.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Selected", "S", "The initial selection state of each item.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Height", "H", "Optional checklist box height in pixels.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Checklist", "CL", "The checklist object", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var items = new List<string>();
            var selected = new List<bool>();
            DA.GetDataList("Checklist Items", items);
            DA.GetDataList("Selected", selected);
            DA.SetData("Checklist", LegacyStubHelpers.PlaceholderGoo("Checklist", InstanceGuid, DA.Iteration, $"{items.Count} item(s) — Phase 4 stub"));
        }

        public override Guid ComponentGuid => new Guid("{6e21dbe5-ecb8-4530-8a22-7cd713cf40d5}");
    }

    /// <summary>Beta 0.6.x "Create Grid" (3-input variant). Phase 4 port pending.</summary>
    public class CreateGrid_Beta_DEPRECATED : GH_Component
    {
        public CreateGrid_Beta_DEPRECATED()
            : base("Create Grid", "Grid", "Creates a grid layout for UI elements.", "Human UI", "UI Containers") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to add to the grid.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "The width of the grid.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The height of the grid.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Grid", "S", "The created grid", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData("Grid", LegacyStubHelpers.PlaceholderGoo("Grid", InstanceGuid, DA.Iteration, "Phase 4 stub"));
        }

        public override Guid ComponentGuid => new Guid("{1e68a9a8-c28d-4799-854c-337dc4018917}");
    }

    /// <summary>Later "Create Grid" with row/column definitions. Phase 4 port pending.</summary>
    public class CreateGrid_Full_DEPRECATED : GH_Component
    {
        public CreateGrid_Full_DEPRECATED()
            : base("Create Grid", "Grid", "Creates a grid layout for UI elements.", "Human UI", "UI Containers") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to add to the grid.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "The width of the grid.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The height of the grid.", GH_ParamAccess.item);
            pManager.AddTextParameter("Row Definitions", "RD", "The row definitions of the grid.", GH_ParamAccess.list);
            pManager.AddTextParameter("Column Definitions", "CD", "The column definitions of the grid.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element Row", "ER", "The row for each element.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element Column", "EC", "The column for each element.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element Row Span", "ERS", "The row span for each element.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Element Column Span", "ECS", "The column span for each element.", GH_ParamAccess.list);
            for (int i = 1; i < pManager.ParamCount; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Grid", "S", "The created grid", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData("Grid", LegacyStubHelpers.PlaceholderGoo("Grid", InstanceGuid, DA.Iteration, "Phase 4 stub"));
        }

        public override Guid ComponentGuid => new Guid("{b618569a-868d-4a88-a035-faa1416a841f}");
    }

    /// <summary>
    /// Tabbed View Beta variant (Names + Text Size + Tab N variable params). Same
    /// variable-parameter shape as the current TabContainer so .gh files saved
    /// against this GUID with multiple Tab 1/Tab 2 inputs deserialize cleanly.
    /// </summary>
    public class TabbedView_Beta1_DEPRECATED : GH_Component, IGH_VariableParameterComponent
    {
        public TabbedView_Beta1_DEPRECATED()
            : base("Tabbed View", "Tabs", "Creates a tabbed view containing other UI elements.", "Human UI", "UI Containers") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Tab Names", "N", "The names of the tabs.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddNumberParameter("Tab Text Size", "S", "The text size of the tabs.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddGenericParameter("Tab 0", "T0", "The UI elements for tab 0.", GH_ParamAccess.list);
            pManager[2].Optional = true;
            VariableParameterMaintenance();
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tabs", "T", "The created tab container", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData("Tabs", LegacyStubHelpers.PlaceholderGoo("Tabs", InstanceGuid, DA.Iteration, "Phase 4 stub"));
        }

        public override Guid ComponentGuid => new Guid("{669ed7cd-5b59-4484-b179-4e8934ab39b3}");

        public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input && index >= 2;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input && index >= 2 && Params.Input.Count > 3;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var p = new Param_GenericObject { Optional = true };
            Params.RegisterInputParam(p, index);
            return p;
        }
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance()
        {
            for (int i = 2; i < Params.Input.Count; i++)
            {
                var p = Params.Input[i];
                p.NickName = $"T{i - 2}";
                p.Name = $"Tab {i - 2}";
                p.Access = GH_ParamAccess.list;
                p.Optional = true;
                p.DataMapping = GH_DataMapping.Flatten;
            }
        }
    }

    /// <summary>
    /// Tabbed View earliest variant (Names + Tab N). Variable-parameter so old
    /// .gh files with multiple tab inputs deserialize cleanly.
    /// </summary>
    public class TabbedView_Beta0_DEPRECATED : GH_Component, IGH_VariableParameterComponent
    {
        public TabbedView_Beta0_DEPRECATED()
            : base("Tabbed View", "Tabs", "Creates a tabbed view containing other UI elements.", "Human UI", "UI Containers") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Tab Names", "N", "The names of the tabs.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddGenericParameter("Tab 0", "T0", "The UI elements for tab 0.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            VariableParameterMaintenance();
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tabs", "T", "The created tab container", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData("Tabs", LegacyStubHelpers.PlaceholderGoo("Tabs", InstanceGuid, DA.Iteration, "Phase 4 stub"));
        }

        public override Guid ComponentGuid => new Guid("{df93e843-3893-4ffc-b2e3-666190768b8e}");

        public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input && index >= 1;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input && index >= 1 && Params.Input.Count > 2;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var p = new Param_GenericObject { Optional = true };
            Params.RegisterInputParam(p, index);
            return p;
        }
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance()
        {
            for (int i = 1; i < Params.Input.Count; i++)
            {
                var p = Params.Input[i];
                p.NickName = $"T{i - 1}";
                p.Name = $"Tab {i - 1}";
                p.Access = GH_ParamAccess.list;
                p.Optional = true;
                p.DataMapping = GH_DataMapping.Flatten;
            }
        }
    }

    /// <summary>Create Objects from XAML. Eto has no XAML loader, so this is a permanent stub.</summary>
    public class CreateObjectsFromXaml_DEPRECATED : GH_Component
    {
        public CreateObjectsFromXaml_DEPRECATED()
            : base("Create Objects from XAML", "XAML", "Create UI Objects from a XAML string.", "Human UI", "UI Elements") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("XAML", "X", "The XAML string to create UI elements from.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "The created UI element", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Create Objects from XAML is not supported in the Eto port (no XAML loader on macOS).");
            DA.SetData("Object", LegacyStubHelpers.PlaceholderGoo("XAML", InstanceGuid, DA.Iteration, "not supported"));
        }

        public override Guid ComponentGuid => new Guid("{fd2eb7a5-9db0-4688-ad19-3736eb4fb182}");
    }
}

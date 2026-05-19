using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Scrollable stack of Eto CheckBoxes — Eto's CheckBoxList renders horizontally
    /// only, so HumanUI's vertically-scrolling checklist composes individual
    /// CheckBoxes inside a Scrollable. Each CheckBox holds the item label as Text;
    /// SetChecklist / ValueListener walk the stack to read state.
    /// </summary>
    public class CreateCheckList_Component : GH_Component
    {
        public CreateCheckList_Component()
            : base("Create Checklist", "Checklist",
                "Creates a listbox containing checkboxes.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Checklist Items", "L", "The initial list of options to display in the checklist.", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Selected", "S", "The initially selected state of all boxes. Defaults to unchecked for all.", GH_ParamAccess.list, false);
            pManager.AddNumberParameter("Height", "H", "Optional checklist box height in pixels.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Checklist", "CL", "The checklist object", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var listItems = new List<string>();
            var selected = new List<bool>();
            double height = 100;
            if (!DA.GetDataList("Checklist Items", listItems)) return;
            DA.GetDataList("Selected", selected);

            var scroll = new Scrollable { ID = "GH_Checklist" };
            var stack = BuildChecklistContent(listItems, selected);
            scroll.Content = stack;
            if (DA.GetData("Height", ref height)) scroll.Height = (int)height;

            DA.SetData("Checklist", new UIElement_Goo(scroll, "Checklist", InstanceGuid, DA.Iteration));
        }

        internal static StackLayout BuildChecklistContent(List<string> listItems, List<bool> selected)
        {
            var stack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Spacing = 2,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                ID = "GH_ChecklistItems",
            };
            for (int i = 0; i < listItems.Count; i++)
            {
                bool isSel = (selected != null && selected.Count > 0) && selected[i % selected.Count];
                stack.Items.Add(new CheckBox { Text = listItems[i], Checked = isSel });
            }
            return stack;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.createChecklist;

        public override Guid ComponentGuid => new Guid("{6e21dbe5-ecb8-4530-8a22-7cd713cf40d5}");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
#if HUI_WINDOWS
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;
#endif

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Composite drop-down menu: produces a StackLayout containing (optional Label) +
    /// Eto DropDown. SetList walks into the layout to find the DropDown for updates.
    /// </summary>
    public class CreatePullDown_Component : GH_Component
    {
        internal const string IdWithLabel = "GH_PullDown_Label";
        internal const string IdNoLabel = "GH_PullDown_NoLabel";

        private bool showLabel = true;

        public CreatePullDown_Component()
            : base("Create Pulldown Menu", "Pulldown",
                "Creates a pulldown menu from which items can be selected.",
                "Human UI", "UI Elements")
        {
        }

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Show Label", Menu_ShowLabelClicked, true, showLabel)
                .ToolTipText = "When checked, the UI Element will include the supplied label.";
        }
#endif

        public void Menu_ShowLabelClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Show Label Toggle");
            showLabel = !showLabel;
            ExpireSolution(true);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("showLabel", showLabel);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("showLabel", ref showLabel);
            return base.Read(reader);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Label", "L", "Optional label for the Text Box", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("List Items", "L", "The initial list of options to display in the list.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Selected Index", "I", "The initially selected index. Defaults to the first item.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Pulldown", "PD", "The pulldown object", GH_ParamAccess.list);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string label = "";
            DA.GetData("Label", ref label);

            var listItems = new List<object>();
            int selectedIndex = 0;
            if (!DA.GetDataList("List Items", listItems)) return;
            bool selectedIndexSupplied = DA.GetData("Selected Index", ref selectedIndex);

            // Honour GH_ValueList inputs (either wired directly or wrapped in GH_ObjectWrapper).
            // Each one produces its own pulldown; if there are none we fall back to the
            // straight item list.
            var valLists = Params.Input[1].Sources.OfType<GH_ValueList>().ToList();
            if (valLists.Count == 0)
            {
                foreach (object o in listItems)
                {
                    if (o is GH_ObjectWrapper wrapper && wrapper.Value is GH_ValueList vl)
                        valLists.Add(vl);
                }
            }

            if (valLists.Count == 0)
            {
                EmitPulldown(DA, label, listItems.Select(o => o?.ToString() ?? string.Empty), selectedIndex);
            }
            else
            {
                foreach (var vl in valLists)
                {
                    var values = vl.ListItems.Select(li => li.Name).ToList();
                    int idx = selectedIndexSupplied ? selectedIndex : vl.ListItems.IndexOf(vl.FirstSelectedItem);
                    EmitPulldown(DA, label, values, idx);
                }
            }
        }

        private void EmitPulldown(IGH_DataAccess DA, string label, IEnumerable<string> items, int selectedIndex)
        {
            var dropdown = new DropDown();
            foreach (var item in items)
                dropdown.Items.Add(new ListItem { Text = item });
            if (selectedIndex >= 0 && selectedIndex < dropdown.Items.Count)
                dropdown.SelectedIndex = selectedIndex;

            var stack = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 4,
            };

            bool labeled = !string.IsNullOrWhiteSpace(label) && showLabel;
            if (labeled)
            {
                stack.Items.Add(new StackLayoutItem(new Label { Text = label }, VerticalAlignment.Center));
                stack.ID = IdWithLabel;
            }
            else
            {
                stack.ID = IdNoLabel;
            }
            stack.Items.Add(new StackLayoutItem(dropdown, VerticalAlignment.Center, expand: true));

            DA.SetData("Pulldown", new UIElement_Goo(stack, $"Pulldown: {label}", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreatePullDown;

        public override Guid ComponentGuid => new Guid("{fc6ae741-ecd1-432f-abb4-36b3f439c6f5}");
    }
}

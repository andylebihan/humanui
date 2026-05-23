using System;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
#if HUI_WINDOWS
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;
#endif

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Composite text-entry control. The produced UIElement_Goo wraps a StackLayout
    /// holding (optional Label) + TextBox + (optional Button). SetTextBox /
    /// ValueListener walk into the layout via HUI_Util.findTextBox.
    /// </summary>
    public class CreateTextBox_Component : GH_Component
    {
        // Tag values applied to the wrapping StackLayout's ID so downstream components
        // (Set, ValueListener) can distinguish button-vs-no-button textboxes.
        internal const string IdWithButton = "GH_TextBox";
        internal const string IdNoButton = "GH_TextBox_NoButton";

        private bool showLabel = true;
        private bool enterEvent;
        private bool enterMenuEnabled;

        public CreateTextBox_Component()
            : base("Create Text Box", "TextBox",
                "Create a box for text entry, with a button to pass its value.",
                "Human UI", "UI Elements")
        {
        }

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Show Label", Menu_ShowLabelClicked, true, showLabel)
                .ToolTipText = "When checked, the UI Element will include the supplied label.";

            var enterItem = GH_DocumentObject.Menu_AppendItem(menu, "Use Enter to submit", Menu_EnterEventClicked, true, enterEvent);
            if (!enterMenuEnabled)
            {
                enterItem.Enabled = false;
                enterItem.Checked = true;
            }
        }
#endif

        private void Menu_EnterEventClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Enter Event Toggle");
            enterEvent = !enterEvent;
            ExpireSolution(true);
        }

        private void Menu_ShowLabelClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Show Label Toggle");
            showLabel = !showLabel;
            ExpireSolution(true);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("showLabel", showLabel);
            writer.SetBoolean("enterEvent", enterEvent);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetBoolean("showLabel", ref showLabel);
            reader.TryGetBoolean("enterEvent", ref enterEvent);
            return base.Read(reader);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Label", "L", "Optional label for the Text Box", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Default Text", "D", "The starting text in the text box", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Update Button", "U", "Set to true to associate text box \nwith a button for updates. Otherwise event listening will \nassociate with every change in text box content.", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Box", "TB", "The created text box.", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool includeButton = true;
            string label = "";
            string defaultValue = "";
            if (!DA.GetData("Default Text", ref defaultValue)) return;
            DA.GetData("Label", ref label);
            DA.GetData("Update Button", ref includeButton);

            var tb = new TextBox { Text = defaultValue };
            var labelCtl = new Label { Text = label };
            var btn = new Button { Width = 50, Text = "Set" };

            var stack = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 4,
            };

            if (!string.IsNullOrWhiteSpace(label) && showLabel)
                stack.Items.Add(new StackLayoutItem(labelCtl, VerticalAlignment.Center));

            stack.Items.Add(new StackLayoutItem(tb, VerticalAlignment.Center, expand: true));

            if (includeButton)
            {
                stack.Items.Add(new StackLayoutItem(btn, VerticalAlignment.Center));
                stack.ID = IdWithButton;
                enterMenuEnabled = false;
            }
            else
            {
                stack.ID = IdNoButton;
                enterMenuEnabled = true;
            }

            if (enterEvent || !enterMenuEnabled)
                tb.Tag = "enterEvent";

            DA.SetData("Text Box", new UIElement_Goo(stack, $"TextBox: {label}", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateTextBox;

        public override Guid ComponentGuid => new Guid("{41A3A0D8-E0F4-4B48-88B3-BF87D79A3CFD}");
    }
}

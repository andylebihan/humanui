using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Creates an on/off toggle. Eto.Forms has no native ToggleSwitch like MahApps,
    /// so the port falls back to a CheckBox. The On/Off labels become the checked
    /// vs unchecked Text; when the user has only set one of them we still flip
    /// between the two on state change.
    /// </summary>
    public class CreateToggle_Component : GH_Component
    {
        public CreateToggle_Component()
            : base("Create Toggle", "Toggle",
                "Creates an on-off toggle.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Default", "D", "Whether toggle is on by default", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Label", "L", "The optional label for the toggle", GH_ParamAccess.item);
            pManager.AddTextParameter("On Label", "On", "The text to display when on", GH_ParamAccess.item);
            pManager.AddTextParameter("Off Label", "Off", "The text to display when off", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toggle", "T", "The Toggle Element.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool defaultVal = false;
            string label = "", onVal = "", offVal = "";

            bool hasLabel = DA.GetData("Label", ref label);
            bool hasOnVal = DA.GetData("On Label", ref onVal);
            bool hasOffVal = DA.GetData("Off Label", ref offVal);
            DA.GetData("Default", ref defaultVal);

            // The Header label, if provided, wraps the toggle in a horizontal stack.
            var cb = new CheckBox { Checked = defaultVal };
            cb.ID = "GH_Toggle";

            string onText = hasOnVal ? onVal : (hasLabel ? label : "On");
            string offText = hasOffVal ? offVal : (hasLabel ? label : "Off");
            cb.Tag = new[] { offText, onText };

            cb.Text = (cb.Checked == true) ? onText : offText;
            cb.CheckedChanged += (_, _) =>
            {
                cb.Text = (cb.Checked == true) ? onText : offText;
            };

            if (hasLabel)
            {
                var stack = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    ID = "GH_Toggle_Label",
                    Tag = "Toggle Switch",
                };
                stack.Items.Add(new Label { Text = label });
                stack.Items.Add(cb);
                DA.SetData("Toggle", new UIElement_Goo(stack, "Toggle Switch", InstanceGuid, DA.Iteration));
            }
            else
            {
                DA.SetData("Toggle", new UIElement_Goo(cb, "Toggle Switch", InstanceGuid, DA.Iteration));
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.createToggle;

        public override Guid ComponentGuid => new Guid("{baf92e4a-c2ca-47d1-99c1-5e78631994d5}");
    }
}

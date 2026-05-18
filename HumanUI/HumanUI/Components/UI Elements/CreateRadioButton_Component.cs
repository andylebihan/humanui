using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Creates a single Eto RadioButton. Eto wires group membership through a
    /// constructor-time `controller` reference (unlike WPF's string GroupName),
    /// so we keep a process-wide map of group-name → controller. When the same
    /// group name appears again, the new RadioButton is constructed against the
    /// existing controller. Stale entries (window closed, controller disposed)
    /// are replaced on the next create.
    /// </summary>
    public class CreateRadioButton_Component : GH_Component
    {
        public CreateRadioButton_Component()
            : base("Create Radio Button", "RadioBtn",
                "Creates a single radio button. Be sure to assign a radio button group for proper switching behavior",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        private static readonly Dictionary<string, RadioButton> _groupControllers = new();

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Radio Button Name", "N", "The name of the radio button.", GH_ParamAccess.item);
            pManager.AddTextParameter("Radio Button Group", "G", "The group the radio button belongs to. \n Only one button in a group can be selected at a time.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Initially Selected", "S", "Whether or not this button should be selected initially.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Radio Button", "RB", "The created radio button.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string btnName = "";
            string groupName = "";
            if (!DA.GetData("Radio Button Name", ref btnName)) return;
            if (!DA.GetData("Radio Button Group", ref groupName)) return;
            bool isSelected = false;
            DA.GetData("Initially Selected", ref isSelected);

            RadioButton rb = AcquireGroupedRadio(groupName);
            rb.Text = btnName;
            rb.Checked = isSelected;
            // Tag holds the group name so SetRadioButton / ValueListener can disambiguate
            // composite controls if we ever wrap them.
            rb.Tag = groupName;

            DA.SetData("Radio Button", new UIElement_Goo(rb, $"Radio Button: {btnName}", InstanceGuid, DA.Iteration));
        }

        private static RadioButton AcquireGroupedRadio(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return new RadioButton();

            if (_groupControllers.TryGetValue(groupName, out var controller) && !controller.IsDisposed)
            {
                return new RadioButton(controller);
            }

            var fresh = new RadioButton();
            _groupControllers[groupName] = fresh;
            return fresh;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateRadioButton;

        public override Guid ComponentGuid => new Guid("{5B7E6AF2-EB03-477A-ABEB-3C0065593296}");
    }
}

using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Second-generation pulldown menu shipped after the original Beta 0.6.x line. Kept
    /// alive as a hidden, obsolete component so .gh files that ended up at this GUID
    /// resolve without going through the upgrader chain.
    /// </summary>
    public class CreatePullDown_Component_ALSO_DEPRECATED : GH_Component
    {
        public CreatePullDown_Component_ALSO_DEPRECATED()
            : base("Create Pulldown Menu", "Pulldown",
                "Creates a pulldown menu from which items can be selected.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("List Items", "L", "The initial list of options to display in the list.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Selected Index", "I", "The initially selected index. Defaults to the first item.", GH_ParamAccess.item, 0);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Pulldown", "PD", "The pulldown object", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var listItems = new List<object>();
            int selectedIndex = 0;
            if (!DA.GetDataList("List Items", listItems)) return;
            DA.GetData("Selected Index", ref selectedIndex);

            var dropdown = new DropDown();
            foreach (var item in listItems)
            {
                string text = item is GH_String gs ? gs.Value :
                              item is GH_ObjectWrapper wrap ? wrap.Value?.ToString() ?? string.Empty :
                              item?.ToString() ?? string.Empty;
                dropdown.Items.Add(new ListItem { Text = text });
            }
            if (selectedIndex >= 0 && selectedIndex < dropdown.Items.Count)
                dropdown.SelectedIndex = selectedIndex;

            DA.SetData("Pulldown", new UIElement_Goo(dropdown, "Pulldown", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreatePullDown;

        public override Guid ComponentGuid => new Guid("{8F5B1D66-DE73-47A2-9678-9E59CEA106C0}");
    }
}

using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Legacy pulldown menu shipped with Human UI Beta 0.6.6 (and earlier). Kept alive
    /// as a hidden, obsolete component so .gh files saved at that version resolve
    /// without going through the upgrader chain. Internally it produces the same Eto
    /// DropDown the current CreatePullDown_Component does; the only differences are
    /// the legacy 2-input shape and the item-typed output.
    /// </summary>
    public class CreatePullDown_Component_DEPRECATED : GH_Component
    {
        public CreatePullDown_Component_DEPRECATED()
            : base("Create Pulldown Menu", "Pulldown",
                "Creates a pulldown menu from which items can be selected.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("List Items", "L", "The initial list of options to display in the list.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Selected Index", "I", "The initially selected index. Defaults to the first item.", GH_ParamAccess.item, 0);
        }

        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override bool Obsolete => true;

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Pulldown", "PD", "The pulldown object", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var listItems = new List<string>();
            int selectedIndex = 0;
            if (!DA.GetDataList("List Items", listItems)) return;
            DA.GetData("Selected Index", ref selectedIndex);

            var dropdown = new DropDown();
            foreach (var item in listItems)
                dropdown.Items.Add(new ListItem { Text = item });
            if (selectedIndex >= 0 && selectedIndex < dropdown.Items.Count)
                dropdown.SelectedIndex = selectedIndex;

            DA.SetData("Pulldown", new UIElement_Goo(dropdown, "Pulldown", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreatePullDown;

        public override Guid ComponentGuid => new Guid("{1CA8D537-EF52-487C-828D-034B1BCA7361}");
    }
}

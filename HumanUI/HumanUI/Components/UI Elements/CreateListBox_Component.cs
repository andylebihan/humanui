using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    public class CreateListBox_Component : GH_Component
    {
        public CreateListBox_Component()
            : base("Create List Box", "ListBox",
                "Creates a list box from which items can be selected.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("List Items", "L", "The initial list of options to display in the list.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Selected Index", "I", "The initially selected index. Defaults to the first item.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Height", "H", "List box height in pixels.", GH_ParamAccess.item, 100);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("List Box", "LB", "The list box object", GH_ParamAccess.item);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var listItems = new List<string>();
            int selectedIndex = 0;
            double height = 100;
            if (!DA.GetDataList("List Items", listItems)) return;
            DA.GetData("Height", ref height);
            DA.GetData("Selected Index", ref selectedIndex);

            var lb = new ListBox { Height = (int)height };
            foreach (var item in listItems)
                lb.Items.Add(new ListItem { Text = item });

            if (selectedIndex >= 0 && selectedIndex < lb.Items.Count)
                lb.SelectedIndex = selectedIndex;

            DA.SetData("List Box", new UIElement_Goo(lb, "List Box", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateListBox;

        public override Guid ComponentGuid => new Guid("{2dddb05e-5503-4506-8f9e-5c0f4c35f8b0}");
    }
}

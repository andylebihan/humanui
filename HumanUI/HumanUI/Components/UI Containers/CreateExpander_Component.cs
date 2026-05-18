using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Containers
{
    public class CreateExpander_Component : GH_Component
    {
        public CreateExpander_Component()
          : base("Create Expander", "Expander",
              "A collapsible expander for content",
              "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to put in the expander", GH_ParamAccess.list);
            pManager.AddTextParameter("Header", "H", "The text to display in the expander header", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Open", "O", "The starting state of the expander - true for open, false for collapsed", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Expander", "E", "The expander element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool open = true;
            string header = "";
            DA.GetData("Open", ref open);
            DA.GetData("Header", ref header);
            var elementsToAdd = new List<UIElement_Goo>();
            if (!DA.GetDataList("UI Elements", elementsToAdd)) return;

            var stack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 4,
                ID = "GH_Stack",
            };
            foreach (var u in elementsToAdd)
            {
                if (u?.element == null) continue;
                HUI_Util.removeParent(u.element);
                stack.Items.Add(new StackLayoutItem(u.element, HorizontalAlignment.Stretch));
            }

            var expander = new Expander
            {
                Header = new Label { Text = header },
                Content = stack,
                Expanded = open,
            };

            DA.SetData("Expander", new UIElement_Goo(expander, "Expander", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Expander;

        public override Guid ComponentGuid => new Guid("{3cec9da4-eb68-4063-9325-57850921a8b2}");
    }
}

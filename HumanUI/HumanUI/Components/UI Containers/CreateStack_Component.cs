using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Containers
{
    public class CreateStack_Component : GH_Component
    {
        public CreateStack_Component()
            : base("Create Stack", "Stack",
                "Creates a group of UI elements stacked vertically or horizontally.",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to group together", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Horizontal", "H", "Set to true for horizontal arrangement; false for vertical.", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Stack", "S", "The combined group of elements", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var elementsToAdd = new List<UIElement_Goo>();
            bool horiz = true;
            if (!DA.GetDataList("UI Elements", elementsToAdd)) return;
            DA.GetData("Horizontal", ref horiz);

            var stack = new StackLayout
            {
                Orientation = horiz ? Orientation.Horizontal : Orientation.Vertical,
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

            DA.SetData("Stack", new UIElement_Goo(stack, "Stack", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateStack;

        public override Guid ComponentGuid => new Guid("{30edb451-7870-4204-a6a3-e38745f42590}");
    }
}

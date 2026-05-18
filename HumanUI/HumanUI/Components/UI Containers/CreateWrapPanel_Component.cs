using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Wrapping layout. Eto.Forms has no direct WrapPanel equivalent; the Phase 2 port
    /// renders as a non-wrapping StackLayout so existing files load without errors. A
    /// follow-up pass can swap in a custom wrap-laying Drawable / DynamicLayout when
    /// the visual difference matters for a user.
    /// </summary>
    public class CreateWrapPanel_Component : GH_Component
    {
        public CreateWrapPanel_Component()
            : base("Create WrapPanel", "WrapPanel",
                "Creates a group of UI elements WrapPaneled vertically or horizontally.",
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
            pManager.AddGenericParameter("WrapPanel", "S", "The combined group of elements", GH_ParamAccess.item);
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
                ID = "GH_WrapPanel",
            };
            foreach (var u in elementsToAdd)
            {
                if (u?.element == null) continue;
                HUI_Util.removeParent(u.element);
                stack.Items.Add(new StackLayoutItem(u.element, HorizontalAlignment.Stretch));
            }

            DA.SetData("WrapPanel", new UIElement_Goo(stack, "WrapPanel", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateWrapPanel;

        public override Guid ComponentGuid => new Guid("{00F0E6DF-7227-42A7-B148-A9A4E245C928}");
    }
}

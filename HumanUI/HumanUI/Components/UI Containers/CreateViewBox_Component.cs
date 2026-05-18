using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Sized container. WPF's Viewbox scaled its contents; Eto.Forms has no automatic
    /// scale-to-fit primitive so the Phase 2 port produces a fixed-size Panel containing
    /// a StackLayout. Visual scaling falls back to natural sizing of the child elements.
    /// </summary>
    public class CreateViewBox_Component : GH_Component
    {
        public CreateViewBox_Component()
            : base("Create View Box", "ViewBox",
                "Scale a group of UI Elements by placing them in a ViewBox.",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to scale", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "The width of the viewbox", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The height of the viewbox", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ViewBox", "VB", "The viewbox", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var elementsToAdd = new List<UIElement_Goo>();
            double width = 0, height = 0;
            if (!DA.GetDataList("UI Elements", elementsToAdd)) return;
            if (!DA.GetData("Width", ref width)) return;
            if (!DA.GetData("Height", ref height)) return;

            var stack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Spacing = 4,
            };
            foreach (var u in elementsToAdd)
            {
                if (u?.element == null) continue;
                HUI_Util.removeParent(u.element);
                stack.Items.Add(new StackLayoutItem(u.element, HorizontalAlignment.Stretch));
            }

            var panel = new Panel
            {
                Width = (int)width,
                Height = (int)height,
                Content = stack,
            };

            DA.SetData("ViewBox", new UIElement_Goo(panel, "ViewBox", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateViewBox;

        public override Guid ComponentGuid => new Guid("{51123304-F2C4-41EC-B31F-FD8C50E1A113}");
    }
}

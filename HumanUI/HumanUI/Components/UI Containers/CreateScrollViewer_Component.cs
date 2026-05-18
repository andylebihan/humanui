using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components.UI_Containers
{
    public class CreateScrollViewer_Component : GH_Component
    {
        public CreateScrollViewer_Component()
            : base("Create Scroll Viewer", "ScrollViewer",
                "Allows an element to scroll independently of the rest of the window",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Elements", "E", "The UI elements to put in the scroll group", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Horizontal Scroll Bar Visibility", "HV", "Whether or not to show the horizontal scroll bar", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Vertical Scroll Bar Visibility", "VV", "Whether or not to show the vertical scroll bar", GH_ParamAccess.item, 0);
            var horizViz = (Param_Integer)pManager[1];
            var vertViz = (Param_Integer)pManager[2];
            horizViz.AddNamedValue("Auto", 0);
            horizViz.AddNamedValue("Show", 1);
            horizViz.AddNamedValue("Hide", 2);
            vertViz.AddNamedValue("Auto", 0);
            vertViz.AddNamedValue("Show", 1);
            vertViz.AddNamedValue("Hide", 2);

            pManager.AddNumberParameter("Width", "W", "The width of the scrolling revion", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "The height of the scrolling revion", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ScrollViewer", "S", "The scrolling group of elements", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var elementsToAdd = new List<UIElement_Goo>();
            int horizViz = 0, vertViz = 0;
            double height = -1, width = -1;
            if (!DA.GetDataList("UI Elements", elementsToAdd)) return;
            DA.GetData("Horizontal Scroll Bar Visibility", ref horizViz);
            DA.GetData("Vertical Scroll Bar Visibility", ref vertViz);
            bool hasHeight = DA.GetData("Height", ref height);
            bool hasWidth = DA.GetData("Width", ref width);

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

            // Eto's Scrollable always provides scrollbars when needed; the platform
            // backend decides Auto vs. Show. There's no first-class way to force-hide a
            // scrollbar across all backends, so Hide is treated as "let the platform
            // decide" -- closest match given the API constraint.
            var sv = new Scrollable
            {
                Border = BorderType.None,
                ExpandContentWidth = true,
                Content = stack,
            };
            if (hasWidth) sv.Width = (int)width;
            if (hasHeight) sv.Height = (int)height;

            DA.SetData("ScrollViewer", new UIElement_Goo(sv, "ScrollViewer", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ScrollViewer;

        public override Guid ComponentGuid => new Guid("{038A7B79-5443-4CA1-BF88-C0CD3894C357}");
    }
}

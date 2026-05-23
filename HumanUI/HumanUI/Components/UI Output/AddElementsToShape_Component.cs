using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components
{
    /// <summary>
    /// Phase 5 stub. The original WPF component added arbitrary FrameworkElements
    /// (labels, text blocks) on top of a WPF Shape Grid. The Eto port can't mix
    /// Eto controls into a WPF visual tree without per-control unwrapping logic,
    /// so this component is preserved as a hidden no-op: the GUID and parameter
    /// shape match the original so .gh files load, and a warning surfaces when
    /// the user tries to invoke it.
    /// </summary>
    public class AddElementsToShape : GH_Component
    {
        public AddElementsToShape()
            : base("Add Elements to Shape(s)", "AddElem2Shape",
                "Put UI Elements (like text!) over the top of a shape/shapes element (Eto preview: not supported).",
                "Human UI", "UI Output")
        {
        }

        public override Guid ComponentGuid => new Guid("{DB39D16E-1BE9-41C5-B69F-7B52F38E1EF0}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to add to", "S", "The Shape object to add element(s) to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Element to add", "E", "The element(s) to add to the shapes.", GH_ParamAccess.list);
            pManager.AddPointParameter("Element Location", "L", "The point at which to position the element", GH_ParamAccess.list);
            pManager.AddNumberParameter("Scale", "Sc", "The scale", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.AddElementsToShapes;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object shape = null;
            var elems = new List<UIElement_Goo>();
            var pts = new List<Point3d>();
            double scale = 1.0;
            DA.GetData("Shape to add to", ref shape);
            DA.GetDataList("Element to add", elems);
            DA.GetDataList("Element Location", pts);
            DA.GetData("Scale", ref scale);

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "AddElementsToShape is not supported in the Eto port — labels cannot be hosted inside the WPF Shape grid without per-control adapter logic.");
        }
    }
}

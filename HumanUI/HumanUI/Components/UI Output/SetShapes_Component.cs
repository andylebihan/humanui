using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Phase 5 stub. Paired with CreateMultiShape_Component which is deferred.
    /// </summary>
    public class SetShapes_Component : GH_Component
    {
        public SetShapes_Component()
            : base("Set Shapes", "SetShapes",
                "Replace an existing shape in the window",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "The shapes container to update", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curves", "SC", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Colors", "FC", "The fill colors. Leave empty for no fill", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weights", "SW", "The stroke weights. Leave empty or set to 0 for no stroke.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Colors", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Optional output width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Optional output height.", GH_ParamAccess.item);
            for (int i = 2; i < pManager.ParamCount; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Set Shapes is a Phase 5 stub in this Eto preview.");
        }

        public override Guid ComponentGuid => new Guid("{EDC4A536-7412-46F2-B56F-6D8668D6B983}");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Grasshopper.Kernel;
using HumanUI.Components.UI_Elements;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Replace the curves / fills / strokes inside an existing Create Shapes
    /// container. The container is the inner WPF Grid (ClickableShapeGrid)
    /// reachable through HUI_WpfHost.Unwrap.
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
            object shapeObj = null;
            var crvs = new List<Curve>();
            var fillCols = new List<System.Drawing.Color>();
            var strokeWeights = new List<double>();
            var strokeCols = new List<System.Drawing.Color>();
            double scale = 1.0;
            double width = 0, height = 0;
            if (!DA.GetData("Shape to Modify", ref shapeObj)) return;
            if (!DA.GetDataList("Shape Curves", crvs)) return;
            DA.GetDataList("Fill Colors", fillCols);
            DA.GetDataList("Stroke Weights", strokeWeights);
            DA.GetDataList("Stroke Colors", strokeCols);
            DA.GetData("Scale", ref scale);
            // Per-shape lookups below use Count > 0 rather than the GetDataList
            // bool — an upstream wire can be connected but produce nothing,
            // which would crash i % 0 in the loop. Mirrors the same defensive
            // pattern used in CreateMultiShape_Component.

            var grid = HUI_WpfHost.Unwrap<Grid>(shapeObj);
            if (grid == null) return;

            // Clear existing Paths (preserve any non-Shape children — e.g., the
            // legacy AddElementsToShape labels, even though that component is
            // stubbed in the Eto port).
            var oldPaths = grid.Children.OfType<Path>().ToList();
            foreach (var p in oldPaths) grid.Children.Remove(p);

            for (int i = 0; i < crvs.Count; i++)
            {
                var path = new Path { Data = CreateShape_Component.PathGeomFromCrvs(new List<Curve> { crvs[i] }, scale, false) };
                if (fillCols.Count > 0) path.Fill = new SolidColorBrush(HUI_Util.ToMediaColor(fillCols[i % fillCols.Count]));
                if (strokeWeights.Count > 0) path.StrokeThickness = strokeWeights[i % strokeWeights.Count];
                if (strokeCols.Count > 0) path.Stroke = new SolidColorBrush(HUI_Util.ToMediaColor(strokeCols[i % strokeCols.Count]));
                grid.Children.Add(path);
            }
            if (DA.GetData("Width", ref width)) grid.Width = width;
            if (DA.GetData("Height", ref height)) grid.Height = height;
        }

        public override Guid ComponentGuid => new Guid("{EDC4A536-7412-46F2-B56F-6D8668D6B983}");
    }
}

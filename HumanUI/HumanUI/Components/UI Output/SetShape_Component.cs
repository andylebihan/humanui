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
    /// Update an existing CreateShape output: swap geometry, fill, stroke, and
    /// optionally container size. The Shape's HUI_WpfHost is unwrapped to the
    /// inner WPF Grid → Path.
    /// </summary>
    public class SetShape_Component : GH_Component
    {
        public SetShape_Component()
            : base("Set Shape", "SetShape",
                "Modify an existing shape",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape to Modify", "S", "The shape element to modify", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shape Curve", "SC", "The new curve outline", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddColourParameter("Fill Color", "FC", "Fill color", GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager.AddNumberParameter("Stroke Weight", "SW", "Stroke weight", GH_ParamAccess.item);
            pManager[3].Optional = true;
            pManager.AddColourParameter("Stroke Color", "SC", "Stroke color", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager.AddNumberParameter("Scale", "Scl", "Scale factor", GH_ParamAccess.item, 1.0);
            pManager[5].Optional = true;
            pManager.AddIntegerParameter("Width", "W", "Width", GH_ParamAccess.item);
            pManager[6].Optional = true;
            pManager.AddIntegerParameter("Height", "H", "Height", GH_ParamAccess.item);
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object shapeObj = null;
            var crvs = new List<Curve>();
            var fillCol = System.Drawing.Color.Transparent;
            double strokeWeight = 0;
            var strokeCol = System.Drawing.Color.Transparent;
            double scale = 1.0;
            int width = 0, height = 0;
            if (!DA.GetData("Shape to Modify", ref shapeObj)) return;
            bool hasCrvs = DA.GetDataList("Shape Curve", crvs);

            var grid = HUI_WpfHost.Unwrap<Grid>(shapeObj);
            if (grid == null) return;
            var path = grid.Children.OfType<Path>().FirstOrDefault();
            if (path == null) return;

            DA.GetData("Scale", ref scale);
            if (hasCrvs && crvs.Count > 0)
                path.Data = CreateShape_Component.PathGeomFromCrvs(crvs, scale, false);
            if (DA.GetData("Fill Color", ref fillCol))
                path.Fill = new SolidColorBrush(HUI_Util.ToMediaColor(fillCol));
            if (DA.GetData("Stroke Weight", ref strokeWeight))
                path.StrokeThickness = strokeWeight;
            if (DA.GetData("Stroke Color", ref strokeCol))
                path.Stroke = new SolidColorBrush(HUI_Util.ToMediaColor(strokeCol));
            if (DA.GetData("Width", ref width)) grid.Width = width;
            if (DA.GetData("Height", ref height)) grid.Height = height;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetShape;

        public override Guid ComponentGuid => new Guid("{f6881435-7de3-4098-ada8-f3068ed7331c}");
    }
}

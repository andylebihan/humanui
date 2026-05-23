using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// WPF Path-based shape, embedded in an Eto panel via HUI_WpfHost. The shape
    /// is drawn from one or more Rhino polyline curves, mirrored to match screen
    /// coordinates (+Y down) and optionally scaled. Internal curves act as
    /// holes via the standard WPF even-odd fill rule of Geometry.Parse.
    /// </summary>
    public class CreateShape_Component : GH_Component
    {
        public CreateShape_Component()
            : base("Create Shape", "Shape",
                "Creates a simple shape from a polyline",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shape", "S", "The shape to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill color. Leave empty for no fill", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weight. Leave empty or set to 0 for no stroke.", GH_ParamAccess.item);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke color", GH_ParamAccess.item, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Width", "W", "The width of the container for the shape. \nLeave blank to autosize.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Height", "H", "The height of the container for the shape. \nLeave blank to autosize.", GH_ParamAccess.item);
            for (int i = 1; i < 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var shapeCrvs = new List<Curve>();
            var fillCol = System.Drawing.Color.Transparent;
            double strokeWeight = 0;
            var strokeCol = System.Drawing.Color.Transparent;
            double scale = 1.0;
            int width = 0, height = 0;

            if (!DA.GetDataList("Shape", shapeCrvs)) return;
            DA.GetData("Scale", ref scale);

            var path = new Path { Data = PathGeomFromCrvs(shapeCrvs, scale, false) };
            if (DA.GetData("Fill Color", ref fillCol))
                path.Fill = new SolidColorBrush(HUI_Util.ToMediaColor(fillCol));
            if (DA.GetData("Stroke Weight", ref strokeWeight))
                path.StrokeThickness = strokeWeight;
            if (DA.GetData("Stroke Color", ref strokeCol))
                path.Stroke = new SolidColorBrush(HUI_Util.ToMediaColor(strokeCol));

            var g = new Grid();
            if (DA.GetData("Width", ref width)) g.Width = width;
            if (DA.GetData("Height", ref height)) g.Height = height;
            g.Children.Add(path);

            var host = new HUI_WpfHost(g);
            if (width > 0) host.Width = width;
            if (height > 0) host.Height = height;
            DA.SetData("Shape", new UIElement_Goo(host, "Shape", InstanceGuid, DA.Iteration));
        }

        /// <summary>
        /// Build a WPF Geometry from a list of Rhino curves by converting each to
        /// a polyline, mirroring to screen Y, and emitting SVG-style "M x,y x,y …"
        /// path data. The shared helper is reused by CreateMultiShape_Component.
        /// </summary>
        public static Geometry PathGeomFromCrvs(List<Curve> c, double scale, bool rebox)
        {
            var sb = new System.Text.StringBuilder();
            RebaseGeometry(c.OfType<GeometryBase>(), scale, rebox);
            foreach (var crv in c)
            {
                if (!crv.TryGetPolyline(out var pl))
                {
                    var p = crv.ToPolyline(0, 0, 0.1, 2.0, 0, 0, crv.GetLength() / 50, 0, true);
                    p?.TryGetPolyline(out pl);
                }
                if (pl == null) continue;
                sb.Append("M ");
                foreach (var pt in pl) sb.Append($"{pt.X:0.000},{pt.Y:0.000} ");
            }
            return Geometry.Parse(sb.ToString());
        }

        public static void RebaseGeometry(IEnumerable<GeometryBase> geo, double scale, bool rebox)
        {
            foreach (var g in geo)
            {
                g.Transform(Rhino.Geometry.Transform.Mirror(Plane.WorldZX));
                g.Transform(Rhino.Geometry.Transform.Scale(Point3d.Origin, scale));
            }
            if (rebox)
            {
                var b = BoundingBox.Empty;
                foreach (var g in geo) b.Union(g.GetBoundingBox(true));
                var boxBase = b.PointAt(0, 0, 0);
                foreach (var g in geo) g.Transform(Rhino.Geometry.Transform.Translation(new Vector3d(-boxBase)));
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShape;

        public override Guid ComponentGuid => new Guid("{0ab1c8a7-4182-4a7b-bda3-67c24677182c}");
    }
}

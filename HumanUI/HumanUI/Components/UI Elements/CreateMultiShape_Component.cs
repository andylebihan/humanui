using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Shapes;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Multi-path WPF shape composite: each input curve becomes its own Path
    /// (so per-shape fill / stroke styling is possible). Hosted inside an Eto
    /// panel via HUI_WpfHost. The ClickMode menu controls hit-testing behavior
    /// via the ClickableShapeGrid container — see ClickableShapeGrid.ClickMode
    /// for the per-mode semantics (Button / Toggle / Picker).
    /// </summary>
    public class CreateMultiShape_Component : GH_Component
    {
        internal ClickableShapeGrid.ClickMode clickMode;

        public CreateMultiShape_Component()
            : base("Create Shapes", "Shapes",
                "Creates shapes from a polylines",
                "Human UI", "UI Elements")
        {
            clickMode = ClickableShapeGrid.ClickMode.None;
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Shapes", "S", "The shapes to add as Polyline(s)", GH_ParamAccess.list);
            pManager.AddColourParameter("Fill Color", "FC", "The fill colors. Leave empty for no fill", GH_ParamAccess.list);
            pManager.AddNumberParameter("Stroke Weight", "SW", "The stroke weights. Leave empty or set to 0 for no stroke.", GH_ParamAccess.list);
            pManager.AddColourParameter("Stroke Color", "SC", "The stroke colors", GH_ParamAccess.list, System.Drawing.Color.Black);
            pManager.AddNumberParameter("Scale", "Scl", "Use this value to resize the shape.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Width", "W", "Optional output width.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Optional output height.", GH_ParamAccess.item);
            for (int i = 1; i < 7; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Shape", "S", "The created shape.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var crvs = new List<Curve>();
            var fillCols = new List<System.Drawing.Color>();
            var strokeWeights = new List<double>();
            var strokeCols = new List<System.Drawing.Color>();
            double scale = 1.0;
            double width = 0, height = 0;

            DA.GetDataList("Fill Color", fillCols);
            DA.GetDataList("Stroke Weight", strokeWeights);
            DA.GetDataList("Stroke Color", strokeCols);
            DA.GetData("Scale", ref scale);
            if (!DA.GetDataList("Shapes", crvs)) return;
            // GetDataList can return true while the inbound list is empty (e.g.,
            // a wire whose upstream evaluates to nothing), so per-shape lookups
            // are guarded by Count > 0 rather than the GetDataList bool to
            // avoid `i % 0` DivideByZero in the i-loop below.

            var g = new ClickableShapeGrid { clickMode = clickMode };
            if (DA.GetData("Width", ref width)) g.Width = width;
            if (DA.GetData("Height", ref height)) g.Height = height;

            for (int i = 0; i < crvs.Count; i++)
            {
                var path = new Path
                {
                    Data = CreateShape_Component.PathGeomFromCrvs(new List<Curve> { crvs[i] }, scale, false),
                };
                if (fillCols.Count > 0) path.Fill = new SolidColorBrush(HUI_Util.ToMediaColor(fillCols[i % fillCols.Count]));
                if (strokeWeights.Count > 0) path.StrokeThickness = strokeWeights[i % strokeWeights.Count];
                if (strokeCols.Count > 0) path.Stroke = new SolidColorBrush(HUI_Util.ToMediaColor(strokeCols[i % strokeCols.Count]));
                g.Children.Add(path);
            }

            DA.SetData("Shape", new UIElement_Goo(new HUI_WpfHost(g), "Shape", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateShapes;

        public override Guid ComponentGuid => new Guid("{94288CED-76F6-438A-9216-E60C8290F640}");

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ClickMode", (int)clickMode);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int v = -1;
            if (reader.TryGetInt32("ClickMode", ref v)) clickMode = (ClickableShapeGrid.ClickMode)v;
            UpdateMessage();
            return base.Read(reader);
        }

        private void UpdateMessage()
        {
            Message = clickMode switch
            {
                ClickableShapeGrid.ClickMode.ButtonMode => "Button Mode",
                ClickableShapeGrid.ClickMode.ToggleMode => "Toggle Mode",
                ClickableShapeGrid.ClickMode.PickerMode => "Picker Mode",
                _ => "",
            };
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Static", (_, _) => SetMode(ClickableShapeGrid.ClickMode.None), true, clickMode == ClickableShapeGrid.ClickMode.None);
            GH_DocumentObject.Menu_AppendItem(menu, "Button Mode", (_, _) => SetMode(ClickableShapeGrid.ClickMode.ButtonMode), true, clickMode == ClickableShapeGrid.ClickMode.ButtonMode);
            GH_DocumentObject.Menu_AppendItem(menu, "Toggle Mode", (_, _) => SetMode(ClickableShapeGrid.ClickMode.ToggleMode), true, clickMode == ClickableShapeGrid.ClickMode.ToggleMode);
            GH_DocumentObject.Menu_AppendItem(menu, "Picker Mode", (_, _) => SetMode(ClickableShapeGrid.ClickMode.PickerMode), true, clickMode == ClickableShapeGrid.ClickMode.PickerMode);
        }

        private void SetMode(ClickableShapeGrid.ClickMode m)
        {
            RecordUndoEvent("ClickMode change");
            clickMode = m;
            UpdateMessage();
            ExpireSolution(true);
        }
    }
}

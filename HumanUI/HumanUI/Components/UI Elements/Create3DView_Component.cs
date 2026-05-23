using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using HelixToolkit.Wpf;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Embeds a HelixToolkit.Wpf HelixViewport3D inside an Eto panel via
    /// HUI_WpfHost. The viewport keeps the original left-click orbit/pan
    /// gestures and renders the input meshes with per-mesh material colors.
    /// </summary>
    public class Create3DView_Component : GH_Component
    {
        public Create3DView_Component()
            : base("Create 3D View", "3DView",
                "Creates an orbitable 3d viewport with a custom-defined mesh",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh to display", "M", "The mesh(es) to display in the viewport", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The color with which to display the mesh.", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager.AddNumberParameter("View Width", "W", "The width of the 3d viewport", GH_ParamAccess.item, 300);
            pManager.AddNumberParameter("View Height", "H", "The height of the 3d viewport", GH_ParamAccess.item, 300);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("3DView", "V", "The 3D view containing your mesh.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var meshes = new List<Mesh>();
            var cols = new List<System.Drawing.Color>();
            double width = 300, height = 300;
            if (!DA.GetDataList("Mesh to display", meshes)) return;
            DA.GetDataList("Mesh Colors", cols);
            DA.GetData("View Width", ref width);
            DA.GetData("View Height", ref height);

            var vp3 = new HelixViewport3D
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Width = width,
                Height = height,
                ZoomExtentsWhenLoaded = true,
            };
            vp3.RotateGesture.MouseAction = System.Windows.Input.MouseAction.LeftClick;
            vp3.PanGesture.MouseAction = System.Windows.Input.MouseAction.LeftClick;
            vp3.Children.Add(new SunLight());

            var mv3 = new System.Windows.Media.Media3D.ModelVisual3D
            {
                Content = new _3DViewModel(meshes, cols).Model,
            };
            vp3.Children.Add(mv3);


            var host = new HUI_WpfHost(vp3) { Width = (int)width, Height = (int)height };
            DA.SetData("3DView", new UIElement_Goo(host, "3D View", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Create3dView;

        public override Guid ComponentGuid => new Guid("{5d84c99e-fe9c-4546-8cb0-9f6fe58e011d}");
    }
}

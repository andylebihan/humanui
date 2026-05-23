using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using HelixToolkit.Wpf;
using Rhino.Geometry;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Swap the meshes / colors inside an existing 3D View. Rebuilds the
    /// ModelVisual3D in-place so the camera, lights, and gestures are
    /// preserved; the parent HelixViewport3D is unwrapped from its
    /// HUI_WpfHost via the standard HUI_WpfHost.Unwrap helper.
    /// </summary>
    public class Set3DView_Component : GH_Component
    {
        public Set3DView_Component()
            : base("Set 3D View", "Set3DView",
                "Modify the contents of an existing 3D View",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("3D View", "V", "The 3D View to modify", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh to display", "M", "The new meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The new mesh colours", GH_ParamAccess.list, System.Drawing.Color.Red);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object viewObj = null;
            var meshes = new List<Mesh>();
            var cols = new List<System.Drawing.Color>();
            if (!DA.GetData("3D View", ref viewObj)) return;
            if (!DA.GetDataList("Mesh to display", meshes)) return;
            DA.GetDataList("Mesh Colors", cols);

            var vp3 = HUI_WpfHost.Unwrap<HelixViewport3D>(viewObj);
            if (vp3 == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not resolve the 3D View element.");
                return;
            }

            // Match the original WPF SetView pattern: clear everything, then
            // re-add a fresh SunLight before the new mesh. This is necessary
            // because SunLight derives from ModelVisual3D, so filtering by type
            // alone would strip the light. Without a light the mesh renders
            // unlit and effectively invisible against the default background —
            // the orientation cube still appears because it has its own
            // internal lighting.
            //
            // If no color list arrived (e.g., a wire goes nowhere or upstream
            // ValueListener returns null for an unsupported control), default
            // to red so the user gets a visible mesh instead of an empty cols
            // list that would crash _3DViewModel with DivideByZero on i % 0.
            if (cols.Count == 0) cols.Add(System.Drawing.Color.Red);
            vp3.Children.Clear();
            vp3.Children.Add(new SunLight());
            vp3.Children.Add(new System.Windows.Media.Media3D.ModelVisual3D
            {
                Content = new _3DViewModel(meshes, cols).Model,
            });
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Set3dView;

        public override Guid ComponentGuid => new Guid("{3472130d-fc0e-409d-9295-f93ecaf1afb5}");
    }
}

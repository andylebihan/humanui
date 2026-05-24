using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
#if HUI_WINDOWS
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
#endif

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Textured variant of Set 3D View. Windows uses HelixToolkit's
    /// material/texture pipeline (image-file paths get loaded as
    /// DiffuseMaterials). Mac falls back to plain colored meshes with a
    /// remark — the HUI_View3D software renderer doesn't support
    /// textures.
    /// </summary>
    public class Set3DViewTex_Component : GH_Component
    {
        public Set3DViewTex_Component()
            : base("Set 3D View Textured", "Set3DViewTex",
                "Allows you to modify the contents of an existing 3D view.",
                "Human UI", "UI Output")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("3D View", "V", "The 3D view to modify", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh to display", "M", "The mesh(es) to display in the viewport", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The color with which to display the mesh.", GH_ParamAccess.list);
            pManager.AddTextParameter("Mesh Texture", "T", "The textures to display each mesh", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object o = null;
            var meshes = new List<Mesh>();
            var textures = new List<string>();
            var cols = new List<System.Drawing.Color>();
            if (!DA.GetData("3D View", ref o)) return;
            DA.GetDataList("Mesh to display", meshes);
            bool hasTexture = DA.GetDataList("Mesh Texture", textures);
            bool hasColor = DA.GetDataList("Mesh Colors", cols);

#if HUI_WINDOWS
            var vp3 = HUI_Util.GetUIElement<HelixViewport3D>(o);
            if (vp3 == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not resolve the 3D View element.");
                return;
            }

            ModelVisual3D mv3 = GetModelVisual3D(vp3);
            var mv3s = GetModels(vp3);
            var mats = new List<Material>();

            vp3.Children.Clear();
            vp3.Children.Add(new SunLight());

            if (!hasColor && !hasTexture)
            {
                // Preserve existing materials across mesh swap.
                foreach (var mv30 in mv3s)
                {
                    if (mv30.Content is Model3DGroup model)
                    {
                        foreach (var mod in model.Children)
                        {
                            if (mod is GeometryModel3D geom) mats.Add(geom.Material);
                        }
                    }
                }
                mv3.Content = new _3DViewModel(meshes, mats).Model;
            }
            else if (!hasTexture)
            {
                mv3.Content = new _3DViewModel(meshes, cols).Model;
            }
            else
            {
                mv3.Content = new _3DViewModel(meshes, textures).Model;
            }
            vp3.Children.Add(mv3);
#else
            // Mac: HUI_View3D doesn't render textures. Fall back to plain
            // colored geometry and emit a remark if the user supplied a
            // texture path so they know it's been silently ignored.
            var view = HUI_Util.GetUIElement<HUI_View3D>(o);
            if (view == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not resolve the 3D View element.");
                return;
            }
            if (hasTexture)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Mesh textures are not supported on Mac (HUI_View3D is a software renderer). Falling back to flat colors.");
            if (cols.Count == 0) cols.Add(System.Drawing.Color.Red);
            view.SetGeometry(meshes, cols);
#endif
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Set3dView_textured;

#if HUI_WINDOWS
        private static ModelVisual3D GetModelVisual3D(HelixViewport3D vp3)
        {
            foreach (var v in vp3.Children)
                if (v is ModelVisual3D mv) return mv;
            return null;
        }

        private static List<ModelVisual3D> GetModels(HelixViewport3D vp3)
        {
            var models = new List<ModelVisual3D>();
            foreach (var v in vp3.Children)
                if (v is ModelVisual3D mv) models.Add(mv);
            return models;
        }
#endif

        public override Guid ComponentGuid => new Guid("{47D12D28-2711-435A-A445-DD4016EBB363}");
    }
}

using System;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Stub for the Eto migration. Pairs with the Create3DView stub.
    /// </summary>
    public class Set3DView_Component : GH_Component
    {
        public Set3DView_Component()
            : base("Set 3D View", "Set3DView",
                "Modify the contents of an existing 3D View (Eto preview: not yet ported).",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("3D View", "V", "The 3D View to modify", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh to display", "M", "The new meshes", GH_ParamAccess.list);
            pManager.AddColourParameter("Mesh Colors", "C", "The new mesh colours", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Set 3D View is not yet ported to Eto.");
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Set3dView;

        public override Guid ComponentGuid => new Guid("{3472130d-fc0e-409d-9295-f93ecaf1afb5}");
    }
}

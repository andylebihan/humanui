using System;
using System.Linq;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components
{
    /// <summary>
    /// Introspect an Eto.Forms control via reflection and emit its property
    /// names + types. Useful for hunting fields to feed Set Element
    /// Property. Marked experimental because the property surface differs
    /// between Eto control types (and between Windows / Mac platform
    /// handlers in some cases).
    /// </summary>
    public class GetElementProperties_Component : GH_Component
    {
        public GetElementProperties_Component()
            : base("Get Element Properties", "GetProps",
                "Tries to get all properties of any element. This is experimental!",
                "Human UI", "UI Main")
        { }

        public override Guid ComponentGuid => new Guid("{097D774D-A647-4DEA-8333-63E8F662A7EA}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Element", "E", "The element to get properties for", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Property Names", "N", "The names of the properties", GH_ParamAccess.list);
            pManager.AddTextParameter("Property Types", "T", "The types of the properties", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object elem = null;
            if (!DA.GetData("UI Element", ref elem)) return;

            var control = HUI_Util.GetUIElement<Control>(elem);
            if (control == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element could not be unwrapped to a control.");
                return;
            }

            var props = control.GetType().GetProperties();
            DA.SetDataList("Property Names", props.Select(p => p.Name));
            DA.SetDataList("Property Types", props.Select(p => p.PropertyType.ToString()));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.GetProps;
    }
}

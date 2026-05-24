using System;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace HumanUI.Components
{
    /// <summary>
    /// Set an arbitrary Eto control property by name, via reflection. Used
    /// in tandem with Get Element Properties to enable ad-hoc property
    /// changes without a custom Set-* component. Experimental — there's
    /// no validation that the supplied value type matches the property.
    /// </summary>
    public class SetElementProperty_Component : GH_Component
    {
        public SetElementProperty_Component()
            : base("Set Element Property", "SetProp",
                "Tries to set any property of an element. This is experimental!",
                "Human UI", "UI Main")
        { }

        public override Guid ComponentGuid => new Guid("{CF439FD5-6CC5-4DB6-A265-9B0E5AED2989}");
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("UI Element", "E", "The element to set", GH_ParamAccess.item);
            pManager.AddTextParameter("Property Name", "P", "The name of the property to set", GH_ParamAccess.item);
            pManager.AddGenericParameter("Value to set", "V", "The value or object to set the property to", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object elem = null;
            string propName = string.Empty;
            object val = null;

            if (!DA.GetData("UI Element", ref elem)) return;
            if (!DA.GetData("Property Name", ref propName)) return;
            if (!DA.GetData("Value to set", ref val)) return;

            var control = HUI_Util.GetUIElement<Control>(elem);
            if (control == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Element could not be unwrapped to a control.");
                return;
            }

            // Peel the GH wrapper layers down to a raw .NET value so
            // reflection-SetValue gets something the target property
            // actually wants.
            if (val is GH_ObjectWrapper wrapper)
            {
                val = wrapper.Value;
            }
            else if (val is IGH_Goo)
            {
                try { val = val.GetType().GetProperty("Value")?.GetValue(val); }
                catch { /* leave val as-is */ }
            }

            var type = control.GetType();
            var prop = type.GetProperty(propName);
            if (prop == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Object type {type} does not contain a property called {propName}");
                return;
            }
            try
            {
                prop.SetValue(control, val);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Something went wrong setting the property:\n{e.Message}");
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetProp;
    }
}

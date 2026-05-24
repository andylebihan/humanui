using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Mutates a CreateExpander result — change the header text and/or
    /// open/close it. The Eto Expander.Header is a Control (typically a
    /// Label that CreateExpander wraps the user's header text in); we
    /// drill through that to update the text without rebuilding the
    /// header control identity.
    /// </summary>
    public class SetExpander_Component : GH_Component
    {
        public SetExpander_Component()
            : base("Set Expander", "SetExp",
                "Sets the properties of an expander container",
                "Human UI", "UI Output")
        { }

        public override Guid ComponentGuid => new Guid("{8A270D94-FBD4-49F0-8CFE-5FB43CE85BEE}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Expander", "E", "The expander to modify", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("Name", "N", "The new name of the expander", GH_ParamAccess.item)].Optional = true;
            pManager[pManager.AddBooleanParameter("Expanded Open", "O", "Set to true to expand or false to collapse", GH_ParamAccess.item)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object expanderElement = null;
            string newName = string.Empty;
            bool expanded = false;

            if (!DA.GetData("Expander", ref expanderElement)) return;
            bool hasName = DA.GetData("Name", ref newName);
            bool hasExpanded = DA.GetData("Expanded Open", ref expanded);

            var e = HUI_Util.GetUIElement<Expander>(expanderElement);
            if (e == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input is not an Expander.");
                return;
            }

            if (hasName)
            {
                // CreateExpander wraps the user's header text in a Label.
                // Update the existing Label in place so the layout doesn't
                // get reshuffled; fall back to replacing the whole header
                // if it isn't a Label.
                if (e.Header is Label label) label.Text = newName;
                else e.Header = new Label { Text = newName };
            }
            if (hasExpanded) e.Expanded = expanded;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.setExpander;
    }
}

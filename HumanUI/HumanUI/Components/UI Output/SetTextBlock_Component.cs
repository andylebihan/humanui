using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Modify the text content of a TextBlock. Both CreateLabel and CreateTextBlock map
    /// to the same Eto.Forms.Label internally, so this and SetLabel are functionally
    /// equivalent now; the separate component preserves user-visible naming and the
    /// historical GH GUID.
    /// </summary>
    public class SetTextBlock_Component : GH_Component
    {
        public SetTextBlock_Component()
            : base("Set TextBlock Contents", "SetTextBlock",
                "Modify the contents of an existing Text Block object.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Text Block to modify", "TB", "The text block object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New Text Block contents", "C", "The new text to display in the text block", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object textBlockObject = null;
            string newContents = "";
            if (!DA.GetData("New Text Block contents", ref newContents)) return;
            if (!DA.GetData("Text Block to modify", ref textBlockObject)) return;
            var label = HUI_Util.GetUIElement<Label>(textBlockObject);
            if (label != null)
            {
                label.Text = newContents;
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetTextBlock;

        public override Guid ComponentGuid => new Guid("{A1E21BA0-2F60-41DF-BB5A-619802C5AF9A}");
    }
}

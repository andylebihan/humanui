using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;
using HumanUI.Components.UI_Elements;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Replace the items and (optionally) the checked state of an existing
    /// Checklist scrollable. Mirrors CreateCheckList_Component.BuildChecklistContent
    /// so a Set call leaves the same composite shape.
    /// </summary>
    public class SetChecklist_Component : GH_Component
    {
        public SetChecklist_Component()
            : base("Set Checklist Contents", "SetChecklist",
                "Use this to set the contents of a checklist",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Checklist to modify", "L", "The list object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New checklist contents", "C", "The new items to display in the checklist", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Selected", "S", "The optional boolean values to control if items are selected", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object listObject = null;
            var listContents = new List<string>();
            var isSelected = new List<bool>();
            if (!DA.GetData("Checklist to modify", ref listObject)) return;
            if (!DA.GetDataList("New checklist contents", listContents)) return;
            DA.GetDataList("Selected", isSelected);

            var scroll = HUI_Util.GetUIElement<Scrollable>(listObject);
            if (scroll == null) return;
            scroll.Content = CreateCheckList_Component.BuildChecklistContent(listContents, isSelected);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetCheckList;

        public override Guid ComponentGuid => new Guid("{11A5A354-FC1B-4CEA-894A-BBEB71A23DB5}");
    }
}

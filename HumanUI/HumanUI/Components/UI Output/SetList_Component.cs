using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Set the items of a ListBox or DropDown. CreateListBox produces a bare ListBox;
    /// CreatePulldown produces a StackLayout wrapping a DropDown, so we walk into the
    /// container to find whichever selector control is in there.
    /// </summary>
    public class SetList_Component : GH_Component
    {
        public SetList_Component()
            : base("Set List Contents", "SetList",
                "Use this to set the contents of either a List Box or a Pulldown Menu",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("List to modify", "L", "The list object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New list contents", "C", "The new items to display in the label", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Selected Index", "I", "The optional index to select in the updated list", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object listObject = null;
            var listContents = new List<string>();
            int selectedIndex = -1;
            if (!DA.GetData("List to modify", ref listObject)) return;
            if (!DA.GetDataList("New list contents", listContents)) return;

            var control = HUI_Util.GetUIElement<Control>(listObject);
            var listBox = FindControl<ListBox>(control);
            var dropDown = listBox == null ? FindControl<DropDown>(control) : null;
            if (listBox == null && dropDown == null) return;

            bool indexSupplied = DA.GetData("Selected Index", ref selectedIndex);

            if (listBox != null)
            {
                int previous = listBox.SelectedIndex;
                listBox.Items.Clear();
                foreach (var item in listContents)
                    listBox.Items.Add(new ListItem { Text = item });
                int target = indexSupplied ? selectedIndex : previous;
                if (target >= 0 && target < listBox.Items.Count) listBox.SelectedIndex = target;
            }
            else
            {
                int previous = dropDown.SelectedIndex;
                dropDown.Items.Clear();
                foreach (var item in listContents)
                    dropDown.Items.Add(new ListItem { Text = item });
                int target = indexSupplied ? selectedIndex : previous;
                if (target >= 0 && target < dropDown.Items.Count) dropDown.SelectedIndex = target;
            }
        }

        private static T FindControl<T>(Control c) where T : Control
        {
            if (c is T match) return match;
            if (c is Container container)
            {
                foreach (var child in container.Controls)
                {
                    var found = FindControl<T>(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetList;

        public override Guid ComponentGuid => new Guid("{d98497d6-c164-4499-aedb-78d04c09eba4}");
    }
}

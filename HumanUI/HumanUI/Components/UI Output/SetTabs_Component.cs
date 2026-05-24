using System;
using System.Collections.Generic;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Mutates a Tabbed View (Eto.Forms.TabControl): rename tabs, show/hide
    /// individual tabs, and/or select the active tab index. The WPF version
    /// used Visibility.Collapsed to hide a tab; Eto has no equivalent on
    /// TabPage so a "Show Tabs = false" entry maps to TabPage.Visible.
    /// </summary>
    public class SetTabs_Component : GH_Component
    {
        public SetTabs_Component()
            : base("Set Tabbed View", "SetTab",
                "Sets the properties of a tabbed view",
                "Human UI", "UI Output")
        { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Tabbed View", "T", "The tabbed view to modify", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("Tab Names", "N", "The list of names corresponding to each tab (optional)", GH_ParamAccess.list)].Optional = true;
            pManager[pManager.AddBooleanParameter("Show Tabs", "S", "Provide a list of boolean values to selectively hide/show tabs (optional)", GH_ParamAccess.list)].Optional = true;
            pManager[pManager.AddIntegerParameter("Set Tab Index", "I", "The index value of the tab to select (optional)", GH_ParamAccess.item)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object tabContainer = null;
            var tabNames = new List<string>();
            var showTabs = new List<bool>();
            int selectIndex = -1;
            if (!DA.GetData("Tabbed View", ref tabContainer)) return;
            bool hasNames = DA.GetDataList("Tab Names", tabNames);
            bool hasShowTabs = DA.GetDataList("Show Tabs", showTabs);
            bool hasIndex = DA.GetData(3, ref selectIndex);

            var tabControl = HUI_Util.GetUIElement<TabControl>(tabContainer);
            if (tabControl == null) return;

            for (int i = 0; i < tabControl.Pages.Count; i++)
            {
                var page = tabControl.Pages[i];
                if (hasNames && i < tabNames.Count) page.Text = tabNames[i];
                if (hasShowTabs && i < showTabs.Count)
                {
                    page.Visible = showTabs[i];
                    if (!showTabs[i] && tabControl.SelectedIndex == i)
                    {
                        tabControl.SelectedIndex = (i + 1) % tabControl.Pages.Count;
                    }
                }
            }
            if (hasIndex)
            {
                tabControl.SelectedIndex = selectIndex;
                tabControl.Focus();
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetTab;
        public override Guid ComponentGuid => new Guid("1e63a1ca-e3e8-44ad-8ee6-0a660e01c84b");
    }
}

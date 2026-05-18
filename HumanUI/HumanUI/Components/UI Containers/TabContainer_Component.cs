using System;
using System.Collections.Generic;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components.UI_Containers
{
    /// <summary>
    /// Eto TabControl wrapped as a variable-parameter Grasshopper component. Tab N
    /// inputs grow dynamically as the user wires additional tabs — the canonical
    /// HumanUI Tabbed View pattern. Icons are loaded from disk paths and shown
    /// alongside the tab text via an Eto Image; if Eto's TabPage doesn't accept
    /// an image header on the current platform we drop back to text only.
    /// </summary>
    public class TabContainer_Component : GH_Component, IGH_VariableParameterComponent
    {
        public TabContainer_Component()
            : base("Tabbed View", "Tabs",
                "Creates a series of tabbed views that can contain UI element layouts",
                "Human UI", "UI Containers")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Tab Names", "N", "The labels for the tabs you're creating.", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager.AddNumberParameter("Tab Text Size", "S", "The font size for tab elements", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("Tab Icon Source", "I", "The path to an icon image - one per tab", GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager.AddGenericParameter("Tab 0", "T0", "The contents of the first tab", GH_ParamAccess.list);
            VariableParameterMaintenance();
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tabs", "T", "The Tab control", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "It looks like you're trying to do something with data trees here that doesn't make sense.");
                return;
            }

            var tabNames = new List<string>();
            var iconPaths = new List<string>();
            double fontSize = 14;
            DA.GetDataList("Tab Icon Source", iconPaths);
            bool hasNames = DA.GetDataList("Tab Names", tabNames);
            bool setSize = DA.GetData("Tab Text Size", ref fontSize);

            int tabCount = hasNames ? tabNames.Count : iconPaths.Count;
            var tabList = new List<List<UIElement_Goo>>();
            for (int i = 3; i < Params.Input.Count; i++)
            {
                var currentTab = new List<UIElement_Goo>();
                DA.GetDataList(i, currentTab);
                tabList.Add(currentTab);
            }
            if (tabList.Count != tabCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You don't have the same number of specified tab names (or icons) and tab inputs.");
            }

            var tabs = new TabControl();
            tabs.ID = "GH_Tabs";

            for (int tabIndex = 0; tabIndex < tabList.Count; tabIndex++)
            {
                string tabName = (tabIndex < tabNames.Count) ? tabNames[tabIndex] : $"Tab {tabIndex}";
                var page = new TabPage { Text = tabName };
                // Eto's TabPage doesn't expose a per-tab font; Tab Text Size is accepted
                // for backward compatibility but has no visible effect in the Eto port.
                if (tabIndex < iconPaths.Count && File.Exists(iconPaths[tabIndex]))
                {
                    try { page.Image = new Bitmap(iconPaths[tabIndex]); }
                    catch { /* image load failures are non-fatal */ }
                }

                var stack = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    ID = "GH_TabItem",
                };
                foreach (var u in tabList[tabIndex])
                {
                    if (u?.element == null) continue;
                    HUI_Util.removeParent(u.element);
                    stack.Items.Add(new StackLayoutItem(u.element, HorizontalAlignment.Stretch));
                }
                page.Content = stack;
                tabs.Pages.Add(page);
            }

            DA.SetData("Tabs", new UIElement_Goo(tabs, $"Tab Control with {tabList.Count} tabs", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.TabControl;

        public override Guid ComponentGuid => new Guid("{EAF93260-86B3-4AE7-82D2-58E683DEAE7B}");

        #region VariableParameterImplementation

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Output) return false;
            if (index < 3) return false;
            return true;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            if (side == GH_ParameterSide.Output) return false;
            if (Params.Input.Count <= 4) return false;
            if (index < 3) return false;
            return true;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var input = new Param_GenericObject { Optional = true };
            Params.RegisterInputParam(input, index);
            return input;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public void VariableParameterMaintenance()
        {
            for (int i = 3; i < Params.Input.Count; i++)
            {
                var p = Params.Input[i];
                p.NickName = $"T{i - 3}";
                p.Name = $"Tab {i - 3}";
                p.Description = $"The ui elements to include in tab {i - 3}";
                p.Access = GH_ParamAccess.list;
                p.Optional = true;
                p.DataMapping = GH_DataMapping.Flatten;
            }
        }

        #endregion
    }
}

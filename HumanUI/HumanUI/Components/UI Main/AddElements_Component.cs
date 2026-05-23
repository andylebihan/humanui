using System;
using System.Collections.Generic;
#if HUI_WINDOWS
using System.Windows.Forms;
#endif
using Grasshopper.Kernel;
using GH_IO.Serialization;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Adds Eto controls produced by Create-* components to a Human UI window. Preserves
    /// the original component GUID and parameter shape so existing .gh files load cleanly.
    /// </summary>
    public class AddElements_Component : GH_Component
    {
        public AddElements_Component()
            : base("Add Elements", "AddElems",
                "Add UI Controls to a window",
                "Human UI", "UI Main")
        {
            DoVLChecking = true;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Window", "W", "The window to which to add the elements", GH_ParamAccess.item);
            pManager.AddGenericParameter("Elements", "E", "The Controls and other elements you want to add to the window", GH_ParamAccess.list);
            pManager[1].DataMapping = GH_DataMapping.Flatten;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Added Elements", "E", "The elements added.", GH_ParamAccess.list);
            pManager.AddTextParameter("Element Names", "N", "The names of the added elements.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            MainWindow mw = null;
            List<UIElement_Goo> elementsToAdd = new List<UIElement_Goo>();
            Dictionary<string, UIElement_Goo> resultDict = new Dictionary<string, UIElement_Goo>();

            if (!DA.GetData("Window", ref mw)) return;
            if (!DA.GetDataList("Elements", elementsToAdd)) return;

            mw.clearElements();

            // ValueListener flow-loop checking is reintroduced in phase 3 when
            // ValueListener_Component is ported back; until then DoVLChecking is a no-op.

            foreach (UIElement_Goo u in elementsToAdd)
            {
                if (u == null) continue;
                HUI_Util.removeParent(u.element);
                mw.AddElement(u.element);
                HUI_Util.AddToDict(u, resultDict);
            }

            DA.SetDataList("Added Elements", resultDict);
            DA.SetDataList("Element Names", resultDict.Keys);
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.AddElements;

        bool DoVLChecking = true;

        void updateMessage()
        {
            Message = DoVLChecking ? "" : "Fast Mode";
        }

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Disable Flow Loop Checking (Fast Mode)", FastModeClicked, true, !DoVLChecking);
        }
#endif

        private void FastModeClicked(object sender, EventArgs e)
        {
            DoVLChecking = !DoVLChecking;
            updateMessage();
            ExpireSolution(true);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("DoVLChecking", DoVLChecking);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            bool check = true;
            reader.TryGetBoolean("DoVLChecking", ref check);
            DoVLChecking = check;
            updateMessage();
            ExpireSolution(true);
            return base.Read(reader);
        }

        public override Guid ComponentGuid => new Guid("{73b5e187-b35d-45bd-8495-9e06b429bc07}");
    }
}

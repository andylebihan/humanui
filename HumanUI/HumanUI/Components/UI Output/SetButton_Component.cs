using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    public class SetButton_Component : GH_Component
    {
        public SetButton_Component()
          : base("Set Button", "SetBtn",
                "Change the content of an existing Button element.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Button to modify", "B", "The button object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("Button Name", "N", "The new text to display on the button", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("Image Path", "I", "The new image to display on the button.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object buttonObject = null;
            string newName = "";
            string newImage = "";
            if (!DA.GetData("Button to modify", ref buttonObject)) return;
            bool hasText = DA.GetData("Button Name", ref newName);
            bool hasIcon = DA.GetData("Image Path", ref newImage);

            var btn = HUI_Util.GetUIElement<Button>(buttonObject);
            if (btn == null) return;
            if (!hasText && !hasIcon) return;

            if (hasText) btn.Text = newName;
            if (hasIcon && File.Exists(newImage))
            {
                try { btn.Image = new Bitmap(newImage); }
                catch { /* ignore */ }
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetButton;

        public override Guid ComponentGuid => new Guid("{f7b661aa-71b0-483e-acd6-95663c78d7df}");
    }
}

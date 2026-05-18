using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    public class SetImage_Component : GH_Component
    {
        public SetImage_Component()
            : base("Set Image", "SetImg",
                "Change the content of an existing Image control.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Image to modify", "I", "The image object to modify", GH_ParamAccess.item);
            pManager.AddTextParameter("New Image Path", "I2", "The path to a new image to replace in the window", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object imageObject = null;
            string newPath = "";
            if (!DA.GetData("New Image Path", ref newPath)) return;
            if (!DA.GetData("Image to modify", ref imageObject)) return;

            var view = HUI_Util.GetUIElement<ImageView>(imageObject);
            if (view == null) return;
            try
            {
                if (File.Exists(newPath))
                    view.Image = new Bitmap(newPath);
                else
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Image file not found: {newPath}");
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SetImage;

        public override Guid ComponentGuid => new Guid("{bc15817d-291f-461b-a1a8-f3c66fd053be}");
    }
}

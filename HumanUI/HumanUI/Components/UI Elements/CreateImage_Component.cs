using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Image element backed by Eto.Forms.ImageView. The Goo wraps the ImageView directly
    /// so SetImage can find it.
    /// </summary>
    public class CreateImage_Component : GH_Component
    {
        public CreateImage_Component()
            : base("Create Image", "Image",
                "Creates an image object to be added to the window",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Image Source", "src", "The file location of the image to add.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Image Width", "W", "The width of the image", GH_ParamAccess.item, 300);
            pManager.AddNumberParameter("Image Height", "H", "The height of the image. Set to 0 or leave blank to scale image proportionally to its width.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Image", "I", "The image object. Use in conjunction with an \"Add Elements\" component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string imgSrc = "";
            double width = 0;
            double height = 0;
            if (!DA.GetData("Image Source", ref imgSrc)) return;
            DA.GetData("Image Width", ref width);
            DA.GetData("Image Height", ref height);

            var view = new ImageView();
            string name = "Image";
            try
            {
                if (File.Exists(imgSrc))
                {
                    var bmp = new Bitmap(imgSrc);
                    if (height == 0)
                        height = bmp.Height / (double)bmp.Width * width;
                    view.Image = bmp;
                    name = Path.GetFileNameWithoutExtension(imgSrc);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Image file not found: {imgSrc}");
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
            }

            view.Width = (int)width;
            view.Height = (int)height;

            DA.SetData("Image", new UIElement_Goo(view, name, InstanceGuid, DA.Iteration));
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateImage;

        public override Guid ComponentGuid => new Guid("{3c76e033-c9a8-4b6d-94c4-8ccdfac09834}");
    }
}

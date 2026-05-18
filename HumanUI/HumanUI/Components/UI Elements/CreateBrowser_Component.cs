using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Wraps an Eto WebView so a HumanUI window can host a web page. On Windows
    /// Eto's WebView delegates to the system WebView2/Edge component (or IE if
    /// not present); on macOS it uses WebKit. Both paths require the platform
    /// runtime to be installed — if WebView creation throws we fall back to a
    /// Label so the rest of the window still renders.
    /// </summary>
    public class CreateBrowser_Component : GH_Component
    {
        public CreateBrowser_Component()
            : base("Create Browser", "Browser",
                "Creates a web browser window.",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL", "U", "The URI/URL of the page to display", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Width", "W", "Width of the window", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddIntegerParameter("Height", "H", "Height of the window", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Browser", "WB", "The created Web Browser", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string url = "";
            int width = -1;
            int height = -1;
            if (!DA.GetData("URL", ref url)) return;

            Control control;
            try
            {
                var wb = new WebView();
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    wb.Url = uri;
                }
                if (DA.GetData("Width", ref width)) wb.Width = width;
                if (DA.GetData("Height", ref height)) wb.Height = height;
                control = wb;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"WebView not available on this platform: {ex.Message}");
                control = new Label { Text = $"[Browser unavailable] {url}" };
            }

            DA.SetData("Browser", new UIElement_Goo(control, $"Browser: {url}", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.createBrowser;

        public override Guid ComponentGuid => new Guid("{cf49dbbd-93b5-4f9f-81ea-f585f20a5843}");

        public override GH_Exposure Exposure => GH_Exposure.secondary;
    }
}

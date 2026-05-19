using System;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Eto port of the Markdown Viewer. The WPF original used Markdown.Xaml and
    /// FlowDocumentScrollViewer, which have no cross-platform equivalent. The
    /// Eto port renders to HTML in a WebView; the Styles File input is accepted
    /// for compatibility but no-ops.
    /// </summary>
    public class CreateMarkdownViewer_Component : GH_Component
    {
        public CreateMarkdownViewer_Component()
            : base("Create Markdown Viewer", "MDV",
                "Creates a block of formatted text based on Markdown-formatted input",
                "Human UI", "UI Elements")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Text", "T", "The markdown-syntax-formatted text you'd like to render", GH_ParamAccess.item);
            pManager.AddTextParameter("Styles File", "SF", "The optional path to a styles.xaml file. Defaults will be used if not supplied", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddTextParameter("Asset Directory", "AD", "The optional root directory for any assets like in-line images included in your markdown", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Markdown Viewer", "MDV", "The Markdown Viewer Element.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string text = "";
            string stylePath = "";
            string assetPath = "";
            if (!DA.GetData("Text", ref text)) return;
            DA.GetData("Styles File", ref stylePath);
            DA.GetData("Asset Directory", ref assetPath);
            if (!string.IsNullOrEmpty(stylePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Styles File is ignored in the Eto port — markdown is styled with a default inline CSS.");
            }

            try
            {
                var mdv = new MarkdownViewer(text, stylePath, assetPath);
                DA.SetData("Markdown Viewer", new UIElement_Goo(mdv, "Markdown Viewer", InstanceGuid, DA.Iteration));
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"WebView not available on this platform: {ex.Message}");
                var fallback = new Eto.Forms.Label { Text = text };
                DA.SetData("Markdown Viewer", new UIElement_Goo(fallback, "Markdown Viewer", InstanceGuid, DA.Iteration));
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.MarkdownViewer;

        public override Guid ComponentGuid => new Guid("{127E3F49-5F69-46A1-96FE-2E531EEAD975}");
    }
}

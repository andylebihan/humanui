using System;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Output
{
    /// <summary>
    /// Navigate / refresh an existing Browser element. Eto's WebView exposes
    /// GoBack / GoForward / Reload directly; CanGoBack/Forward are honored to
    /// avoid no-op clicks.
    /// </summary>
    public class SetBrowser_Component : GH_Component
    {
        public SetBrowser_Component()
            : base("Set Browser", "SetBrowser",
                "Control the Browser element - with back/forward buttons, and control over the displayed site etc.",
                "Human UI", "UI Output")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Browser", "B", "The Browser UI Element to modify", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Back", "Bk", "Set to true in order to send the browser back a page", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Forward", "Fwd", "Set to true to send the browser forward a page", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Refresh", "Rf", "Set to true to refresh the current window", GH_ParamAccess.item);
            pManager.AddTextParameter("URL", "U", "The URL to set the current browser to access.", GH_ParamAccess.item);
            for (int i = 1; i < 5; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj = null;
            bool forward = false;
            bool back = false;
            bool refresh = false;
            string URL = "";
            if (!DA.GetData("Browser", ref obj)) return;

            var wb = HUI_Util.GetUIElement<WebView>(obj);
            if (wb == null) return;

            if (DA.GetData("Back", ref back) && back && wb.CanGoBack) wb.GoBack();
            if (DA.GetData("Forward", ref forward) && forward && wb.CanGoForward) wb.GoForward();
            if (DA.GetData("Refresh", ref refresh) && refresh) wb.Reload();
            if (DA.GetData("URL", ref URL) && Uri.TryCreate(URL, UriKind.Absolute, out var uri)) wb.Url = uri;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.setBrowser;

        public override Guid ComponentGuid => new Guid("{a880cc82-b5df-45bc-a730-afa8352c4679}");
    }
}

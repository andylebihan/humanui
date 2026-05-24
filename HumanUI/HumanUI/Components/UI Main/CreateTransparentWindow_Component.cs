using System;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Variant of LaunchWindow_Component that opens with a semi-transparent
    /// background. Eto.Forms.Window doesn't expose a true cross-platform
    /// AllowsTransparency flag, so we approximate the WPF behavior by
    /// dropping window chrome (WindowStyle.None) and tinting the background
    /// with an alpha-blended color. On Windows this matches the old MahApps
    /// MetroWindow with AllowsTransparency = true; on Mac the background
    /// shows through the chromeless NSWindow.
    /// </summary>
    public class CreateTransparentWindow_Component : LaunchWindow_Component
    {
        public CreateTransparentWindow_Component()
            : base("Launch Transparent Window", "LaunchXPWin",
                "This component launches a new blank, transparent control window.",
                "Human UI", "UI Main")
        { }

        public override Guid ComponentGuid => new Guid("{106D7436-7223-454A-A2DA-57EE118E6815}");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.LaunchWindow_Transparent;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (mw != null)
            {
                mw.WindowStyle = WindowStyle.None;
                mw.BackgroundColor = new Color(1f, 1f, 1f, 0.4f);
            }
            base.SolveInstance(DA);
        }
    }
}

using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
#if HUI_WINDOWS
// Bring in WinForms via type aliases so they don't shadow Eto.Forms types (Button etc).
// Windows-only: Grasshopper's AppendAdditionalComponentMenuItems hook takes a
// System.Windows.Forms.ToolStripDropDown, which doesn't exist at the net7.0 TFM.
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;
#endif

namespace HumanUI.Components.UI_Elements
{
    /// <summary>
    /// Selectable button visual style. Eto.Forms doesn't have direct equivalents for the
    /// MahApps Square / Circle / Borderless styles; in the Eto port the menu is preserved
    /// so component state and serialization don't drift, but only Default and Borderless
    /// have visible effect (Borderless drops the button border via Eto's flat hint).
    /// </summary>
    public enum buttonStyle { Default, Square, Circle, Borderless };

    public class CreateButton_Component : GH_Component
    {
        protected buttonStyle bs = buttonStyle.Default;

        public CreateButton_Component()
            : base("Create Button", "Button",
                "Create a Button object.",
                "Human UI", "UI Elements")
        {
            UpdateMenu();
        }

        public CreateButton_Component(string name, string nickname, string description)
            : base(name, nickname, description, "Human UI", "UI Elements")
        {
            UpdateMenu();
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        private void UpdateMenu()
        {
            Message = bs switch
            {
                buttonStyle.Square => "Square Style",
                buttonStyle.Circle => "Circle Style",
                buttonStyle.Borderless => "Borderless",
                _ => "",
            };
        }

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Default Style", menu_makeDefaultStyle, true, bs == buttonStyle.Default)
                .ToolTipText = "Use the default button style.";
            GH_DocumentObject.Menu_AppendItem(menu, "Square Style", menu_makeSquareStyle, true, bs == buttonStyle.Square)
                .ToolTipText = "Use a flat, square button style.";
            GH_DocumentObject.Menu_AppendItem(menu, "Circle Style", menu_makeCircleStyle, true, bs == buttonStyle.Circle)
                .ToolTipText = "Use a circle (or ellipse) button style.";
            GH_DocumentObject.Menu_AppendItem(menu, "Borderless Style", menu_makeBorderless, true, bs == buttonStyle.Borderless)
                .ToolTipText = "Use a borderless button style.";
        }
#endif

        private void menu_makeDefaultStyle(object sender, EventArgs e) { RecordUndoEvent("Button Style Change"); bs = buttonStyle.Default; UpdateMenu(); ExpireSolution(true); }
        private void menu_makeSquareStyle(object sender, EventArgs e) { RecordUndoEvent("Button Style Change"); bs = buttonStyle.Square; UpdateMenu(); ExpireSolution(true); }
        private void menu_makeCircleStyle(object sender, EventArgs e) { RecordUndoEvent("Button Style Change"); bs = buttonStyle.Circle; UpdateMenu(); ExpireSolution(true); }
        private void menu_makeBorderless(object sender, EventArgs e) { RecordUndoEvent("Button Style Change"); bs = buttonStyle.Borderless; UpdateMenu(); ExpireSolution(true); }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("ButtonStyle", (int)bs);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            int readVal = -1;
            reader.TryGetInt32("ButtonStyle", ref readVal);
            bs = (buttonStyle)readVal;
            UpdateMenu();
            return base.Read(reader);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Button Name", "N", "The text to display on the button", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager.AddTextParameter("Image Path", "I", "The image to display on the button.", GH_ParamAccess.item);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Button", "B", "The created Button", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "Button";
            string imagePath = "";
            bool hasText = DA.GetData("Button Name", ref name);
            bool hasIcon = DA.GetData("Image Path", ref imagePath);
            if (!hasText && !hasIcon) return;

            var btn = new Button();
            SetupButton(name, imagePath, hasText, hasIcon, btn, bs);

            DA.SetData("Button", new UIElement_Goo(btn, name, InstanceGuid, DA.Iteration));
        }

        /// <summary>
        /// Configure an Eto.Forms.Button with optional text and image. Shared with
        /// CreateRhinoPickButton_Component (Phase 3) and SetButton_Component.
        /// </summary>
        protected static void SetupButton(string name, string imagePath, bool hasText, bool hasIcon, Button btn, buttonStyle bs)
        {
            if (hasIcon && File.Exists(imagePath))
            {
                try { btn.Image = new Bitmap(imagePath); }
                catch { /* silently ignore unreadable images */ }
            }
            btn.Text = hasText ? name : string.Empty;

            // Eto has no direct equivalent of MahApps Square / Circle / Borderless styles.
            // Borderless approximates with Style="borderless" hint where the platform
            // supports it; Square and Circle currently no-op and the message tag tells
            // the user the menu state was recorded.
            if (bs == buttonStyle.Borderless)
            {
                btn.Style = "borderless";
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.CreateButton;

        public override Guid ComponentGuid => new Guid("{9A5B87D6-046E-4C33-9ACA-5AF2F7503047}");
    }
}

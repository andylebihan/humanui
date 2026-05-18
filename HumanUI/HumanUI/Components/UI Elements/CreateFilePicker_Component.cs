using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace HumanUI.Components
{
    public class CreateFilePicker_Component : GH_Component
    {
        public CreateFilePicker_Component()
            : base("Create File Picker", "FilePicker",
                "Create a dialog box that lets you choose a path for a file, folder, or save path.",
                "Human UI", "UI Elements")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Button Label Text", "BT", "The text to display on the button to the right of the text box", GH_ParamAccess.item, "Browse...");
            pManager.AddIntegerParameter("Dialog Type", "DT", "The type of dialog to display - Open, Save, or Folder", GH_ParamAccess.item, 0);
            var typeParam = pManager[1] as Param_Integer;
            typeParam.AddNamedValue("Open File", 0);
            typeParam.AddNamedValue("Save File", 1);
            typeParam.AddNamedValue("Browse Folder", 2);
            pManager.AddBooleanParameter("Must Exist", "ME", "Set to true if the file (in an open file dialog) must exist", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Filter", "F", "The file filter(s) to use. Specify type names, separated from path filters with a | character, like \"Grasshopper Files|*.gh\" and join multiples with | characters as well.", GH_ParamAccess.item, "All Files|*.*");
            pManager.AddTextParameter("Starting Path", "SP", "The optional starting directory the dialog should open with", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("File Picker", "P", "The File Picker element", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string buttonLabelText = "";
            int dialogType = -1;
            bool mustExist = true;
            string filter = "";
            string startingDir = "";
            DA.GetData("Button Label Text", ref buttonLabelText);
            DA.GetData("Dialog Type", ref dialogType);
            DA.GetData("Must Exist", ref mustExist);
            DA.GetData("Filter", ref filter);
            DA.GetData("Starting Path", ref startingDir);

            var picker = new FilePicker(buttonLabelText, (fileDialogType)dialogType, mustExist, filter, startingDir);
            DA.SetData("File Picker", new UIElement_Goo(picker, "File Picker", InstanceGuid, DA.Iteration));
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.FilePicker;

        public override Guid ComponentGuid => new Guid("{76332FC9-F97C-4428-BEFE-723FA7C0AD37}");
    }
}

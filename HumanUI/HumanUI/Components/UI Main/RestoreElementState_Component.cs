using System;
using Eto.Forms;
using Grasshopper.Kernel;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Push a saved State back into its source elements. Variable-output
    /// pattern lets the user optionally pipe the Restore boolean back out
    /// so downstream components can sequence after the restore.
    /// </summary>
    public class RestoreElementState_Component : GH_Component, IGH_VariableParameterComponent
    {
        public RestoreElementState_Component()
            : base("Restore Element States", "Restore",
                "Restore the saved states of UI elements",
                "Human UI", "UI Main")
        {
            Params.ParameterSourcesChanged += ParamSourcesChanged;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Restore", "R", "Set to true to restore the state. \nProbably not a good idea to leave this set to true while you're adding new states.", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Saved States", "SS", "The collection of saved states", GH_ParamAccess.item);
            pManager.AddTextParameter("State Name to restore", "N", "The name of the state to restore", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool restore = false;
            string setName = string.Empty;
            StateSet_Goo states = null;
            if (!DA.GetData("Saved States", ref states)) return;
            if (!DA.GetData("State Name to restore", ref setName)) return;
            DA.GetData("Restore", ref restore);

            if (Params.Output.Count > 0) DA.SetData(0, restore);

            if (!restore) return;
            if (!states.states.TryGetValue(setName, out var stateToRestore)) return;

            foreach (var elementState in stateToRestore.stateDict)
            {
                var goo = elementState.Key;
                var element = goo.element;
                // If the live Control has no parent in the window tree,
                // chase the (componentGuid, outputIndex) lookup back into
                // the GH document to rediscover the realized Control. This
                // covers fresh-document-load — Save's Read() already
                // populated the shadow state but the live Control didn't
                // exist yet then.
                if (element == null || HUI_Util.FindTopmostParent<MainWindow>(element) == null)
                {
                    var rediscovered = SaveElementState_Component.GetElementGoo(OnPingDocument(), goo.instanceGuid, goo.index);
                    if (rediscovered == null) continue;
                    element = rediscovered.element;
                }
                HUI_Util.TrySetElementValue(HUI_Util.extractBaseElement(element), elementState.Value);
            }
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.RestoreState;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public override Guid ComponentGuid => new Guid("{A6567BB1-37D1-46CB-AD10-594FF726299B}");

        private void ParamSourcesChanged(object sender, GH_ParamServerEventArgs e)
        {
            if (e.ParameterSide == GH_ParameterSide.Output && e.ParameterIndex == Params.Output.Count - 1)
            {
                IGH_Param newParam = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
                Params.RegisterOutputParam(newParam);
                VariableParameterMaintenance();
                Params.OnParametersChanged();
            }
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Output && Params.Output.Count < 1;

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Output;

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
            => new Grasshopper.Kernel.Parameters.Param_Boolean();

        public bool DestroyParameter(GH_ParameterSide side, int index) => true;

        public void VariableParameterMaintenance()
        {
            for (int i = 0; i < Params.Output.Count; i++)
            {
                Params.Output[i].Name = "Complete";
                Params.Output[i].NickName = "C";
                Params.Output[i].Description = "Optional output parameter to indicate restore execution";
                Params.Output[i].Access = GH_ParamAccess.item;
            }
        }
    }
}

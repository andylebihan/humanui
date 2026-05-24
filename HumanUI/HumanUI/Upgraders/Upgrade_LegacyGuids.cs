using System;
using Grasshopper.Kernel;

namespace HumanUI.Upgraders
{
    /// <summary>
    /// Simple GUID-swap upgraders for components whose only change between
    /// the historical (0.6.x / 0.7.x WPF) build and the current Eto port
    /// is the ComponentGuid. SwapComponents preserves wires and chunk
    /// state, so each upgrader is just an old→new GUID pair.
    ///
    /// One file rather than one-per-upgrader because these all share the
    /// trivial structure — splitting them would just add ceremony.
    /// </summary>
    public abstract class _SwapOnlyUpgrader : IGH_UpgradeObject
    {
        public abstract Guid UpgradeFrom { get; }
        public abstract Guid UpgradeTo { get; }
        public abstract DateTime Version { get; }

        public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
        {
            if (!(target is IGH_Component component)) return null;
            return GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
        }
    }

    /// <summary>0.6.x Create Button → current CreateButton_Component.</summary>
    public class Upgrade_CreateButton : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{b29e7c03-27ee-446c-894c-476261a807d1}");
        public override Guid UpgradeTo => new Guid("{9A5B87D6-046E-4C33-9ACA-5AF2F7503047}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.6.x Create Radio Button → current CreateRadioButton_Component.</summary>
    public class Upgrade_CreateRadioButton : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{c6c908a8-48aa-4cde-b959-dfb389b299a2}");
        public override Guid UpgradeTo => new Guid("{5B7E6AF2-EB03-477A-ABEB-3C0065593296}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.6.x Create File Picker → current CreateFilePicker_Component.</summary>
    public class Upgrade_CreateFilePicker : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{ea5d59c3-bb17-49e5-9967-1f46b21e3e51}");
        public override Guid UpgradeTo => new Guid("{76332FC9-F97C-4428-BEFE-723FA7C0AD37}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.6.x Launch Window → current LaunchWindow_Component.</summary>
    public class Upgrade_LaunchWindow : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{f9d46462-5227-4c4e-9268-0b678960d7a9}");
        public override Guid UpgradeTo => new Guid("{0A6B8A40-57A4-4D8D-9F09-F34869655D1E}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.6.x Capture Window → current CaptureWindow_Component.</summary>
    public class Upgrade_CaptureWindow : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{cbf6b72e-bfda-4d36-b114-80a1e8d942c6}");
        public override Guid UpgradeTo => new Guid("{900FCBA9-1B83-403E-B909-9293146469D8}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.7.x Create Gradient Editor → current CreateGradientEditor_Component.</summary>
    public class Upgrade_CreateGradientEditor : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{818ce7bb-0e46-4203-bc92-419bb786ba49}");
        public override Guid UpgradeTo => new Guid("{DA8DD6A3-AD57-471E-B891-94902C8647ED}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>
    /// 0.6.x MD Slider → current CreateSlider (HUI_FloatSlider). The
    /// underlying param shape between MD Slider and Slider isn't 1:1, so
    /// downstream wires may need a manual nudge, but the swap at least
    /// gets the component to load instead of showing Unrecognized Object.
    /// </summary>
    public class Upgrade_MDSlider : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{2E335FB4-447C-42D4-A10B-A7C9AB882757}");
        public override Guid UpgradeTo => new Guid("{C77ACC8A-FE64-43F0-9485-D23744F6152E}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>0.6.x Set MD Slider → current SetSlider_Component.</summary>
    public class Upgrade_SetMdSlider : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{04C34E5B-2A9F-4062-8330-EFAB05A86818}");
        public override Guid UpgradeTo => new Guid("{B412D7D3-02E2-4A8E-BDCC-2E1F8B2A8834}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>
    /// 0.7.x Set Graph Contents → current SetChart_LiveUpdate. Original
    /// "Graph" component evolved into "Chart"; param shape is identical
    /// (Chart to modify / values / names / Title / SubTitle).
    /// </summary>
    public class Upgrade_SetGraph : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{12A5A354-FC1B-4CEA-894A-BBEB71A23DB5}");
        public override Guid UpgradeTo => new Guid("{1C4AC2EC-3090-4D03-8D17-923417129692}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }

    /// <summary>
    /// First-generation Set Window Properties → current. Different from
    /// Upgrade_SetWindowPropertiesComponent2to3 in Upgrade_SetWindowPropertiesComponent.cs
    /// (that upgrader handles B2CA4D57); this one handles the even older
    /// aa3816cc that predates B2CA4D57.
    /// </summary>
    public class Upgrade_SetWindowProperties1to3 : _SwapOnlyUpgrader
    {
        public override Guid UpgradeFrom => new Guid("{aa3816cc-918e-4383-9125-8f00922f154a}");
        public override Guid UpgradeTo => new Guid("{14A1EE78-6536-43B2-B6D8-4B26A736F0A9}");
        public override DateTime Version => new DateTime(2024, 1, 1);
    }
}

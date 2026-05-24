using System;
using Grasshopper.Kernel;

namespace HumanUI.Upgraders
{
    /// <summary>
    /// Maps the historical Simple Grid GUID (Human UI 0.7.x WPF) to the
    /// Eto-port GUID introduced in Phase 1 of the migration. Parameter
    /// shapes are identical between the two so a straight SwapComponents
    /// covers it — no per-param reshuffling required.
    /// </summary>
    public class Upgrade_CreateSimpleGrid : IGH_UpgradeObject
    {
        public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
        {
            if (!(target is IGH_Component component)) return null;
            return GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
        }

        public Guid UpgradeFrom => new Guid("{a39c7889-7ce1-489a-8165-84cbe841dd67}");
        public Guid UpgradeTo => new Guid("{4df77b45-0d74-44ea-9445-6d5d8b1d17ad}");
        public DateTime Version => new DateTime(2024, 1, 1);
    }
}

using System;
using Grasshopper.Kernel;

namespace HumanUI.Upgraders
{
    /// <summary>
    /// Bridges the second-generation CreatePullDown_Component GUID (master's hidden
    /// CreatePullDown_Component_ALSO_DEPRECATED) directly to the current Eto-ported
    /// CreatePullDown_Component. The Eto branch dropped the intermediate component, so
    /// the existing Upgrade_CreatePulldownComponent (oldest -> middle) is followed by
    /// this upgrader (middle -> current) when a Beta 0.6.x file is opened.
    /// </summary>
    public class Upgrade_PullDownMidToCurrent : IGH_UpgradeObject
    {
        public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
        {
            if (!(target is IGH_Component component)) return null;
            return GH_UpgradeUtil.SwapComponents(component, UpgradeTo) as GH_Component;
        }

        public Guid UpgradeFrom => new Guid("{8F5B1D66-DE73-47A2-9678-9E59CEA106C0}");

        public Guid UpgradeTo => new Guid("{FC6AE741-ECD1-432F-ABB4-36B3F439C6F5}");

        public DateTime Version => new DateTime(2026, 5, 18);
    }
}

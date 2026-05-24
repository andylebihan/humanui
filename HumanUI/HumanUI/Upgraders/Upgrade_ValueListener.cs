using System;
using Grasshopper.Kernel;

namespace HumanUI.Upgraders
{
    /// <summary>
    /// Map the historical ValueListener GUID (0.6.x WPF) to the Eto-port
    /// GUID introduced in Phase 2 of the migration. Both versions write
    /// AddEventsEnabled into the same chunk key ("SomeProperty"), so
    /// SwapComponents propagates the saved Live/Manual state — no need
    /// to reference the deprecated type just to read it.
    /// </summary>
    public class Upgrade_ValueListener : IGH_UpgradeObject
    {
        public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
        {
            if (!(target is IGH_Component component)) return null;
            var swapped = GH_UpgradeUtil.SwapComponents(component, UpgradeTo);
            if (swapped is ValueListener_Component vl) vl.updateMessage();
            return swapped;
        }

        public Guid UpgradeFrom => new Guid("{78fb7e0c-ae2a-45ad-b09c-83df32d0b3bc}");
        public Guid UpgradeTo => new Guid("{D6BA0398-70A7-46E7-A068-274486EB0ACB}");
        public DateTime Version => new DateTime(2016, 2, 17);
    }
}

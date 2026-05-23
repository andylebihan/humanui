using System.Reflection;
using Grasshopper.Kernel;

namespace HumanUI
{
    /// <summary>
    /// Runs once at Grasshopper startup. Registers the "Human UI" category icon
    /// and prints a version/path stamp to Rhino's command line so it's obvious
    /// which build is loaded — useful for confirming yak installs on both
    /// Windows and Mac during the Eto migration.
    /// </summary>
    public class HUI_PriorityLoad : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("Human UI", Properties.Resources.Icon_16);

            try
            {
                var asm = typeof(HUI_PriorityLoad).Assembly;
                var version = asm.GetName().Version?.ToString() ?? "(unknown)";
                var location = asm.Location ?? "(in-memory)";
#if HUI_WINDOWS
                const string tfm = "net7.0-windows";
#else
                const string tfm = "net7.0";
#endif
                Rhino.RhinoApp.WriteLine($"HumanUI {version} ({tfm}) loaded from {location}");
            }
            catch
            {
                // Best-effort diagnostic — never fail the assembly load over a banner.
            }

            return GH_LoadingInstruction.Proceed;
        }
    }
}

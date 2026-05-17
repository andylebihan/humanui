using System.Threading;
using Xunit;

namespace HumanUI.Tests
{
    /// <summary>
    /// Phase 0 placeholder: the only live UI test we can run today is constructing the
    /// Eto MainWindow. The WPF-specific resource-resolution Theory data (every
    /// MahApps pack-URI and string-keyed lookup) is gone because the codebase no
    /// longer touches WPF resources. As Eto components are ported in phases 1-5 new
    /// Theory rows return here covering whatever Eto resources / theme keys the new
    /// implementations rely on.
    ///
    /// Eto widgets need a Platform initialized once per process before they can be
    /// instantiated. In Rhino that's done by the host; under xUnit we do it ourselves
    /// (Wpf backend; net7.0-windows test host). The Wpf backend also requires an STA
    /// thread, hence the manual thread setup in the runner.
    /// </summary>
    public class UiIntegrationTests
    {
        private static readonly object _initLock = new();
        private static bool _initialized;

        private static void EnsureEtoPlatform()
        {
            lock (_initLock)
            {
                if (_initialized) return;
                if (Eto.Platform.Instance == null)
                    Eto.Platform.Initialize(Eto.Platforms.Wpf);
                _initialized = true;
            }
        }

        // Skipped during Phase 0: Eto.Platform.Initialize(Eto.Platforms.Wpf) needs the
        // Wpf backend assembly with the SAME PublicKeyToken as the Eto.dll we compile
        // against (the McNeel-signed copy that lives inside RhinoCommon). The NuGet
        // Eto.Platform.Wpf is signed with a different key, so wiring it up under xUnit
        // is non-trivial and not worth solving in Phase 0. Re-enable once a real
        // Rhino-hosted integration loop exists.
        [Fact(Skip = "Requires Rhino-hosted Eto platform; smoke-tested in Rhino directly.")]
        public void MainWindow_ConstructsWithoutException()
        {
            HumanUIBaseApp.MainWindow win = null;
            System.Exception caught = null;
            var t = new Thread(() =>
            {
                try
                {
                    EnsureEtoPlatform();
                    win = new HumanUIBaseApp.MainWindow();
                }
                catch (System.Exception e) { caught = e; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (caught != null) throw new Xunit.Sdk.XunitException("MainWindow construction failed: " + caught);
            Assert.NotNull(win);
            Assert.Equal("MainWindow", win.Title);
        }
    }
}

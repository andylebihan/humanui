using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Grasshopper.Kernel;
using Xunit;

namespace HumanUI.Tests
{
    /// <summary>
    /// Safety-net tests that would have caught the "10 UI Main components
    /// silently excluded from compile" backlog. Each test walks the source
    /// tree or assembly metadata and asserts something about registration.
    /// </summary>
    public class ComponentRegistrationTests
    {
        private static readonly Assembly Asm = typeof(HumanUIInfo).Assembly;

        private static readonly List<Type> ComponentTypes = Asm.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(GH_Component).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .ToList();

        private static readonly Dictionary<Guid, Type> GuidToComponent = ComponentTypes
            .Select(t => new { t, Guid = ((GH_Component)Activator.CreateInstance(t)).ComponentGuid })
            .ToDictionary(x => x.Guid, x => x.t);

        private static readonly List<Type> UpgraderTypes = Asm.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(IGH_UpgradeObject).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
            .ToList();

        private static string RepoRoot()
        {
            // Walk up from the test asm location until we find the HumanUI source tree.
            var dir = new DirectoryInfo(Path.GetDirectoryName(typeof(ComponentRegistrationTests).Assembly.Location)!);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "HumanUI.sln")))
                dir = dir.Parent;
            return dir?.FullName;
        }

        /// <summary>
        /// Walk every .cs under HumanUI/HumanUI/Components/, extract the
        /// ComponentGuid string, and assert each one resolves to a live
        /// component in the loaded assembly. Catches the failure mode where
        /// a Compile Remove silently strips a Create*/Set* file.
        /// </summary>
        [Fact]
        public void EveryComponentInSourceTreeIsRegisteredInAssembly()
        {
            var root = RepoRoot();
            Assert.NotNull(root);

            var componentsDir = Path.Combine(root, "HumanUI", "HumanUI", "Components");
            Assert.True(Directory.Exists(componentsDir), $"Couldn't find Components/ under {root}");

            // Files we don't expect to be registered:
            //  - Create_TEMPLATE / Set_TEMPLATE / Set_TEMPLATE — skeletons.
            //  - CreateMDSlider / SetMdSlider — old WPF slider, superseded by
            //    CreateSlider (HUI_FloatSlider). Upgraders redirect the GUIDs.
            //  - CreateObjectsFromXaml — WPF-XAML only; GUID is covered by
            //    Legacy_BetaComponents_DEPRECATED stub.
            //  - The Hard 5 GH components on the WPF side that have Mac
            //    counterparts in MacStubs/. The HumanUI dll the test loads
            //    is the net7.0-windows build, so those WPF files ARE
            //    registered — no skip needed for them.
            var ignoredFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Create_TEMPLATE_Component.cs",
                "Set_TEMPLATE_Component.cs",
                "CreateMDSlider_Component.cs",
                "SetMdSlider_Component.cs",
                "CreateObjectsFromXaml_Component.cs",
            };

            var guidRegex = new Regex(@"ComponentGuid\s*=>?\s*new\s+Guid\(""\{?([0-9a-fA-F\-]+)\}?""\)", RegexOptions.Compiled);
            var missing = new List<string>();

            foreach (var path in Directory.EnumerateFiles(componentsDir, "*.cs", SearchOption.AllDirectories))
            {
                var filename = Path.GetFileName(path);
                if (ignoredFilenames.Contains(filename)) continue;

                var text = File.ReadAllText(path);
                var match = guidRegex.Match(text);
                if (!match.Success) continue;
                if (!Guid.TryParse(match.Groups[1].Value, out var guid)) continue;

                if (!GuidToComponent.ContainsKey(guid))
                {
                    var rel = Path.GetRelativePath(componentsDir, path);
                    missing.Add($"{rel} (GUID {guid})");
                }
            }

            Assert.True(missing.Count == 0,
                "Component .cs files present in tree but not registered in the assembly:\n" +
                string.Join("\n", missing));
        }

        /// <summary>
        /// Every IGH_UpgradeObject in the assembly must point at a registered
        /// active component. Catches upgraders that targeted a component
        /// later deleted, or that have a typo in UpgradeTo.
        /// </summary>
        [Fact]
        public void EveryUpgraderUpgradeToIsRegistered()
        {
            var bad = new List<string>();
            foreach (var upgraderType in UpgraderTypes)
            {
                var upgrader = (IGH_UpgradeObject)Activator.CreateInstance(upgraderType);
                if (!GuidToComponent.ContainsKey(upgrader.UpgradeTo))
                    bad.Add($"{upgraderType.FullName}: UpgradeTo {upgrader.UpgradeTo} is not a registered component");
            }
            Assert.True(bad.Count == 0, string.Join("\n", bad));
        }

        /// <summary>
        /// No upgrader's UpgradeFrom may collide with a non-deprecated
        /// active component — that would mean the active component
        /// triggers an upgrade on itself. Deprecated stubs (in the
        /// HumanUI.Deprecated namespace) are allowed to share the GUID
        /// with an upgrader: the upgrader runs first during load and
        /// SwapComponents replaces the deprecated instance with the
        /// current target, so the stub is just a fallback that catches
        /// .gh files where the upgrader didn't fire for some reason.
        /// </summary>
        [Fact]
        public void NoUpgraderUpgradeFromCollidesWithActiveNonDeprecatedComponent()
        {
            var collisions = new List<string>();
            foreach (var upgraderType in UpgraderTypes)
            {
                var upgrader = (IGH_UpgradeObject)Activator.CreateInstance(upgraderType);
                if (!GuidToComponent.TryGetValue(upgrader.UpgradeFrom, out var activeType)) continue;
                if (activeType.Namespace?.Contains("Deprecated") == true) continue;
                collisions.Add($"{upgraderType.FullName}: UpgradeFrom {upgrader.UpgradeFrom} also active as {activeType.FullName}");
            }
            Assert.True(collisions.Count == 0, string.Join("\n", collisions));
        }

        /// <summary>
        /// No two upgraders may claim the same old GUID — Grasshopper would
        /// pick whichever it sees first, which is undefined.
        /// </summary>
        [Fact]
        public void UpgraderUpgradeFromGuidsAreUnique()
        {
            var byFrom = new Dictionary<Guid, List<string>>();
            foreach (var upgraderType in UpgraderTypes)
            {
                var upgrader = (IGH_UpgradeObject)Activator.CreateInstance(upgraderType);
                if (!byFrom.TryGetValue(upgrader.UpgradeFrom, out var list))
                    byFrom[upgrader.UpgradeFrom] = list = new List<string>();
                list.Add(upgraderType.FullName);
            }
            var dups = byFrom.Where(kvp => kvp.Value.Count > 1)
                .Select(kvp => $"{kvp.Key}: {string.Join(", ", kvp.Value)}")
                .ToList();
            Assert.True(dups.Count == 0, "Duplicate UpgradeFrom:\n" + string.Join("\n", dups));
        }
    }
}

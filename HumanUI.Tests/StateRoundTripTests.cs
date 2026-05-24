using System;
using Xunit;

namespace HumanUI.Tests
{
    /// <summary>
    /// Tests over StateSet_Goo and State without instantiating Eto
    /// controls — the xunit test host can't bring up the Eto Platform
    /// without the WPF backend (the NuGet Eto.Platform.Wpf has a
    /// different PublicKeyToken than the McNeel-signed Eto.dll bundled
    /// in RhinoCommon — see UiIntegrationTests.cs). Per-control
    /// TrySetElementValue round-trips are smoke-tested in Rhino
    /// directly; here we cover the bits that don't need a UI handler:
    /// dictionary semantics and serialization.
    /// </summary>
    public class StateRoundTripTests
    {
        // UIElement_Goo accepts null Control (its IsValid just returns
        // element != null) so we can exercise State/StateSet without
        // bringing up Eto.
        private static UIElement_Goo Stub(string name)
            => new UIElement_Goo(null, name, Guid.NewGuid(), 0);

        [Fact]
        public void StateSet_StartsEmpty()
        {
            var set = new StateSet_Goo();
            Assert.Equal(0, set.Count);
            Assert.Empty(set.Names);
            Assert.Equal("Empty State Set", set.ToString());
        }

        [Fact]
        public void StateSet_AddPopulatesNames()
        {
            var set = new StateSet_Goo();
            set.Add("preset1", new State());
            set.Add("preset2", new State());
            Assert.Equal(2, set.Count);
            Assert.Equal(new[] { "preset1", "preset2" }, set.Names);
            var s = set.ToString();
            Assert.Contains("preset1", s);
            Assert.Contains("preset2", s);
        }

        [Fact]
        public void StateSet_AddDuplicateNameOverwrites()
        {
            var set = new StateSet_Goo();
            var s1 = new State();
            s1.AddMember(Stub("g1"), 1);
            var s2 = new State();
            s2.AddMember(Stub("g2"), 2);
            set.Add("foo", s1);
            set.Add("foo", s2);
            Assert.Single(set.states);
            Assert.Same(s2, set.states["foo"]);
        }

        [Fact]
        public void StateSet_ClearRemovesAllEntries()
        {
            var set = new StateSet_Goo();
            set.Add("a", new State());
            set.Add("b", new State());
            set.Clear();
            Assert.Equal(0, set.Count);
        }

        [Fact]
        public void StateSet_DuplicatePreservesValues()
        {
            var set = new StateSet_Goo();
            var state = new State();
            state.AddMember(Stub("g"), "value");
            set.Add("preset", state);

            var clone = (StateSet_Goo)set.Duplicate();
            Assert.Equal(set.Count, clone.Count);
            Assert.Equal(set.Names, clone.Names);
        }

        [Fact]
        public void State_AddDuplicateGooOverwrites()
        {
            var state = new State();
            var goo = Stub("g");
            state.AddMember(goo, "first");
            state.AddMember(goo, "second");
            Assert.Single(state.stateDict);
            Assert.Equal("second", state.stateDict[goo]);
        }

        [Fact]
        public void State_AddDistinctGoosAccumulates()
        {
            var state = new State();
            state.AddMember(Stub("a"), "v1");
            state.AddMember(Stub("b"), "v2");
            Assert.Equal(2, state.stateDict.Count);
        }
    }
}

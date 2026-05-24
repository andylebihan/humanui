using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel.Types;

namespace HumanUI
{
    /// <summary>
    /// GH_Goo wrapper for a name-keyed dictionary of UI element states.
    /// Drops the WPF dependency the WPF version carried; the underlying
    /// State is now keyed by UIElement_Goo (which already wraps an
    /// Eto.Forms.Control on both platforms) so this type is portable.
    /// </summary>
    public class StateSet_Goo : GH_Goo<Dictionary<string, State>>
    {
        public Dictionary<string, State> states { get; set; }
        public int Count => states.Count;

        public StateSet_Goo()
        {
            states = new Dictionary<string, State>();
        }

        public StateSet_Goo(Dictionary<string, State> _states)
        {
            states = _states;
        }

        public void Add(string name, State state)
        {
            // Overwrite-on-collision matches the WPF behavior. Save Element
            // States re-runs every solve and would otherwise accumulate
            // duplicates.
            if (states.ContainsKey(name)) states.Remove(name);
            states.Add(name, state);
        }

        public override IGH_Goo Duplicate() => new StateSet_Goo(states);

        public override bool IsValid => states != null;

        public override string ToString()
        {
            if (states.Count < 1) return "Empty State Set";
            return "State Set: \n" + string.Join("\n", states.Keys);
        }

        public void Clear() => states.Clear();

        public string[] Names => states.Keys.ToArray();

        public override string TypeDescription => "A collection of saved interface states";
        public override string TypeName => "UI Element State Collection";
    }

    /// <summary>
    /// Single named state — element → captured value mapping. Mirrors the
    /// shape of GetElementValue's return per element.
    /// </summary>
    public class State : Dictionary<UIElement_Goo, object>
    {
        public Dictionary<UIElement_Goo, object> stateDict { get; set; }

        public State()
        {
            stateDict = new Dictionary<UIElement_Goo, object>();
        }

        public void AddMember(UIElement_Goo u, object o)
        {
            if (stateDict.ContainsKey(u)) stateDict.Remove(u);
            stateDict.Add(u, o);
        }

        public override string ToString() => base.ToString() + stateDict.Count;
    }
}

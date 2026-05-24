using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HumanUIBaseApp;

namespace HumanUI.Components.UI_Main
{
    /// <summary>
    /// Snapshot the current values of a set of UI elements into a named
    /// state. The state set is serialized into the .gh file as a "shadow"
    /// — element values keyed by (componentGuid, outputIndex) — and
    /// rehydrated into a live State on the next solve once the actual
    /// Control instances exist in the window tree.
    /// </summary>
    public class SaveElementState_Component : GH_Component
    {
        public SaveElementState_Component()
            : base("Save Element States", "SaveStates",
                "This component lets you save the states of selected elements for later retrieval",
                "Human UI", "UI Main")
        {
            savedStates = new StateSet_Goo();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements", "E", "All Controls and other elements belonging to the window", GH_ParamAccess.list);
            pManager.AddTextParameter("Name Filter(s)", "F", "The optional filter(s) for the elements you want to save.", GH_ParamAccess.list);
            pManager[1].Optional = true;
            pManager.AddBooleanParameter("Save State", "S", "Set to true to save the current state of all selected elements", GH_ParamAccess.item, false);
            pManager.AddTextParameter("State Name", "N", "The name under which to save the state. If the name already exists, saved state will be overwritten", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Clear Saved States", "C", "Set to true to clear all saved states.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Saved States", "SS", @"The saved element states. Connect to a ""Restore State"" component to reinstate.", GH_ParamAccess.item);
            pManager.AddTextParameter("Saved State Names", "N", "The names of all currently saved states", GH_ParamAccess.list);
        }

        // Live state — used during the session. Shadow state — only simple
        // serializable types, written to the .gh file. We re-hydrate live
        // from shadow once the element tree is available again post-load.
        private Dictionary<string, Dictionary<Tuple<Guid, int>, object>> savedShadowStates = new();
        private StateSet_Goo savedStates;
        private bool hasProperlyGrabbedShadowElements;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First pass after deserialize: try to walk the shadow set and
            // hydrate it into live state. Falls through if any element isn't
            // yet hosted in a window (race against MainWindow setup).
            if (!hasProperlyGrabbedShadowElements)
            {
                try { hasProperlyGrabbedShadowElements = ShadowToState(); }
                catch (Exception e)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"It threw a {e.GetType().Name} error when restoring shadow states");
                }
            }

            // Defensive: if any live element has lost its parent (window
            // closed / reopened) reshape the live state from shadow.
            if (savedStates != null && AnyElementOrphaned()) ShadowToState();

            var elementObjects = new List<object>();
            var allElements = new List<KeyValuePair<string, UIElement_Goo>>();
            var elementFilters = new List<string>();
            string stateName = "Unnamed State";
            bool saveState = false;
            bool clearState = false;

            if (!DA.GetDataList("Elements", elementObjects)) return;
            if (!DA.GetData("State Name", ref stateName)) return;
            DA.GetData("Save State", ref saveState);
            DA.GetData("Clear Saved States", ref clearState);
            DA.GetDataList("Name Filter(s)", elementFilters);

            var filtered = new List<UIElement_Goo>();
            foreach (var o in elementObjects)
            {
                switch (o)
                {
                    case UIElement_Goo goo:
                        filtered.Add(goo);
                        break;
                    case GH_ObjectWrapper wrapper when wrapper.Value is KeyValuePair<string, UIElement_Goo> kvp:
                        allElements.Add(kvp);
                        break;
                }
            }

            if (allElements.Count > 0)
            {
                var elementDict = allElements.ToDictionary(p => p.Key, p => p.Value);
                if (elementFilters.Count > 0)
                {
                    foreach (var fil in elementFilters)
                        if (elementDict.TryGetValue(fil, out var goo))
                            filtered.Add(goo);
                }
                else
                {
                    foreach (var goo in elementDict.Values) filtered.Add(goo);
                }
            }

            if (clearState)
            {
                savedStates.Clear();
                savedShadowStates.Clear();
            }

            if (saveState)
            {
                var named = new State();
                foreach (var u in filtered)
                {
                    if (u?.element == null) continue;
                    var leaf = HUI_Util.extractBaseElement(u.element);
                    var value = HUI_Util.GetElementValue(leaf);
                    named.AddMember(u, value);
                }
                savedStates.Add(stateName, named);
            }

            DA.SetData("Saved States", savedStates);
            DA.SetDataList("Saved State Names", savedStates.Names);
        }

        private bool AnyElementOrphaned()
        {
            foreach (var state in savedStates.states.Values)
            {
                foreach (var goo in state.stateDict.Keys)
                {
                    if (HUI_Util.FindTopmostParent<MainWindow>(goo.element) == null)
                        return true;
                }
            }
            return false;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.SaveState;

        public override bool Write(GH_IWriter writer)
        {
            // The on-disk representation only uses primitive types so the
            // file survives even when the live Control tree doesn't exist
            // yet (loading a .gh fresh into Grasshopper).
            var stateSetChunk = writer.CreateChunk("stateSetChunk");
            stateSetChunk.SetInt32("StateCount", savedStates.states.Count);
            int i = 0;
            foreach (var statePair in savedStates.states)
            {
                var stateChunk = stateSetChunk.CreateChunk("State", i);
                stateChunk.SetString("stateName", statePair.Key);
                var state = statePair.Value;
                stateChunk.SetInt32("itemCount", state.stateDict.Count);
                int j = 0;
                foreach (var item in state.stateDict)
                {
                    var element = item.Key;
                    var value = item.Value;
                    var stateItemChunk = stateChunk.CreateChunk("stateItem", j);
                    stateItemChunk.SetString("ElementID", element.instanceGuid.ToString());
                    stateItemChunk.SetInt32("ElementIndex", element.index);

                    var stringValue = value?.ToString() ?? string.Empty;
                    var typeString = value?.GetType().ToString() ?? "null";
                    if (value is List<bool> bools)
                    {
                        typeString = "LIST OF BOOL";
                        stringValue = HUI_Util.stringFromBools(bools);
                    }
                    if (value is List<string> strs)
                    {
                        typeString = "LIST OF STRING";
                        stringValue = string.Join("|", strs);
                    }
                    stateItemChunk.SetString("ElementValue", stringValue);
                    stateItemChunk.SetString("ElementValueType", typeString);
                    j++;
                }
                i++;
            }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            var stateSet = new Dictionary<string, Dictionary<Tuple<Guid, int>, object>>();
            var stateSetChunk = reader.FindChunk("stateSetChunk");
            if (stateSetChunk != null)
            {
                int stateCount = stateSetChunk.GetInt32("StateCount");
                for (int i = 0; i < stateCount; i++)
                {
                    var state = new Dictionary<Tuple<Guid, int>, object>();
                    var stateChunk = stateSetChunk.FindChunk("State", i);
                    if (stateChunk == null) continue;
                    var stateName = stateChunk.GetString("stateName");
                    int itemCount = stateChunk.GetInt32("itemCount");
                    for (int j = 0; j < itemCount; j++)
                    {
                        var stateItemChunk = stateChunk.FindChunk("stateItem", j);
                        if (stateItemChunk == null) continue;
                        var elementID = new Guid(stateItemChunk.GetString("ElementID"));
                        int index = stateItemChunk.GetInt32("ElementIndex");
                        var elementValue = stateItemChunk.GetString("ElementValue");
                        var elementValueType = stateItemChunk.GetString("ElementValueType");
                        state[Tuple.Create(elementID, index)] = ParseValue(elementValue, elementValueType);
                    }
                    stateSet[stateName] = state;
                }
            }
            savedShadowStates = stateSet;
            return base.Read(reader);
        }

        private bool ShadowToState()
        {
            bool allParented = true;
            foreach (var shadowState in savedShadowStates)
            {
                var name = shadowState.Key;
                var live = new State();
                foreach (var proxy in shadowState.Value.Keys)
                {
                    var value = shadowState.Value[proxy];
                    var goo = GetElementGoo(OnPingDocument(), proxy.Item1, proxy.Item2);
                    if (goo == null) { allParented = false; continue; }
                    live.stateDict[goo] = value;
                    if (HUI_Util.FindTopmostParent<MainWindow>(goo.element) == null)
                        allParented = false;
                }
                savedStates.Add(name, live);
            }
            return allParented;
        }

        private static object ParseValue(string value, string valueType)
        {
            switch (valueType)
            {
                case "System.Double":
                    double.TryParse(value, out var dbl);
                    return dbl;
                case "System.Boolean":
                    bool.TryParse(value, out var bl);
                    return bl;
                case "LIST OF BOOL":
                    return HUI_Util.boolsFromString(value);
                case "LIST OF STRING":
                    return value.Split('|').ToList();
                case "System.Drawing.Color":
                    var parts = value.Split("=,]".ToCharArray());
                    int.TryParse(parts[1], out var a);
                    int.TryParse(parts[3], out var r);
                    int.TryParse(parts[5], out var g);
                    int.TryParse(parts[7], out var b);
                    return System.Drawing.Color.FromArgb(a, r, g, b);
                default:
                    return value;
            }
        }

        /// <summary>
        /// Re-hydrate a UIElement_Goo by chasing its (componentGuid, outputIndex)
        /// back into the active GH document. Used by Restore Element States
        /// when the saved state's Goo references a Control that hasn't been
        /// realized yet.
        /// </summary>
        public static UIElement_Goo GetElementGoo(GH_Document doc, Guid id, int index)
        {
            if (doc == null) return null;
            var comp = doc.FindComponent(id);
            if (comp == null) return null;
            var arr = comp.Params.Output[0].VolatileData.AllData(true).ToArray();
            return index < arr.Length ? arr[index] as UIElement_Goo : null;
        }

        public override Guid ComponentGuid => new Guid("{8b6f72d3-6eff-4d25-8faa-62065ab7663e}");
    }
}

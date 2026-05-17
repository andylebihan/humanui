using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;

namespace HumanUI
{
    /// <summary>
    /// Shared helpers across components. Phase 0 holds only the surface LaunchWindow and
    /// AddElements need; the full breadth (TrySetElementValue / GetElementValue /
    /// extractBaseElements / serialization helpers) will be re-introduced as Set/Get
    /// components are ported in phases 1-5.
    /// </summary>
    internal static class HUI_Util
    {
        /// <summary>
        /// Detach a control from its current parent so a new container can adopt it. This
        /// matters because the same UIElement_Goo can be passed to multiple Set / Container
        /// components in a single solve; each takeover has to undo the previous parenting.
        /// </summary>
        public static void removeParent(Control child)
        {
            if (child == null) return;
            var parent = child.Parent;
            if (parent == null) return;

            switch (parent)
            {
                case StackLayout stack:
                    for (int i = stack.Items.Count - 1; i >= 0; i--)
                    {
                        if (ReferenceEquals(stack.Items[i].Control, child))
                            stack.Items.RemoveAt(i);
                    }
                    break;
                case DynamicLayout dyn:
                    dyn.Clear();
                    break;
                case Panel panel when ReferenceEquals(panel.Content, child):
                    panel.Content = null;
                    break;
                case Scrollable scroll when ReferenceEquals(scroll.Content, child):
                    scroll.Content = null;
                    break;
                case GroupBox group when ReferenceEquals(group.Content, child):
                    group.Content = null;
                    break;
                case Container container:
                    // Last-resort fallback: ask the container to detach. Most Eto containers
                    // expose Detach(child) but the generic Container base doesn't promise it,
                    // so we don't rely on that path unless the specific cases above fail.
                    break;
            }
        }

        /// <summary>
        /// Add an element to a name-keyed result dictionary, disambiguating duplicate names
        /// by appending a count suffix. Mutates the Goo's display name to match.
        /// </summary>
        public static void AddToDict(UIElement_Goo e, Dictionary<string, UIElement_Goo> resultDict)
        {
            int tryCount = 0;
            string keyName = e.name;
            while (resultDict.ContainsKey(keyName))
            {
                tryCount++;
                keyName = $"{e.name} {tryCount:0}";
            }
            e.name = keyName;
            resultDict.Add(keyName, e);
        }

        // Pure-string serialization helpers used across multiple Set/Get components. Kept
        // alive through the migration because the .gh wire format for checklist / list
        // values uses these encodings and the tests cover both directions.

        public static List<bool> boolsFromString(string str)
        {
            var bools = new List<bool>();
            foreach (var s in str.Split(','))
            {
                Boolean.TryParse(s, out bool bl);
                bools.Add(bl);
            }
            return bools;
        }

        public static string stringFromBools(List<bool> bs)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var b in bs)
            {
                sb.Append(b);
                sb.Append(',');
            }
            return sb.ToString();
        }

        internal static string stringFromStrings(List<string> values) => string.Join("|", values);

        internal static List<string> stringsFromString(string value) => value.Split('|').ToList();
    }
}

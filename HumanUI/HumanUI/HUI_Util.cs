using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Grasshopper.Kernel.Types;

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
        /// Extract a typed control from whatever flavour of wrapper Grasshopper
        /// hands us. Returns the requested type whether it's an Eto Control (the
        /// common Tier-1 / containers case) or a WPF FrameworkElement reached
        /// through HUI_WpfHost (the Hard 5 / WPF-backed components). Set-*
        /// components always go through this single entry point.
        /// </summary>
        public static T GetUIElement<T>(object o) where T : class
        {
            switch (o)
            {
                case UIElement_Goo goo:
                    if (goo.element is T direct) return direct;
                    if (goo.element is HUI_WpfHost host && host.WpfElement is T wpf) return wpf;
                    return null;
                case GH_ObjectWrapper wrapper: return wrapper.Value as T;
                case HUI_WpfHost h when h.WpfElement is T w: return w;
                default: return o as T;
            }
        }

        /// <summary>
        /// Walk a container subtree depth-first for the first HUI_FloatSlider. Used by
        /// SetSlider / ValueListener.
        /// </summary>
        public static Components.UI_Elements.HUI_FloatSlider findSlider(Control control)
        {
            if (control == null) return null;
            if (control is Components.UI_Elements.HUI_FloatSlider slider) return slider;
            if (control is Container c)
            {
                foreach (var child in c.Controls)
                {
                    var found = findSlider(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Walk a container subtree depth-first looking for the first TextBox. Used by
        /// SetTextBox / ValueListener to peel a HUI textbox composite (StackLayout with
        /// optional Label + TextBox + optional Button) down to its TextBox child.
        /// </summary>
        public static TextBox findTextBox(Control control)
        {
            if (control == null) return null;
            if (control is TextBox tb) return tb;
            if (control is Container c)
            {
                foreach (var child in c.Controls)
                {
                    var found = findTextBox(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Peel a HUI composite container (TextBox stack, Pulldown stack, Slider stack)
        /// down to its single value-carrying control. Plain controls pass through.
        /// </summary>
        public static Control extractBaseElement(Control element)
        {
            if (element is StackLayout stack)
            {
                switch (stack.ID)
                {
                    case "GH_Slider":
                        return findSlider(stack);
                    case "GH_TextBox":
                    case "GH_TextBox_NoButton":
                        return findTextBox(stack);
                    case "GH_PullDown_Label":
                    case "GH_PullDown_NoLabel":
                        return FindFirst<DropDown>(stack);
                    case "GH_Toggle_Label":
                        return FindFirst<CheckBox>(stack);
                }
            }
            return element;
        }

        /// <summary>
        /// Walk a container subtree depth-first for the first descendant of type T.
        /// </summary>
        public static T FindFirst<T>(Control control) where T : Control
        {
            if (control is T match) return match;
            if (control is Container c)
            {
                foreach (var child in c.Controls)
                {
                    var found = FindFirst<T>(child);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Wrap a raw .NET value in the closest matching GH_Goo so it can be appended to
        /// a GH tree output.
        /// </summary>
        public static IGH_Goo GetRightType(object o)
        {
            switch (o)
            {
                case null: return null;
                case bool b: return new GH_Boolean(b);
                case int i: return new GH_Integer(i);
                case double d: return new GH_Number(d);
                case string s: return new GH_String(s);
                case System.Drawing.Color c: return new GH_Colour(c);
                default: return new GH_ObjectWrapper(o);
            }
        }

        /// <summary>
        /// Return the current "value" of a value-carrying control. Used by ValueListener.
        /// </summary>
        public static object GetElementValue(Control u)
        {
            switch (u)
            {
                case TextBox tb: return tb.Text;
                case CheckBox cb: return cb.Checked ?? false;
                case RadioButton rb: return rb.Checked;
                case Components.UI_Elements.HUI_FloatSlider slider: return slider.FloatValue;
                case HUI_RangeSlider range: return new[] { range.LowerValue, range.UpperValue };
                case ColorPicker cp:
                    {
                        // Eto.Drawing.Color stores ARGB as float 0..1. Convert
                        // back to the System.Drawing.Color GH expects for the
                        // Colour parameter type. Force opaque if AllowAlpha is
                        // off (the default): the picker doesn't expose an
                        // alpha slider, so any A=0 sitting in Value would be
                        // accidental — likely inherited from a Color.Empty
                        // upstream — and pushing it through would render
                        // downstream WPF materials fully transparent.
                        var c = cp.Value;
                        int alpha = cp.AllowAlpha ? (int)Math.Round(c.A * 255) : 255;
                        return System.Drawing.Color.FromArgb(
                            alpha,
                            (int)Math.Round(c.R * 255),
                            (int)Math.Round(c.G * 255),
                            (int)Math.Round(c.B * 255));
                    }
                case ListBox lb:
                    return (lb.SelectedValue as ListItem)?.Text ?? string.Empty;
                case DropDown dd:
                    return (dd.SelectedValue as ListItem)?.Text ?? string.Empty;
                case Label l: return l.Text;
                case HUI_RhPickButton pick: return pick.objIDs;
                case Button b: return b.Text;
                case FilePicker fp: return fp.Path;
                case Scrollable s when s.ID == "GH_Checklist": return CollectChecklistValues(s);
                case GridView gv: return CollectGridViewSelection(gv);
                case TabControl tabs:
                    return tabs.SelectedPage?.Text ?? string.Empty;
                case Expander exp: return exp.Expanded;
                // HUI_WpfHost wraps a WPF FrameworkElement for the Hard 5
                // (3D View, Charts, GraphMapper, GradientEditor,
                // ClickableShapeGrid). Returning the inner element (rather
                // than null) gives downstream Set/Get components something to
                // unwrap and avoids the silent-null cascade that triggered the
                // 3D-View-transparency bug when ColorPicker was missing here.
                case HUI_WpfHost host: return host.WpfElement;
                default: return null;
            }
        }

        /// <summary>
        /// Walk a checklist scrollable and return a parallel list of the current
        /// checked states (in declaration order). ValueListener flattens this
        /// into the output tree.
        /// </summary>
        private static List<bool> CollectChecklistValues(Scrollable s)
        {
            var values = new List<bool>();
            void walk(Control c)
            {
                if (c is CheckBox cb) values.Add(cb.Checked ?? false);
                else if (c is Container cont) foreach (var ch in cont.Controls) walk(ch);
            }
            walk(s);
            return values;
        }

        private static List<string> CollectGridViewSelection(GridView gv)
        {
            var values = new List<string>();
            if (gv.SelectedItem is string[] row)
            {
                values.AddRange(row);
            }
            return values;
        }

        /// <summary>
        /// Return the selected index of a list-based control, or -1 for scalar controls.
        /// </summary>
        public static object GetElementIndex(Control u)
        {
            switch (u)
            {
                case ListBox lb: return lb.SelectedIndex;
                case DropDown dd: return dd.SelectedIndex;
                case GridView gv: return gv.SelectedRow;
                default: return -1;
            }
        }

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

        /// <summary>
        /// Convert a System.Drawing.Color (Grasshopper-side) to a
        /// System.Windows.Media.Color (WPF-side). Used by the Hard 5 WPF
        /// components — kept on HUI_Util so component code stays terse.
        /// </summary>
        public static System.Windows.Media.Color ToMediaColor(System.Drawing.Color color)
            => System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}

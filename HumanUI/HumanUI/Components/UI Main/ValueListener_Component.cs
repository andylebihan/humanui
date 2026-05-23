using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using HumanUI.Components.UI_Elements;
#if HUI_WINDOWS
using ToolStripDropDown = System.Windows.Forms.ToolStripDropDown;
#endif

namespace HumanUI
{
    /// <summary>
    /// Listens for value changes on a set of HumanUI elements and pushes those values out
    /// as a GH tree. The Eto port carries forward the runtime fixes from the modernize-net7
    /// branch: eventedElements is per-instance (not static, no cross-talk), and event
    /// callbacks debounce via GH_Document.ScheduleSolution so a slider drag doesn't trigger
    /// 60 nested solves per second.
    /// </summary>
    public class ValueListener_Component : GH_Component, IGH_VariableParameterComponent
    {
        public ValueListener_Component()
            : base("Value Listener", "Values",
                "This component is used to retrieve the values of UI elements from the window. By default it will automatically refresh when those values change.",
                "Human UI", "UI Main")
        {
            updateMessage();
        }

        // Instance-level so two ValueListeners on a canvas don't trample each other's
        // wired-element bookkeeping.
        private readonly List<Control> eventedElements = new();

        // Trailing-edge debounce window for the GH solver.
        private const int DebounceMs = 50;

        internal bool AddEventsEnabled = true;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Elements", "E", "UI Element(s) to listen to. This can be retrieved either directly from the component \n that generated the element, or from the output of the \"Add Elements\" component.", GH_ParamAccess.list);
            pManager.AddTextParameter("Name Filter(s)", "F", "The optional filter(s) for the elements you want to listen for.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Values", "V", "The values of the listened elements", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Indices", "I", "For list-based objects (checklist, pulldown menu, etc) returns the selected index - otherwise returns -1.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var elementObjects = new List<object>();
            var allElements = new List<KeyValuePair<string, UIElement_Goo>>();
            var elementFilters = new List<string>();

            if (!DA.GetDataList("Elements", elementObjects)) return;
            DA.GetDataList("Name Filter(s)", elementFilters);

            // The Elements input can be either raw UIElement_Goo (from a Create-*
            // component) or KeyValuePair<string, UIElement_Goo> entries wrapped in
            // GH_ObjectWrapper (from AddElements' "Added Elements" output).
            var filteredElements = new List<Control>();
            foreach (var o in elementObjects)
            {
                switch (o)
                {
                    case UIElement_Goo goo when goo.element != null:
                        filteredElements.Add(goo.element);
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
                        if (elementDict.TryGetValue(fil, out var goo) && goo.element != null)
                            filteredElements.Add(goo.element);
                }
                else
                {
                    foreach (var goo in elementDict.Values)
                        if (goo?.element != null) filteredElements.Add(goo.element);
                }
            }

            // Unwire previously listened elements.
            foreach (var u in eventedElements) RemoveEvents(u);
            eventedElements.Clear();

            // Peel composite containers down to their value-carrying children.
            var leaves = filteredElements.Select(HUI_Util.extractBaseElement).Where(c => c != null).ToList();

            var values = new GH_Structure<IGH_Goo>();
            var indices = new GH_Structure<GH_Integer>();
            for (int i = 0; i < leaves.Count; i++)
            {
                var u = leaves[i];
                var value = HUI_Util.GetElementValue(u);
                var index = HUI_Util.GetElementIndex(u);
                var path = new GH_Path(i);

                if (value is string || value is null)
                {
                    values.Append(HUI_Util.GetRightType(value), path);
                }
                else if (value is IEnumerable list && !(value is string))
                {
                    foreach (var item in list)
                        values.Append(HUI_Util.GetRightType(item), path);
                }
                else
                {
                    values.Append(HUI_Util.GetRightType(value), path);
                }

                if (index is IEnumerable indList && !(index is string))
                {
                    foreach (int idx in indList)
                        indices.Append(new GH_Integer(idx), path);
                }
                else
                {
                    indices.Append(new GH_Integer((int)index), path);
                }

                if (AddEventsEnabled)
                {
                    eventedElements.Add(u);
                    AddEvents(u);
                }
            }

            DA.SetDataTree(0, values);
            DA.SetDataTree(1, indices);
        }

        // Wire change-notification handlers for the Tier-1 controls supported in the port.
        // Add new cases here as future phases re-introduce more element types.
        private void AddEvents(Control u)
        {
            switch (u)
            {
                case HUI_FloatSlider slider:
                    slider.ValueChanged -= ExpireThis;
                    slider.ValueChanged += ExpireThis;
                    break;
                case HUI_RangeSlider range:
                    range.RangeChanged -= ExpireThis;
                    range.RangeChanged += ExpireThis;
                    break;
                case ColorPicker cp:
                    cp.ValueChanged -= ExpireThis;
                    cp.ValueChanged += ExpireThis;
                    break;
                case GridView gv:
                    gv.SelectionChanged -= ExpireThis;
                    gv.SelectionChanged += ExpireThis;
                    break;
                case Scrollable s when s.ID == "GH_Checklist":
                    WireChecklistEvents(s, true);
                    break;
#if !HUI_WINDOWS
                case HUI_MultiShape ms:
                    ms.SelectionChanged -= ExpireThis;
                    ms.SelectionChanged += ExpireThis;
                    break;
#endif
                case TextBox tb when (tb.Tag as string) == "enterEvent":
                    tb.KeyDown -= OnTextBoxKeyPressed;
                    tb.KeyDown += OnTextBoxKeyPressed;
                    break;
                case TextBox tb:
                    tb.TextChanged -= ExpireThis;
                    tb.TextChanged += ExpireThis;
                    break;
                case CheckBox cb:
                    cb.CheckedChanged -= ExpireThis;
                    cb.CheckedChanged += ExpireThis;
                    break;
                case RadioButton rb:
                    rb.CheckedChanged -= ExpireThis;
                    rb.CheckedChanged += ExpireThis;
                    break;
                case ListBox lb:
                    lb.SelectedIndexChanged -= ExpireThis;
                    lb.SelectedIndexChanged += ExpireThis;
                    break;
                case DropDown dd:
                    dd.SelectedIndexChanged -= ExpireThis;
                    dd.SelectedIndexChanged += ExpireThis;
                    break;
                case HUI_RhPickButton pick:
                    pick.PickCompleted -= ExpireThis;
                    pick.PickCompleted += ExpireThis;
                    break;
                case FilePicker fp:
                    fp.PathChanged -= ExpireThis;
                    fp.PathChanged += ExpireThis;
                    break;
                case Button b:
                    b.Click -= ExpireThis;
                    b.Click += ExpireThis;
                    break;
            }
        }

        private void RemoveEvents(Control u)
        {
            switch (u)
            {
                case HUI_FloatSlider slider: slider.ValueChanged -= ExpireThis; break;
                case HUI_RangeSlider range: range.RangeChanged -= ExpireThis; break;
                case ColorPicker cp: cp.ValueChanged -= ExpireThis; break;
                case GridView gv: gv.SelectionChanged -= ExpireThis; break;
                case Scrollable s when s.ID == "GH_Checklist": WireChecklistEvents(s, false); break;
#if !HUI_WINDOWS
                case HUI_MultiShape ms: ms.SelectionChanged -= ExpireThis; break;
#endif
                case TextBox tb:
                    tb.TextChanged -= ExpireThis;
                    tb.KeyDown -= OnTextBoxKeyPressed;
                    break;
                case CheckBox cb: cb.CheckedChanged -= ExpireThis; break;
                case RadioButton rb: rb.CheckedChanged -= ExpireThis; break;
                case ListBox lb: lb.SelectedIndexChanged -= ExpireThis; break;
                case DropDown dd: dd.SelectedIndexChanged -= ExpireThis; break;
                case HUI_RhPickButton pick: pick.PickCompleted -= ExpireThis; break;
                case FilePicker fp: fp.PathChanged -= ExpireThis; break;
                case Button b: b.Click -= ExpireThis; break;
            }
        }

        /// <summary>
        /// Walk a checklist scrollable's CheckBox children and toggle subscription
        /// to CheckedChanged. Centralized so AddEvents and RemoveEvents stay in
        /// sync — they need to attach/detach the same handler for every CheckBox.
        /// </summary>
        private void WireChecklistEvents(Scrollable s, bool attach)
        {
            void walk(Control c)
            {
                if (c is CheckBox cb)
                {
                    cb.CheckedChanged -= ExpireThis;
                    if (attach) cb.CheckedChanged += ExpireThis;
                }
                else if (c is Container cont) foreach (var ch in cont.Controls) walk(ch);
            }
            walk(s);
        }

        private void OnTextBoxKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Keys.Enter) ExpireThis(sender, EventArgs.Empty);
        }

        private void ExpireThis(object sender, EventArgs e)
        {
            // Mark expired now, defer the actual solve. ScheduleSolution coalesces repeated
            // calls within DebounceMs so a slider drag becomes ~one solve when the user
            // pauses, not 60 nested re-entrant ExpireSolution(true) calls.
            ExpireSolution(false);
            OnPingDocument()?.ScheduleSolution(DebounceMs);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.ValueListener;

        public override Guid ComponentGuid => new Guid("{D6BA0398-70A7-46E7-A068-274486EB0ACB}");

        internal void updateMessage()
        {
            Message = AddEventsEnabled ? "Live" : "Manual Update";
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("SomeProperty", AddEventsEnabled);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            AddEventsEnabled = false;
            reader.TryGetBoolean("SomeProperty", ref AddEventsEnabled);
            updateMessage();
            return base.Read(reader);
        }

#if HUI_WINDOWS
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            GH_DocumentObject.Menu_AppendItem(menu, "Live Update", Menu_AddEventsClicked, true, AddEventsEnabled)
                .ToolTipText = "When checked, the component will automatically update when UI element values change in the window.";
        }
#endif

        public void Menu_AddEventsClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Add Events");
            AddEventsEnabled = !AddEventsEnabled;
            updateMessage();
            ExpireSolution(true);
        }

        // Optional manual-trigger input slot.

        public bool CanInsertParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Input && index == 2 && Params.Input.Count == 2;

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Input && index == 2 && Params.Input.Count == 3;

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            var trigger = new Param_Boolean
            {
                NickName = "T",
                Name = "Trigger",
                Description = "An optional input parameter to force trigger an update (useful when the component is in manual mode)",
                Optional = true,
            };
            Params.RegisterInputParam(trigger, index);
            return trigger;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
            => side == GH_ParameterSide.Input && index == 2;

        public void VariableParameterMaintenance() { }
    }
}

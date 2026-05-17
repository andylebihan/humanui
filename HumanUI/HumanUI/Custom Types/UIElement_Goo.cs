using System;
using Eto.Forms;
using Grasshopper.Kernel.Types;

namespace HumanUI
{
    /// <summary>
    /// GH_Goo wrapper around an Eto.Forms.Control. Carries the originating component's
    /// instance GUID and output index so SaveElementState / RestoreElementState can
    /// re-locate the control after a session restart, plus a human-readable name used by
    /// the dictionary-based Set/Get components.
    /// </summary>
    public class UIElement_Goo : GH_Goo<Control>
    {
        public Control element { get; set; }

        public string name { get; set; }
        public int index { get; set; }
        public Guid instanceGuid { get; set; }

        public UIElement_Goo(Control _element, string _name, Guid _id, int _index)
        {
            element = _element;
            name = _name;
            instanceGuid = _id;
            index = _index;
        }

        public override IGH_Goo Duplicate()
            => new UIElement_Goo(element, name, instanceGuid, index);

        public override bool IsValid => element != null;

        public override string ToString()
        {
            if (element == null) return $"UI Element {name} (uninitialized)";
            if (element is Container container)
                return $"Container UI Element {name}, Type {element.GetType().Name}, {CountChildren(container)} Children Elements";
            return $"UI Element {name}, Type: {element.GetType().Name}";
        }

        private static int CountChildren(Container c)
        {
            int n = 0;
            foreach (var _ in c.Controls) n++;
            return n;
        }

        public override string TypeDescription => "A UI element";

        public override string TypeName => "UI Element";
    }
}

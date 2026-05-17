using System.Collections.Generic;
using Eto.Drawing;
using Eto.Forms;

namespace HumanUIBaseApp
{
    /// <summary>
    /// The container Form for a Human UI window. Plays the same role the WPF MetroWindow
    /// did before the Eto migration: hosts a vertical StackLayout inside a Scrollable. UI
    /// components add their Eto controls via AddElement(); ValueListener and friends walk
    /// the same children to wire events and read values.
    /// </summary>
    public class MainWindow : Form
    {
        // Vertical stack of user-added controls. Always the sole child of MasterScrollable.
        private readonly StackLayout _masterStack;

        // Scrollable wrapper around the stack. Vertical scroll is always enabled; horizontal
        // is toggled via HorizontalScrollingEnabled to match the old MainWindow behaviour.
        private readonly Scrollable _masterScrollable;

        public MainWindow()
        {
            Title = "MainWindow";
            ClientSize = new Size(450, 596);

            _masterStack = new StackLayout
            {
                Orientation = Orientation.Vertical,
                Padding = new Padding(10),
                Spacing = 4,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };

            _masterScrollable = new Scrollable
            {
                Border = BorderType.None,
                ExpandContentWidth = true,
                ExpandContentHeight = false,
                Content = _masterStack,
            };

            Content = _masterScrollable;
        }

        /// <summary>
        /// Sets the window's title.
        /// </summary>
        public void setWindowName(string name) => Title = name;

        /// <summary>
        /// Whether horizontal scrolling is allowed inside the master scroll viewer.
        /// </summary>
        public bool HorizontalScrollingEnabled { get; set; }

        /// <summary>
        /// Sets the font of UI text. The Eto port applies a font family-only update; size
        /// and style are preserved per-control.
        /// </summary>
        public void setFont(string fontName)
        {
            var family = new FontFamily(fontName);
            ApplyFontFamily(_masterStack, family);
        }

        private static void ApplyFontFamily(Control control, FontFamily family)
        {
            if (control is TextControl tc && tc.Font != null)
                tc.Font = new Font(family, tc.Font.Size, tc.Font.FontStyle);
            if (control is Container container)
            {
                foreach (var child in container.Controls)
                    ApplyFontFamily(child, family);
            }
        }

        /// <summary>
        /// Adds a control to the master stack. Returns the resulting index.
        /// </summary>
        public int AddElement(Control elem)
        {
            _masterStack.Items.Add(new StackLayoutItem(elem, HorizontalAlignment.Stretch));
            return _masterStack.Items.Count - 1;
        }

        /// <summary>
        /// Adds a control at the specified index.
        /// </summary>
        public void AddElement(Control elem, int index)
        {
            _masterStack.Items.Insert(index, new StackLayoutItem(elem, HorizontalAlignment.Stretch));
        }

        /// <summary>
        /// Removes a control from the master stack. Returns the index it occupied, or -1
        /// if it was not found.
        /// </summary>
        public int RemoveFromStack(Control elem)
        {
            for (int i = 0; i < _masterStack.Items.Count; i++)
            {
                if (ReferenceEquals(_masterStack.Items[i].Control, elem))
                {
                    _masterStack.Items.RemoveAt(i);
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Removes all child controls from the master stack.
        /// </summary>
        public void clearElements()
        {
            _masterStack.Items.Clear();
        }

        /// <summary>
        /// Enumerates the controls currently in the master stack.
        /// </summary>
        public IEnumerable<Control> MasterStackChildren
        {
            get
            {
                foreach (var item in _masterStack.Items)
                {
                    if (item.Control != null) yield return item.Control;
                }
            }
        }
    }
}

using System;

namespace HumanUI
{
    /// <summary>
    /// Embed a raw WPF FrameworkElement inside an Eto.Forms.Panel. Works because
    /// Eto.Wpf's Panel handler keeps a System.Windows.Controls.Border as its
    /// ControlObject — assigning Border.Child to our WPF control hands the
    /// layout over to WPF directly while Eto continues to manage the outer
    /// container and overall window lifecycle. On non-WPF Eto backends
    /// (Eto.Mac) the cast fails silently and the panel renders empty; Hard 5
    /// components surface a stub message in that case.
    /// </summary>
    public sealed class HUI_WpfHost : Eto.Forms.Panel
    {
        public System.Windows.FrameworkElement WpfElement { get; }

        public HUI_WpfHost(System.Windows.FrameworkElement wpfElement)
        {
            WpfElement = wpfElement;
            try
            {
                if (ControlObject is System.Windows.Controls.Border border)
                {
                    border.Child = wpfElement;
                }
            }
            catch
            {
                // Eto backend isn't WPF (e.g. Eto.Mac). Caller's stub message stays visible.
            }
        }

        /// <summary>Convenience: walk a Goo or wrapper down to the inner WPF element.</summary>
        public static T Unwrap<T>(object o) where T : System.Windows.FrameworkElement
        {
            switch (o)
            {
                case UIElement_Goo goo: return Unwrap<T>(goo.element);
                case HUI_WpfHost host: return host.WpfElement as T;
                case Grasshopper.Kernel.Types.GH_ObjectWrapper wrap: return Unwrap<T>(wrap.Value);
                default: return o as T;
            }
        }
    }
}

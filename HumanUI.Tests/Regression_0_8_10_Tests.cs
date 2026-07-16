using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Grasshopper.Kernel;
using Xunit;

namespace HumanUI.Tests
{
    /// <summary>
    /// Regression coverage for the three defects reported against the 0.8.10 yak build
    /// (McNeel discourse thread "New HumanUI for Rhino 8"):
    ///
    ///   * posts #5 / #8 -- collapsible stacks/expanders collapse and throw a Grasshopper
    ///     breakpoint after a control changes. Root cause: HUI_Util.removeParent did not
    ///     detach elements owned by a ContentControl, which is how MahApps 2.x templates
    ///     surface container content, so the re-parent on the next solve threw.
    ///   * post #6 / #7 -- self-referential value listeners never settle, and adaptive
    ///     pulldowns raise "Object expired during a solution". Root cause: ExpireThis
    ///     expired on the WPF event thread with no guard for events raised during a solve.
    /// </summary>
    public class Regression_0_8_10_Tests
    {
        private static Application EnsureApplication()
            => Application.Current ?? new Application();

        // ---- posts #5 / #8 : removeParent must detach ContentControl-owned elements ----

        [StaFact]
        public void RemoveParent_DetachesElementOwnedByExpander()
        {
            EnsureApplication();
            var child = new Button();
            var expander = new Expander { Content = child };
            Assert.Same(expander, LogicalTreeHelper.GetParent(child));

            HUI_Util.removeParent(child);

            Assert.Null(expander.Content);
            Assert.Null(LogicalTreeHelper.GetParent(child));
        }

        [StaFact]
        public void RemoveParent_DetachesElementOwnedByScrollViewer()
        {
            EnsureApplication();
            var child = new Button();
            var scrollViewer = new ScrollViewer { Content = child };

            HUI_Util.removeParent(child);

            Assert.Null(scrollViewer.Content);
            Assert.Null(LogicalTreeHelper.GetParent(child));
        }

        [StaFact]
        public void RemoveParent_LeavesElementReAddableAfterExpander()
        {
            // The actual 0.8.10 breakpoint: AddElements clears and re-hosts every element on
            // each solve. If the element is still parented to its old container, WPF throws
            // "Specified element is already the logical child of another element" on the
            // re-add. This reproduces that clear-and-rebuild cycle and asserts it is silent.
            EnsureApplication();
            var child = new Button();
            var oldExpander = new Expander { Content = child };

            HUI_Util.removeParent(child);

            var newExpander = new Expander();
            Exception ex = Record.Exception(() => newExpander.Content = child);
            Assert.Null(ex);
            Assert.Same(newExpander, LogicalTreeHelper.GetParent(child));
        }

        [StaFact]
        public void RemoveParent_StillDetachesFromPanelAndBorder()
        {
            // Guard the pre-existing (working) cases so the hardening did not regress them.
            EnsureApplication();

            var panelChild = new Button();
            var panel = new StackPanel();
            panel.Children.Add(panelChild);
            HUI_Util.removeParent(panelChild);
            Assert.DoesNotContain(panelChild, panel.Children.Cast<UIElement>());

            var borderChild = new Button();
            var border = new Border { Child = borderChild };
            HUI_Util.removeParent(borderChild);
            Assert.Null(border.Child);
        }

        // ---- posts #6 / #7 : ValueListener must not expire on the WPF thread mid-solve ----

        [Fact]
        public void ExpireThis_WithNoActiveDocument_DoesNotThrow()
        {
            // WPF change events can fire while the component has no document (teardown, or a
            // control detached from the canvas). ExpireThis must null-guard OnPingDocument()
            // instead of dereferencing it.
            var component = new ValueListener_Component();
            MethodInfo expireThis = typeof(ValueListener_Component).GetMethod(
                "ExpireThis", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(expireThis);

            Exception ex = Record.Exception(
                () => expireThis.Invoke(component, new object[] { null, EventArgs.Empty }));
            Assert.Null(ex);
        }

        [Fact]
        public void ValueListener_DefersExpireThroughScheduleCallback()
        {
            // The fix routes the expire through GH_Document.ScheduleSolution's callback so it
            // runs on the solution thread. Pin the callback's presence and GH_ScheduleDelegate
            // shape (void method taking a GH_Document) so a refactor cannot quietly revert to
            // synchronous ExpireSolution on the WPF event thread.
            MethodInfo callback = typeof(ValueListener_Component).GetMethod(
                "ScheduleExpireCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(callback);
            Assert.Equal(typeof(void), callback.ReturnType);

            ParameterInfo[] parameters = callback.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(GH_Document), parameters[0].ParameterType);
        }

        [Fact]
        public void ValueListener_DebounceWindowIsSane()
        {
            FieldInfo debounce = typeof(ValueListener_Component).GetField(
                "DebounceMs", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            Assert.NotNull(debounce);
            int ms = (int)debounce.GetRawConstantValue();
            Assert.InRange(ms, 1, 500);
        }
    }
}

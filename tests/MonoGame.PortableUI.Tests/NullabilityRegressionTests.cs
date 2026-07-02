using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class NullabilityRegressionTests
    {
        [TestInitialize]
        public void ResetFocus()
        {
            ScreenEngine.FocusedControl = null;
        }

        [TestMethod]
        public void Content_control_replaces_and_clears_child_parent()
        {
            var host = new Border();
            var first = new Button { Text = "First" };
            var second = new Button { Text = "Second" };
            Control? lastContent = null;
            host.ContentChanged += (sender, args) => lastContent = args.NewControl;

            host.Content = first;
            host.Content = second;
            host.Content = null;

            Assert.IsNull(first.Parent);
            Assert.IsNull(second.Parent);
            Assert.IsNull(host.Content);
            Assert.IsNull(lastContent);
        }

        [TestMethod]
        public void Screen_content_can_be_cleared_and_detaches_previous_child()
        {
            var screen = new TestScreen();
            var content = new StackPanel();

            screen.Content = content;
            screen.Content = null;

            Assert.IsNull(content.Parent);
            Assert.IsNull(screen.Content);
        }

        [TestMethod]
        public void Control_collection_replacement_detaches_old_child()
        {
            var panel = new StackPanel();
            var first = new Button { Text = "First" };
            var second = new Button { Text = "Second" };

            panel.AddChild(first);
            panel.Children[0] = second;

            Assert.IsNull(first.Parent);
            Assert.AreSame(panel, second.Parent);
            Assert.AreSame(second, panel.Children[0]);
        }

        [TestMethod]
        public void Focus_events_report_nullable_old_and_new_elements()
        {
            var first = new Button { Text = "First" };
            var second = new Button { Text = "Second" };
            Control? oldElement = first;
            Control? newElement = null;
            first.GotFocus += (sender, args) => oldElement = args.OldElement;
            first.LostFocus += (sender, args) => newElement = args.NewElement;

            first.Focus();
            second.Focus();
            ScreenEngine.FocusedControl = null;

            Assert.IsNull(oldElement);
            Assert.AreSame(second, newElement);
        }

        [TestMethod]
        public void Context_menu_invokes_item_without_action()
        {
            var menu = new ContextMenu();
            MenuItem? invoked = null;
            var item = new MenuItem("No-op", null);
            menu.Items.Add(item);
            menu.ItemInvoked += (sender, args) => invoked = args.Item;
            var control = (StackPanel)menu.CreateControl(null, false);

            ((Button)control.Children[0]).OnClick();

            Assert.AreSame(item, invoked);
        }

        [TestMethod]
        public void Empty_tab_control_has_no_selected_item()
        {
            var tabs = new TabControl();

            tabs.SelectedIndex = 99;

            Assert.AreEqual(-1, tabs.SelectedIndex);
            Assert.IsNull(tabs.SelectedItem);
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class CompositeControlRegressionTests
    {
        [TestMethod]
        public void Tab_control_clamps_selected_index()
        {
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Header = "One", Content = new TextBlock { Text = "A" } });
            tabs.Items.Add(new TabItem { Header = "Two", Content = new TextBlock { Text = "B" } });

            tabs.SelectedIndex = 42;

            Assert.AreEqual(1, tabs.SelectedIndex);
            Assert.IsNotNull(tabs.SelectedItem);
            Assert.AreEqual("Two", tabs.SelectedItem.Header);
        }

        [TestMethod]
        public void Combo_box_clamps_selection_and_raises_event()
        {
            var comboBox = new ComboBox();
            comboBox.Items.Add("One");
            comboBox.Items.Add("Two");
            var changes = 0;
            comboBox.SelectionChanged += (sender, args) => changes++;

            comboBox.SelectedIndex = 99;

            Assert.AreEqual(1, comboBox.SelectedIndex);
            Assert.AreEqual("Two", comboBox.SelectedItem);
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void List_box_clamps_selection_and_raises_event_only_when_changed()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            var changes = 0;
            listBox.SelectionChanged += (sender, args) => changes++;

            listBox.SelectedIndex = 99;
            listBox.SelectedIndex = 99;

            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.AreEqual("Two", listBox.SelectedItem);
            Assert.AreEqual(1, changes);
        }

        [TestMethod]
        public void List_box_item_click_updates_selection_and_raises_item_invoked()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            ListBoxItemInvokedEventArgs? invoked = null;
            listBox.ItemInvoked += (sender, args) => invoked = args;
            listBox.UpdateLayout(new Rect(0, 0, 160, 80));

            listBox.ItemButtons[1].OnClick();

            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.IsNotNull(invoked);
            Assert.AreEqual(1, invoked.Index);
            Assert.AreEqual("Two", invoked.Item);
        }

        [TestMethod]
        public void List_box_clamps_removed_selection_on_next_layout()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            listBox.Items.Add("Three");
            listBox.SelectedIndex = 2;

            listBox.Items.RemoveAt(2);
            listBox.UpdateLayout(new Rect(0, 0, 160, 80));

            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.AreEqual("Two", listBox.SelectedItem);
        }

        [TestMethod]
        public void List_box_keyboard_navigation_clamps_at_edges()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");

            listBox.OnKeyPressed(KeyboardCommand.CursorDown);
            listBox.OnKeyPressed(KeyboardCommand.CursorDown);
            listBox.OnKeyPressed(KeyboardCommand.CursorDown);

            Assert.AreEqual(1, listBox.SelectedIndex);

            listBox.OnKeyPressed(KeyboardCommand.CursorUp);
            listBox.OnKeyPressed(KeyboardCommand.CursorUp);

            Assert.AreEqual(0, listBox.SelectedIndex);
        }

        [TestMethod]
        public void List_box_can_materialize_rows_while_attached_to_screen()
        {
            var screen = new TestScreen();
            var listBox = new ListBox
            {
                Width = 160,
                Height = 120
            };

            for (var i = 1; i <= 12; i++)
                listBox.Items.Add($"Item {i:00}");

            screen.Content = listBox;
            listBox.UpdateLayout(new Rect(0, 0, 160, 120));

            Assert.AreEqual(12, listBox.ItemButtons.Count);
        }

        [TestMethod]
        public void Combo_box_dropdown_factory_creates_list_box()
        {
            var comboBox = new ComboBox();
            comboBox.Items.Add("One");
            comboBox.Items.Add("Two");
            comboBox.SelectedIndex = 1;

            var listBox = comboBox.CreateDropDownListBox();

            Assert.AreEqual(2, listBox.Items.Count);
            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.AreEqual("Two", listBox.SelectedItem);
        }

        [TestMethod]
        public void Combo_box_dropdown_invocation_selects_item_and_closes_flyout()
        {
            var screen = new TestScreen();
            var comboBox = new ComboBox
            {
                Width = 160,
                Height = 32
            };
            comboBox.Items.Add("One");
            comboBox.Items.Add("Two");
            screen.Content = comboBox;
            comboBox.UpdateLayout(new Rect(0, 0, 160, 32));

            comboBox.OnClick();
            var listBox = screen.FlyOutContent as ListBox;
            Assert.IsNotNull(listBox);

            listBox.ItemButtons[1].OnClick();

            Assert.AreEqual(1, comboBox.SelectedIndex);
            Assert.AreEqual("Two", comboBox.SelectedItem);
            Assert.IsNull(screen.FlyOutContent);
        }

        [TestMethod]
        public void Empty_combo_box_does_not_open_dropdown()
        {
            var screen = new TestScreen();
            var comboBox = new ComboBox
            {
                Width = 160,
                Height = 32
            };
            screen.Content = comboBox;
            comboBox.UpdateLayout(new Rect(0, 0, 160, 32));

            comboBox.OnClick();

            Assert.AreEqual(-1, comboBox.SelectedIndex);
            Assert.IsNull(comboBox.SelectedItem);
            Assert.IsNull(screen.FlyOutContent);
        }

        [TestMethod]
        public void Context_menu_raises_item_invoked()
        {
            var menu = new ContextMenu();
            var invoked = "";
            menu.Items.Add(new MenuItem("Run", () => invoked = "action"));
            menu.ItemInvoked += (sender, args) => invoked = args.Item.Text;
            var control = (StackPanel)menu.CreateControl(null, false);

            ((Button)control.Children[0]).OnClick();

            Assert.AreEqual("Run", invoked);
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

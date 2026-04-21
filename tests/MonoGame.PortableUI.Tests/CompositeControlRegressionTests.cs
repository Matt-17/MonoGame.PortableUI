using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Controls;

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
    }
}

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ItemControlTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
        }

        private static (TestScreen Screen, VirtualInputSource Source, ListBox List) CreateList(Game game, SelectionMode mode)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(300, 300);
            var list = new ListBox { Width = 200, Height = 200, ItemHeight = 20, SelectionMode = mode, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            for (var i = 0; i < 6; i++)
                list.Items.Add($"Item {i}");
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            screen.Content = list;
            engine.NavigateToScreen(screen);
            screen.Update();
            return (screen, source, list);
        }

        private static void ClickItem(TestScreen screen, VirtualInputSource source, ListBox list, int index, params Keys[] held)
        {
            var at = new PointF(20, list.ItemButtons[index].ClippingRect.Top + 5);
            source.SetKeyboardState(new KeyboardState(held));
            source.SetPointer(at);
            screen.Update();
            source.SetPointer(at, leftDown: true);
            screen.Update();
            source.SetPointer(at);
            screen.Update();
            source.SetKeyboardState(new KeyboardState());
            screen.Update();
        }

        [TestMethod]
        public void Multiple_mode_toggles_items_on_click()
        {
            using var game = new Game();
            var (screen, source, list) = CreateList(game, SelectionMode.Multiple);

            ClickItem(screen, source, list, 1);
            ClickItem(screen, source, list, 3);
            CollectionAssert.AreEqual(new[] { 1, 3 }, list.SelectedIndices.ToArray());

            ClickItem(screen, source, list, 1);
            CollectionAssert.AreEqual(new[] { 3 }, list.SelectedIndices.ToArray());
            CollectionAssert.AreEqual(new object[] { "Item 3" }, list.SelectedItems.ToArray());
        }

        [TestMethod]
        public void Extended_mode_uses_ctrl_to_toggle_and_shift_for_ranges()
        {
            using var game = new Game();
            var (screen, source, list) = CreateList(game, SelectionMode.Extended);

            ClickItem(screen, source, list, 1);
            ClickItem(screen, source, list, 4, Keys.LeftShift);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list.SelectedIndices.ToArray());

            ClickItem(screen, source, list, 2, Keys.LeftControl);
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, list.SelectedIndices.ToArray(), string.Join(",", list.SelectedIndices));

            ClickItem(screen, source, list, 5);
            CollectionAssert.AreEqual(new[] { 5 }, list.SelectedIndices.ToArray(), "a plain click selects one item");
        }

        [TestMethod]
        public void Space_toggles_the_current_item_and_setting_selected_index_resets_the_set()
        {
            using var game = new Game();
            var (screen, source, list) = CreateList(game, SelectionMode.Multiple);
            list.SelectedIndex = 2;
            list.Focus();

            screen.HandleTextInput(' ');
            Assert.AreEqual(0, list.SelectedIndices.Count, "Space toggled item 2 off");
            screen.HandleTextInput(' ');
            CollectionAssert.AreEqual(new[] { 2 }, list.SelectedIndices.ToArray());

            ClickItem(screen, source, list, 4);
            list.SelectedIndex = 0;
            CollectionAssert.AreEqual(new[] { 0 }, list.SelectedIndices.ToArray());
        }

        [TestMethod]
        public void List_box_item_template_builds_custom_rows_and_refresh_rebuilds_them()
        {
            var built = 0;
            var list = new ListBox
            {
                ItemTemplate = item =>
                {
                    built++;
                    return new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = "★ " + item } } };
                }
            };
            list.Items.Add("a");
            list.Items.Add("b");
            list.UpdateLayout(new Rect(0, 0, 200, 200));

            Assert.AreEqual(2, built);
            Assert.IsInstanceOfType(list.ItemButtons[0].Content, typeof(StackPanel));

            list.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(2, built, "unchanged items keep their visual");

            list.Refresh();
            Assert.AreEqual(4, built);

            list.ItemTemplate = null;
            list.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual("a", list.ItemButtons[0].Text);
        }

        [TestMethod]
        public void Combo_box_template_renders_the_selected_item_and_the_dropdown_rows()
        {
            var combo = new ComboBox { ItemTemplate = item => new Border { Tag = item, Width = 30, Height = 10 } };
            combo.Items.AddRange(new object[] { "x", "y" });
            combo.SelectedIndex = 1;

            Assert.IsInstanceOfType(combo.Content, typeof(Border));
            Assert.AreEqual("y", ((Border)combo.Content!).Tag);

            var dropDown = combo.CreateDropDownListBox();
            dropDown.UpdateLayout(new Rect(0, 0, 200, 100));
            Assert.IsInstanceOfType(dropDown.ItemButtons[0].Content, typeof(Border));
        }
    }
}

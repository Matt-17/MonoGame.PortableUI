using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class CompositeControlRegressionTests
    {
        [TestInitialize]
        public void ResetState()
        {
            ScreenEngine.FocusedControl = null;
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

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
        public void List_box_item_mouse_down_updates_selection_without_invoking_item()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            ListBoxItemInvokedEventArgs? invoked = null;
            listBox.ItemInvoked += (sender, args) => invoked = args;
            listBox.UpdateLayout(new Rect(0, 0, 160, 80));

            listBox.ItemButtons[1].OnMouseDown(new MouseEventArgs(new PointF(10, 42), MouseButton.Left));

            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.AreEqual("Two", listBox.SelectedItem);
            Assert.IsNull(invoked);
            Assert.AreSame(listBox, ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void List_box_rows_do_not_use_button_focus_visual_or_pressed_animation()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.UpdateLayout(new Rect(0, 0, 160, 40));

            Assert.IsFalse(listBox.ShowFocusVisual);
            Assert.IsFalse(listBox.ItemButtons[0].ShowFocusVisual);
            Assert.IsFalse(listBox.ItemButtons[0].AnimatePressedState);
        }

        [TestMethod]
        public void List_box_mouse_drag_selects_entered_item_from_same_list()
        {
            var listBox = new ListBox();
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            listBox.Items.Add("Three");
            listBox.UpdateLayout(new Rect(0, 0, 160, 120));

            listBox.ItemButtons[0].OnMouseDown(new MouseEventArgs(new PointF(10, 14), MouseButton.Left));
            listBox.ItemButtons[2].OnMouseEnter(new MouseEventArgs(new PointF(10, 70), new List<MouseButton> { MouseButton.Left }));

            Assert.AreEqual(2, listBox.SelectedIndex);
            Assert.AreEqual("Three", listBox.SelectedItem);
        }

        [TestMethod]
        public void List_box_captured_mouse_release_invokes_only_the_started_item()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(160, 120);
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            var listBox = new ListBox
            {
                Width = 160,
                Height = 120
            };
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            listBox.Items.Add("Three");
            ListBoxItemInvokedEventArgs? invoked = null;
            listBox.ItemInvoked += (sender, args) => invoked = args;
            screen.Content = listBox;
            screen.InvalidateLayout(true);

            listBox.ItemButtons[0].OnMouseDown(new MouseEventArgs(new PointF(10, 14), MouseButton.Left));
            Assert.AreSame(listBox, screen.CapturedMouseControl);

            Assert.IsTrue(screen.RouteCapturedMouseMove(new PointF(10, 70), new List<MouseButton> { MouseButton.Left }));
            Assert.IsTrue(screen.RouteCapturedMouseUp(new PointF(10, 70), MouseButton.Left));

            Assert.AreEqual(2, listBox.SelectedIndex);
            Assert.AreEqual("Three", listBox.SelectedItem);
            Assert.IsNull(invoked);
            Assert.IsNull(screen.CapturedMouseControl);
        }

        [TestMethod]
        public void List_box_captured_mouse_click_invokes_started_item()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(160, 80);
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            var listBox = new ListBox
            {
                Width = 160,
                Height = 80
            };
            listBox.Items.Add("One");
            listBox.Items.Add("Two");
            ListBoxItemInvokedEventArgs? invoked = null;
            listBox.ItemInvoked += (sender, args) => invoked = args;
            screen.Content = listBox;
            screen.InvalidateLayout(true);

            listBox.ItemButtons[1].OnMouseDown(new MouseEventArgs(new PointF(10, 42), MouseButton.Left));
            Assert.IsTrue(screen.RouteCapturedMouseUp(new PointF(10, 42), MouseButton.Left));

            Assert.AreEqual(1, listBox.SelectedIndex);
            Assert.IsNotNull(invoked);
            Assert.AreEqual(1, invoked.Index);
            Assert.AreEqual("Two", invoked.Item);
            Assert.IsNull(screen.CapturedMouseControl);
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
        public void Combo_box_dropdown_opens_downward_from_top_edge()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(300, 200);
            var screen = new TestScreen();
            engine.NavigateToScreen(screen);
            var comboBox = new ComboBox
            {
                Width = 160,
                Height = 32
            };
            comboBox.Items.Add("One");
            comboBox.Items.Add("Two");
            screen.Content = comboBox;

            comboBox.OnClick();
            var listBox = screen.FlyOutContent as ListBox;
            Assert.IsNotNull(listBox);

            var expectedStartScaleY = 0.96f;
            var expectedStartTranslationY = -listBox.ClippingRect.Height * (1 - expectedStartScaleY) / 2;
            Assert.AreEqual(1, listBox.Scale.X, 0.001f);
            Assert.AreEqual(expectedStartScaleY, listBox.Scale.Y, 0.001f);
            Assert.AreEqual(0, listBox.Translation.X, 0.001f);
            Assert.AreEqual(expectedStartTranslationY, listBox.Translation.Y, 0.001f);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(120);
            listBox.UpdateTimers();

            Assert.AreEqual(1, listBox.Scale.X, 0.001f);
            Assert.AreEqual(1, listBox.Scale.Y, 0.001f);
            Assert.AreEqual(0, listBox.Translation.X, 0.001f);
            Assert.AreEqual(0, listBox.Translation.Y, 0.001f);
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

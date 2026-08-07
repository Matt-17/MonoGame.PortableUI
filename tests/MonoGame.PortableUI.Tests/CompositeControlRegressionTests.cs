using System;
using System.Collections.Generic;
using System.Linq;
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
            screen.PerformLayoutIfDirty();

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
            screen.PerformLayoutIfDirty();

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
        public void Slider_clamps_value_and_raises_value_changed()
        {
            var slider = new Slider
            {
                Minimum = 10,
                Maximum = 20
            };
            ValueChangedEventArgs? lastChange = null;
            slider.ValueChanged += (sender, args) => lastChange = args;

            slider.Value = 50;

            Assert.AreEqual(20, slider.Value);
            Assert.IsNotNull(lastChange);
            Assert.AreEqual(10, lastChange!.OldValue);
            Assert.AreEqual(20, lastChange.NewValue);
        }

        [TestMethod]
        public void Slider_geometry_tracks_value_percent()
        {
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                ThumbSize = 20,
                TrackHeight = 4
            };
            var rect = new Rect(0, 0, 120, 30);

            var track = slider.GetTrackRect(rect);
            var thumb = slider.GetThumbRect(rect);

            Assert.AreEqual(new Rect(10, 13, 100, 4), track);
            Assert.AreEqual(new Rect(50, 5, 20, 20), thumb);
        }

        [TestMethod]
        public void Slider_keyboard_commands_change_value()
        {
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 10,
                Value = 5,
                SmallChange = 2
            };

            slider.OnKeyPressed(KeyboardCommand.CursorRight);
            Assert.AreEqual(7, slider.Value);

            slider.OnKeyPressed(KeyboardCommand.CursorLeft);
            Assert.AreEqual(5, slider.Value);

            slider.OnKeyPressed(KeyboardCommand.End);
            Assert.AreEqual(10, slider.Value);

            slider.OnKeyPressed(KeyboardCommand.Home);
            Assert.AreEqual(0, slider.Value);
        }

        [TestMethod]
        public void Progress_bar_clamps_value_and_calculates_fill_rect()
        {
            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 200,
                Value = 50
            };

            Assert.AreEqual(new Rect(0, 0, 25, 10), progressBar.GetFillRect(new Rect(0, 0, 100, 10)));

            progressBar.Value = 300;

            Assert.AreEqual(200, progressBar.Value);
            Assert.AreEqual(new Rect(0, 0, 100, 10), progressBar.GetFillRect(new Rect(0, 0, 100, 10)));
        }

        [TestMethod]
        public void Check_box_glyph_kind_changes_marker_geometry()
        {
            var rect = new Rect(0, 0, 20, 20);
            var cross = CheckBox.GetCheckMarkRects(rect, 2, CheckBoxGlyphKind.Cross).ToArray();
            var check = CheckBox.GetCheckMarkRects(rect, 2, CheckBoxGlyphKind.Check).ToArray();

            Assert.IsTrue(cross.Length > 0);
            Assert.IsTrue(check.Length > 0);
            Assert.AreNotEqual(cross[0], check[0]);
        }

        [TestMethod]
        public void Content_presenter_measures_content_with_padding()
        {
            var presenter = new ContentPresenter
            {
                Padding = new Thickness(2, 3),
                Content = new FixedSizeControl(new Size(20, 10))
            };

            var size = presenter.MeasureLayout();

            Assert.AreEqual(24, size.Width);
            Assert.AreEqual(16, size.Height);
        }

        [TestMethod]
        public void Content_control_template_can_wrap_content_in_presenter()
        {
            var child = new FixedSizeControl(new Size(20, 10));
            var host = new TemplateHostControl
            {
                Content = child,
                Template = owner => new Border
                {
                    Padding = new Thickness(4),
                    Content = new ContentPresenter
                    {
                        Content = owner.Content
                    }
                }
            };

            var root = (Border)host.GetDescendants().Single();
            var presenter = (ContentPresenter)root.Content!;

            Assert.AreSame(host, root.Parent);
            Assert.AreSame(root, presenter.Parent);
            Assert.AreSame(presenter, child.Parent);
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

        private sealed class TemplateHostControl : ContentControl
        {
        }

        private sealed class FixedSizeControl : Control
        {
            private readonly Size _size;

            public FixedSizeControl(Size size)
            {
                _size = size;
            }

            public override Size MeasureLayout()
            {
                return _size;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Regression tests for the fixes from docs/audit.md (input & behavior pass).</summary>
    [TestClass]
    public class AuditFixRegressionTests
    {
        [TestMethod]
        public void Enter_activates_focused_button()
        {
            var button = new Button { Text = "Go" };
            var clicks = 0;
            button.Click += (_, _) => clicks++;

            button.OnKeyPressed(KeyboardCommand.Enter);

            Assert.AreEqual(1, clicks);
        }

        [TestMethod]
        public void Space_activates_focused_button()
        {
            var button = new Button { Text = "Go" };
            var clicks = 0;
            button.Click += (_, _) => clicks++;

            button.OnKeyPressed(' ');

            Assert.AreEqual(1, clicks);
        }

        [TestMethod]
        public void Enter_toggles_focused_checkbox()
        {
            var checkBox = new CheckBox { Text = "Opt-in" };

            checkBox.OnKeyPressed(KeyboardCommand.Enter);
            Assert.IsTrue(checkBox.IsChecked);

            checkBox.OnKeyPressed(KeyboardCommand.Enter);
            Assert.IsFalse(checkBox.IsChecked);
        }

        [TestMethod]
        public void Enter_with_modifier_does_not_activate()
        {
            var button = new Button { Text = "Go" };
            var clicks = 0;
            button.Click += (_, _) => clicks++;

            button.OnKeyPressed(KeyboardCommand.Enter, KeyboardModifiers.Control);

            Assert.AreEqual(0, clicks);
        }

        [TestMethod]
        public void Clicking_selected_radio_fires_no_spurious_checked_events()
        {
            var first = new RadioButton { RadioGroup = "audit-group-1" };
            var second = new RadioButton { RadioGroup = "audit-group-1" };
            Assert.IsTrue(first.IsChecked); // first registered member is auto-checked

            var events = new List<bool>();
            first.Checked += (_, args) => events.Add(args.IsChecked);

            first.OnClick(); // already selected: must stay checked, no events

            Assert.IsTrue(first.IsChecked);
            Assert.AreEqual(0, events.Count);

            second.OnClick(); // moves the selection

            Assert.IsFalse(first.IsChecked);
            Assert.IsTrue(second.IsChecked);
            CollectionAssert.AreEqual(new[] { false }, events);
        }

        [TestMethod]
        public void Mouse_down_on_label_does_not_steal_focus()
        {
            var textBox = new TextBox();
            var label = new TextBlock { Text = "Just a label" };
            ScreenEngine.FocusedControl = textBox;

            label.OnMouseDown(new MouseEventArgs(new PointF(1, 1), MouseButton.Left));

            Assert.AreSame(textBox, ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Right_click_does_not_steal_focus()
        {
            var textBox = new TextBox();
            var button = new Button { Text = "Menu" };
            ScreenEngine.FocusedControl = textBox;

            button.OnMouseDown(new MouseEventArgs(new PointF(1, 1), MouseButton.Right));

            Assert.AreSame(textBox, ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Left_click_focuses_focusable_control()
        {
            var textBox = new TextBox();
            var button = new Button { Text = "Go" };
            ScreenEngine.FocusedControl = textBox;

            button.OnMouseDown(new MouseEventArgs(new PointF(1, 1), MouseButton.Left));

            Assert.AreSame(button, ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Margin_area_is_not_hit_testable()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 120);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            var button = new Button
            {
                Width = 80,
                Height = 40,
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Text = "Run"
            };
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            screen.InputSource = source;
            screen.Content = button;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            // (5,5) lies inside the margin box but outside the content box: must not click.
            Click(screen, source, new PointF(5, 5));
            Assert.AreEqual(0, clicks);

            // (30,30) lies inside the content box: must click.
            Click(screen, source, new PointF(30, 30));
            Assert.AreEqual(1, clicks);
        }

        [TestMethod]
        public void Rect_contains_is_inclusive_left_top_exclusive_right_bottom()
        {
            var rect = new Rect(0, 0, 10, 10);

            Assert.IsTrue(rect.Contains(new PointF(0, 0)));
            Assert.IsTrue(rect.Contains(new PointF(9.5f, 9.5f)));
            Assert.IsFalse(rect.Contains(new PointF(10, 10)));
            Assert.IsFalse(rect.Contains(new PointF(10, 5)));
            Assert.IsFalse(rect.Contains(new PointF(5, 10)));
        }

        [TestMethod]
        public void Slider_is_draggable_by_touch()
        {
            var slider = new Slider { Minimum = 0, Maximum = 100, Width = 120, Height = 24, ThumbSize = 20 };
            slider.UpdateLayout(new Rect(0, 0, 120, 24));

            slider.OnTouchDown(new TouchEventArgs(new PointF(110, 12)));
            var afterDown = slider.Value;
            slider.OnTouchMove(new TouchEventArgs(new PointF(60, 12)));
            var afterMove = slider.Value;
            slider.OnTouchUp(new TouchEventArgs(new PointF(60, 12)));

            Assert.IsTrue(afterDown > 90, $"touch-down should jump near the max, got {afterDown}");
            Assert.IsTrue(afterMove < afterDown, "touch-move should drag the value back down");
        }

        [TestMethod]
        public void Flyout_below_placement_anchors_content_top_left()
        {
            var belowContent = new Border { Width = 50, Height = 20, Margin = new Thickness(0) };
            var below = new FlyOut(new PointF(20, 30), false, FlyOutPlacement.Below) { Content = belowContent };
            below.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(30, belowContent.BoundingRect.Top, 0.001f);
            Assert.AreEqual(20, belowContent.BoundingRect.Left, 0.001f);

            var aboveContent = new Border { Width = 50, Height = 20, Margin = new Thickness(0) };
            var above = new FlyOut(new PointF(20, 90), false) { Content = aboveContent };
            above.UpdateLayout(new Rect(0, 0, 200, 200));
            Assert.AreEqual(70, aboveContent.BoundingRect.Top, 0.001f);
        }

        [TestMethod]
        public void Progress_bar_raises_value_changed()
        {
            var progressBar = new ProgressBar { Minimum = 0, Maximum = 10 };
            var events = new List<(float Old, float New)>();
            progressBar.ValueChanged += (_, args) => events.Add((args.OldValue, args.NewValue));

            progressBar.Value = 4;
            progressBar.Value = 4; // unchanged: no event
            progressBar.Value = 25; // clamps to 10

            CollectionAssert.AreEqual(new[] { (0f, 4f), (4f, 10f) }, events);
        }

        [TestMethod]
        public void Tab_control_raises_selection_changed_with_indexes()
        {
            var tabControl = new TabControl();
            tabControl.Items.Add(new TabItem { Header = "One" });
            tabControl.Items.Add(new TabItem { Header = "Two" });
            SelectionChangedEventArgs? received = null;
            tabControl.SelectionChanged += (_, args) => received = args;

            tabControl.SelectedIndex = 1;

            Assert.IsNotNull(received);
            Assert.AreEqual(0, received.OldIndex);
            Assert.AreEqual(1, received.NewIndex);
        }

        [TestMethod]
        public void List_box_selection_changed_carries_old_and_new_index()
        {
            var listBox = new ListBox();
            listBox.Items.Add("a");
            listBox.Items.Add("b");
            SelectionChangedEventArgs? received = null;
            listBox.SelectionChanged += (_, args) => received = args;

            listBox.SelectedIndex = 1;

            Assert.IsNotNull(received);
            Assert.AreEqual(-1, received.OldIndex);
            Assert.AreEqual(1, received.NewIndex);
        }

        [TestMethod]
        public void Property_changes_coalesce_into_one_layout_pass_per_update()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 120);
            var screen = new TestScreen();
            var button = new Button { Width = 80, Height = 40, Text = "A" };
            screen.Content = button;
            engine.NavigateToScreen(screen);
            screen.Update(); // initial layout

            var passesAfterInit = engine.LayoutPassesThisFrame;
            button.Width = 90;
            button.Height = 50;
            button.Margin = new Thickness(4);
            button.Text = "B";
            screen.Update();

            Assert.AreEqual(passesAfterInit + 1, engine.LayoutPassesThisFrame, "N property changes must coalesce into one layout pass");
            Assert.AreEqual(90, button.ClippingRect.Width, 0.001f);

            screen.Update(); // nothing changed: no further pass
            Assert.AreEqual(passesAfterInit + 1, engine.LayoutPassesThisFrame);
        }

        [TestMethod]
        public void From_palette_populates_toggle_switch_badge_and_data_grid_slots()
        {
            var palette = new ThemePalette
            {
                Selection = new Color(20, 126, 133),
                SelectionText = Color.White,
                Danger = new Color(180, 30, 30),
                Surface = new Color(240, 240, 240),
                SurfaceAlt = new Color(225, 225, 225),
                FieldFrame = new Color(210, 210, 210),
                MutedText = new Color(90, 90, 90),
                TabText = Color.Black
            };

            var theme = PortableTheme.FromPalette(palette);

            Assert.AreEqual(palette.Selection, ((Media.SolidColorBrush)theme.ToggleSwitchOnTrackBrush).Color);
            Assert.AreEqual(palette.FieldFrame, ((Media.SolidColorBrush)theme.ToggleSwitchOffTrackBrush).Color);
            Assert.AreEqual(palette.Danger, ((Media.SolidColorBrush)theme.BadgeBackgroundBrush).Color);
            // Dark danger red gets white text; the knob contrasts the selection color.
            Assert.AreEqual(Color.White, theme.BadgeTextColor);
            Assert.AreEqual(Color.White, ((Media.SolidColorBrush)theme.ToggleSwitchKnobBrush).Color);
            Assert.AreEqual(palette.Surface, ((Media.SolidColorBrush)theme.DataGridHeaderBackgroundBrush).Color);
            Assert.AreEqual(palette.SurfaceAlt, ((Media.SolidColorBrush)theme.DataGridAlternateRowBackgroundBrush).Color);
            Assert.AreEqual(palette.TabText, theme.DataGridHeaderTextColor);
        }

        [TestMethod]
        public void Badge_text_contrasts_light_danger_color()
        {
            var palette = new ThemePalette { Danger = new Color(255, 220, 100) };

            var theme = PortableTheme.FromPalette(palette);

            Assert.AreEqual(Color.Black, theme.BadgeTextColor);
        }

        private static void Click(Screen screen, VirtualInputSource source, PointF position)
        {
            source.SetPointer(position);
            screen.Update();
            source.SetPointer(position, leftDown: true);
            screen.Update();
            source.SetPointer(position);
            screen.Update();
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

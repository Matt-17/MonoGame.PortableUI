using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class BadgeCoverageTests
    {
        [TestMethod]
        public void Badge_is_hidden_at_zero_count_by_default()
        {
            var badge = new Badge();

            Assert.AreEqual(0, badge.Count);
            Assert.IsFalse(badge.IsVisible);
            Assert.IsFalse(badge.IsFocusable);
        }

        [TestMethod]
        public void Badge_becomes_visible_for_positive_count_and_hides_again_at_zero()
        {
            var badge = new Badge { Count = 3 };

            Assert.IsTrue(badge.IsVisible);
            Assert.AreEqual("3", Label(badge).Text);

            badge.Count = 0;

            Assert.IsFalse(badge.IsVisible);
        }

        [TestMethod]
        public void Badge_negative_count_stays_hidden()
        {
            var badge = new Badge { Count = -2 };

            Assert.IsFalse(badge.IsVisible);
        }

        [TestMethod]
        public void Badge_caps_label_above_ninety_nine()
        {
            var badge = new Badge { Count = 99 };
            Assert.AreEqual("99", Label(badge).Text);

            badge.Count = 100;
            Assert.AreEqual("99+", Label(badge).Text);

            badge.Count = 12345;
            Assert.AreEqual("99+", Label(badge).Text);
            Assert.AreEqual(12345, badge.Count);
        }

        [TestMethod]
        public void Badge_show_zero_keeps_zero_count_visible()
        {
            var badge = new Badge { ShowZero = true };

            Assert.IsTrue(badge.IsVisible);
            Assert.AreEqual("0", Label(badge).Text);

            badge.ShowZero = false;

            Assert.IsFalse(badge.IsVisible);
        }

        [TestMethod]
        public void Badge_dot_is_visible_without_count_and_hides_label()
        {
            var badge = new Badge { Dot = true };

            Assert.IsTrue(badge.IsVisible);
            Assert.IsFalse(Label(badge).IsVisible);
            Assert.AreEqual(10, badge.MinWidth);
            Assert.AreEqual(10, badge.MinHeight);
            Assert.AreEqual(0, badge.Padding.Left);
        }

        [TestMethod]
        public void Badge_dot_off_restores_pill_metrics_and_label()
        {
            var badge = new Badge { Count = 5, Dot = true };

            badge.Dot = false;

            Assert.IsTrue(badge.IsVisible);
            Assert.IsTrue(Label(badge).IsVisible);
            Assert.AreEqual(20, badge.MinWidth);
            Assert.AreEqual(20, badge.MinHeight);
            Assert.AreEqual(6, badge.Padding.Left);
            Assert.AreEqual(1, badge.Padding.Top);
        }

        [TestMethod]
        public void Badge_dot_off_with_zero_count_hides_badge()
        {
            var badge = new Badge { Dot = true };

            badge.Dot = false;

            Assert.IsFalse(badge.IsVisible);
        }

        [TestMethod]
        public void Badge_measures_at_least_its_minimum_size_plus_margin()
        {
            var pill = new Badge { Count = 1, Margin = new Thickness(2) };
            var pillSize = pill.MeasureLayout();
            Assert.IsTrue(pillSize.Width >= 24, $"pill width {pillSize.Width}");
            Assert.IsTrue(pillSize.Height >= 24, $"pill height {pillSize.Height}");

            var dot = new Badge { Dot = true };
            var dotSize = dot.MeasureLayout();
            Assert.IsTrue(dotSize.Width >= 10, $"dot width {dotSize.Width}");
            Assert.IsTrue(dotSize.Height >= 10, $"dot height {dotSize.Height}");
        }

        [TestMethod]
        public void Badge_color_setter_replaces_background_with_solid_brush()
        {
            var badge = new Badge { BadgeColor = Color.Orange, TextColor = Color.Navy };

            Assert.AreEqual(Color.Orange, badge.BadgeColor);
            Assert.AreEqual(Color.Orange, ((SolidColorBrush)badge.BackgroundBrush!).Color);
            Assert.AreEqual(Color.Navy, Label(badge).TextColor);
            Assert.AreEqual(Color.Navy, badge.TextColor);
        }

        [TestMethod]
        public void Badge_theme_switch_reseeds_untouched_colors_and_keeps_overrides()
        {
            var themeA = PortableTheme.CreateDefault();
            themeA.BadgeBackgroundBrush = new SolidColorBrush(Color.Red);
            themeA.BadgeTextColor = Color.White;
            var themeB = PortableTheme.CreateDefault();
            themeB.BadgeBackgroundBrush = new SolidColorBrush(Color.Green);
            themeB.BadgeTextColor = Color.Black;

            var island = new ThemeIsland { Theme = themeA };
            var untouched = new Badge { Count = 1 };
            var overridden = new Badge { Count = 1 };
            var panel = new StackPanel();
            panel.AddChild(untouched);
            panel.AddChild(overridden);
            island.Content = panel;
            untouched.RefreshThemeResources();
            overridden.RefreshThemeResources();
            overridden.BadgeColor = Color.Purple;
            overridden.TextColor = Color.Yellow;

            Assert.AreSame(themeA.BadgeBackgroundBrush, untouched.BackgroundBrush);
            Assert.AreEqual(Color.Red, untouched.BadgeColor);

            island.Theme = themeB;
            untouched.RefreshThemeResources();
            overridden.RefreshThemeResources();

            Assert.AreSame(themeB.BadgeBackgroundBrush, untouched.BackgroundBrush);
            Assert.AreEqual(Color.Green, untouched.BadgeColor);
            Assert.AreEqual(Color.Black, untouched.TextColor);
            Assert.AreEqual(Color.Purple, overridden.BadgeColor);
            Assert.AreEqual(Color.Yellow, overridden.TextColor);
        }

        private static TextBlock Label(Badge badge)
        {
            return (TextBlock)badge.Content!;
        }
    }

    [TestClass]
    public class ToggleSwitchCoverageTests
    {
        [TestInitialize]
        public void ResetState()
        {
            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        [TestMethod]
        public void Toggle_switch_defaults_to_off_with_pill_size()
        {
            var toggle = new ToggleSwitch();

            Assert.IsFalse(toggle.IsOn);
            Assert.AreEqual(96, toggle.Width);
            Assert.AreEqual(52, toggle.Height);
            Assert.AreEqual(5, toggle.KnobInset);
            Assert.IsTrue(toggle.ShowFocusVisual);
            Assert.IsNotNull(toggle.OffTrackBrush);
            Assert.IsNotNull(toggle.OnTrackBrush);
            Assert.IsNotNull(toggle.KnobBrush);
        }

        [TestMethod]
        public void Toggle_switch_click_flips_state_and_raises_toggled()
        {
            var toggle = new ToggleSwitch();
            var events = new List<bool>();
            toggle.Toggled += (sender, args) => events.Add(args.IsChecked);

            toggle.OnClick();
            Assert.IsTrue(toggle.IsOn);

            toggle.OnClick();
            Assert.IsFalse(toggle.IsOn);

            CollectionAssert.AreEqual(new[] { true, false }, events);
        }

        [TestMethod]
        public void Toggle_switch_setting_same_value_does_not_raise_toggled()
        {
            var toggle = new ToggleSwitch();
            var calls = 0;
            toggle.Toggled += (sender, args) => calls++;

            toggle.IsOn = false;
            Assert.AreEqual(0, calls);

            toggle.IsOn = true;
            toggle.IsOn = true;
            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void Toggle_switch_enter_and_space_toggle_but_modified_keys_do_not()
        {
            var toggle = new ToggleSwitch();

            toggle.OnKeyPressed(KeyboardCommand.Enter);
            Assert.IsTrue(toggle.IsOn);

            toggle.OnKeyPressed(' ');
            Assert.IsFalse(toggle.IsOn);

            toggle.OnKeyPressed(KeyboardCommand.Enter, KeyboardModifiers.Control);
            toggle.OnKeyPressed('x');
            Assert.IsFalse(toggle.IsOn);
        }

        [TestMethod]
        public void Toggle_switch_mouse_click_toggles()
        {
            var toggle = new ToggleSwitch();
            toggle.UpdateLayout(new Rect(0, 0, 96, 52));

            toggle.OnMouseDown(new MouseEventArgs(new PointF(10, 10), MouseButton.Left));
            toggle.OnMouseUp(new MouseEventArgs(new PointF(10, 10), MouseButton.Left));

            Assert.IsTrue(toggle.IsOn);
            Assert.AreSame(toggle, ScreenEngine.Instance!.FocusedControl);
        }

        [TestMethod]
        public void Toggle_switch_measures_fixed_size_plus_margin_within_constraints()
        {
            var toggle = new ToggleSwitch { Margin = new Thickness(4, 2) };
            var size = toggle.MeasureLayout();
            Assert.AreEqual(104, size.Width, 0.001f);
            Assert.AreEqual(56, size.Height, 0.001f);

            var auto = new ToggleSwitch { Width = float.NaN, Height = float.NaN };
            var autoSize = auto.MeasureLayout();
            Assert.AreEqual(96, autoSize.Width, 0.001f);
            Assert.AreEqual(52, autoSize.Height, 0.001f);

            var constrained = new ToggleSwitch { MaxWidth = 60, Margin = new Thickness(1) };
            Assert.AreEqual(62, constrained.MeasureLayout().Width, 0.001f);
        }

        [TestMethod]
        public void Toggle_switch_gone_measures_empty()
        {
            var toggle = new ToggleSwitch { IsGone = true };

            var size = toggle.MeasureLayout();

            Assert.AreEqual(0, size.Width);
            Assert.AreEqual(0, size.Height);
        }

        [TestMethod]
        public void Toggle_switch_theme_switch_reseeds_untouched_brushes_and_keeps_overrides()
        {
            var themeA = PortableTheme.CreateDefault();
            themeA.ToggleSwitchOffTrackBrush = new SolidColorBrush(Color.Gray);
            themeA.ToggleSwitchOnTrackBrush = new SolidColorBrush(Color.Blue);
            themeA.ToggleSwitchKnobBrush = new SolidColorBrush(Color.White);
            var themeB = PortableTheme.CreateDefault();
            themeB.ToggleSwitchOffTrackBrush = new SolidColorBrush(Color.DarkGray);
            themeB.ToggleSwitchOnTrackBrush = new SolidColorBrush(Color.Green);
            themeB.ToggleSwitchKnobBrush = new SolidColorBrush(Color.Black);

            var island = new ThemeIsland { Theme = themeA };
            var toggle = new ToggleSwitch();
            island.Content = toggle;
            toggle.RefreshThemeResources();
            var customKnob = new SolidColorBrush(Color.Pink);
            toggle.KnobBrush = customKnob;

            Assert.AreSame(themeA.ToggleSwitchOnTrackBrush, toggle.OnTrackBrush);

            island.Theme = themeB;
            toggle.RefreshThemeResources();

            Assert.AreSame(themeB.ToggleSwitchOffTrackBrush, toggle.OffTrackBrush);
            Assert.AreSame(themeB.ToggleSwitchOnTrackBrush, toggle.OnTrackBrush);
            Assert.AreSame(customKnob, toggle.KnobBrush);
        }
    }

    [TestClass]
    public class ToggleButtonCoverageTests
    {
        [TestMethod]
        public void Toggle_button_second_click_unchecks_and_raises_false()
        {
            var button = new ToggleButton { Text = "Toggle" };
            var events = new List<bool>();
            button.Checked += (sender, args) => events.Add(args.IsChecked);

            button.OnClick();
            button.OnClick();

            Assert.IsFalse(button.IsChecked);
            CollectionAssert.AreEqual(new[] { true, false }, events);
        }

        [TestMethod]
        public void Toggle_button_setting_same_value_does_not_raise_checked()
        {
            var button = new ToggleButton();
            var calls = 0;
            button.Checked += (sender, args) => calls++;

            button.IsChecked = false;
            button.IsChecked = true;
            button.IsChecked = true;

            Assert.AreEqual(1, calls);
        }

        [TestMethod]
        public void Toggle_button_swaps_background_to_toggle_brush_and_restores_it()
        {
            var original = new SolidColorBrush(Color.DarkSlateGray);
            var toggleBrush = new SolidColorBrush(Color.Gold);
            var button = new ToggleButton { BackgroundBrush = original, ToggleBrush = toggleBrush };

            button.IsChecked = true;
            Assert.AreSame(toggleBrush, button.BackgroundBrush);

            button.IsChecked = false;
            Assert.AreSame(original, button.BackgroundBrush);
        }

        [TestMethod]
        public void Toggle_button_changing_toggle_brush_while_checked_applies_immediately()
        {
            var button = new ToggleButton { IsChecked = true };
            var replacement = new SolidColorBrush(Color.Magenta);

            button.ToggleBrush = replacement;

            Assert.AreSame(replacement, button.BackgroundBrush);
        }

        [TestMethod]
        public void Toggle_button_checked_text_uses_toggle_text_color()
        {
            var button = new ToggleButton { Text = "Bold", ToggleTextColor = Color.Lime };

            button.IsChecked = true;

            Assert.AreEqual(Color.Lime, ((TextBlock)button.Content!).TextColor);
        }

        [TestMethod]
        public void Toggle_button_reports_checked_visual_state_only_while_enabled()
        {
            var button = new InspectableToggleButton();
            Assert.AreNotEqual(ControlVisualState.Checked, button.VisualState);

            button.IsChecked = true;
            Assert.AreEqual(ControlVisualState.Checked, button.VisualState);

            button.IsEnabled = false;
            Assert.AreEqual(ControlVisualState.Disabled, button.VisualState);
        }

        [TestMethod]
        public void Toggle_button_enter_key_toggles()
        {
            var button = new ToggleButton();

            button.OnKeyPressed(KeyboardCommand.Enter);

            Assert.IsTrue(button.IsChecked);
        }

        private sealed class InspectableToggleButton : ToggleButton
        {
            public ControlVisualState VisualState => GetVisualState();
        }
    }

    [TestClass]
    public class SwipePresenterCoverageTests
    {
        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        [TestMethod]
        public void Swipe_presenter_set_content_replaces_children_without_animation()
        {
            var presenter = new SwipePresenter();
            var first = new Border { Translation = new Vector2(30, 0) };
            var second = new Border();

            presenter.SetContent(first);
            Assert.AreSame(first, presenter.Content);
            Assert.AreEqual(1, presenter.Children.Count);
            Assert.AreEqual(Vector2.Zero, first.Translation);

            presenter.SetContent(second);
            Assert.AreSame(second, presenter.Content);
            Assert.AreEqual(1, presenter.Children.Count);
            Assert.IsFalse(presenter.Children.Contains(first));

            presenter.SetContent(null);
            Assert.IsNull(presenter.Content);
            Assert.AreEqual(0, presenter.Children.Count);
        }

        [TestMethod]
        public void Swipe_presenter_first_swipe_behaves_like_set_content()
        {
            var presenter = new SwipePresenter();
            var content = new Border();

            presenter.Swipe(content, SwipeDirection.Left);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));

            Assert.AreSame(content, presenter.Content);
            Assert.AreEqual(1, presenter.Children.Count);
            Assert.AreEqual(Vector2.Zero, content.Translation);
        }

        [TestMethod]
        public void Swipe_presenter_left_swipe_slides_new_content_in_from_the_right()
        {
            var presenter = CreatePresenter(out var first);
            var next = new Border();

            presenter.Swipe(next, SwipeDirection.Left);
            Assert.AreSame(next, presenter.Content);
            Assert.AreEqual(2, presenter.Children.Count);

            // The slide starts on the next layout pass, once the width is known.
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));
            Assert.AreEqual(200, next.Translation.X, 0.001f);
            Assert.AreEqual(0, first.Translation.X, 0.001f);

            ScreenSystem.TotalTime = presenter.Duration;
            next.UpdateTimers();
            first.UpdateTimers();

            Assert.AreEqual(0, next.Translation.X, 0.001f);
            Assert.AreEqual(-200, first.Translation.X, 0.001f);
            Assert.AreEqual(1, presenter.Children.Count);
            Assert.IsFalse(presenter.Children.Contains(first));
            Assert.AreSame(next, presenter.Content);
        }

        [TestMethod]
        public void Swipe_presenter_right_swipe_mirrors_the_slide()
        {
            var presenter = CreatePresenter(out var first);
            var previous = new Border();

            presenter.Swipe(previous, SwipeDirection.Right);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));

            Assert.AreEqual(-200, previous.Translation.X, 0.001f);

            ScreenSystem.TotalTime = presenter.Duration;
            previous.UpdateTimers();
            first.UpdateTimers();

            Assert.AreEqual(0, previous.Translation.X, 0.001f);
            Assert.AreEqual(200, first.Translation.X, 0.001f);
            Assert.AreEqual(1, presenter.Children.Count);
        }

        [TestMethod]
        public void Swipe_presenter_slide_distance_uses_content_width_inside_padding_and_margin()
        {
            var presenter = new SwipePresenter { Margin = new Thickness(10), Padding = new Thickness(5) };
            presenter.SetContent(new Border());
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));
            var next = new Border();

            presenter.Swipe(next, SwipeDirection.Left);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));

            Assert.AreEqual(170, next.Translation.X, 0.001f);
        }

        [TestMethod]
        public void Swipe_presenter_new_swipe_mid_flight_snaps_previous_swap()
        {
            var presenter = CreatePresenter(out var first);
            var second = new Border();
            var third = new Border();

            presenter.Swipe(second, SwipeDirection.Left);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50);
            second.UpdateTimers();
            first.UpdateTimers();

            presenter.Swipe(third, SwipeDirection.Left);

            // The first outgoing control is dropped immediately and the interrupted one is snapped home.
            Assert.IsFalse(presenter.Children.Contains(first));
            Assert.AreEqual(Vector2.Zero, second.Translation);
            Assert.AreEqual(2, presenter.Children.Count);
            Assert.AreSame(third, presenter.Content);

            presenter.UpdateLayout(new Rect(0, 0, 200, 100));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50) + presenter.Duration;
            second.UpdateTimers();
            third.UpdateTimers();
            // The cancelled animation of `second` must not fight the new outgoing slide.
            first.UpdateTimers();

            Assert.AreEqual(1, presenter.Children.Count);
            Assert.AreSame(third, presenter.Children[0]);
            Assert.AreEqual(0, third.Translation.X, 0.001f);
        }

        [TestMethod]
        public void Swipe_presenter_set_content_mid_flight_drops_outgoing_content()
        {
            var presenter = CreatePresenter(out var first);
            var second = new Border();
            var replacement = new Border();

            presenter.Swipe(second, SwipeDirection.Left);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));

            presenter.SetContent(replacement);

            Assert.AreEqual(1, presenter.Children.Count);
            Assert.AreSame(replacement, presenter.Content);
        }

        [TestMethod]
        public void Swipe_presenter_auto_size_is_largest_child_plus_padding_and_margin()
        {
            var presenter = new SwipePresenter
            {
                Width = float.NaN,
                Height = float.NaN,
                Padding = new Thickness(4),
                Margin = new Thickness(1)
            };
            presenter.SetContent(new Border { Width = 80, Height = 30 });

            var size = presenter.MeasureLayout();

            Assert.AreEqual(80 + 8 + 2, size.Width, 0.001f);
            Assert.AreEqual(30 + 8 + 2, size.Height, 0.001f);
        }

        [TestMethod]
        public void Swipe_presenter_fixed_size_ignores_children()
        {
            var presenter = new SwipePresenter { Width = 50, Height = 40 };
            presenter.SetContent(new Border { Width = 300, Height = 300 });

            var size = presenter.MeasureLayout();

            Assert.AreEqual(50, size.Width, 0.001f);
            Assert.AreEqual(40, size.Height, 0.001f);
        }

        [TestMethod]
        public void Swipe_presenter_clips_descendants()
        {
            Assert.IsTrue(new InspectableSwipePresenter().Clips);
        }

        private static SwipePresenter CreatePresenter(out Border first)
        {
            var presenter = new SwipePresenter { Easing = Easings.Linear };
            first = new Border();
            presenter.SetContent(first);
            presenter.UpdateLayout(new Rect(0, 0, 200, 100));
            return presenter;
        }

        private sealed class InspectableSwipePresenter : SwipePresenter
        {
            public bool Clips => ClipsDescendants;
        }
    }

    [TestClass]
    public class ImageCoverageTests
    {
        [TestMethod]
        public void Image_defaults_to_uniform_and_is_not_focusable()
        {
            var image = new Image();

            Assert.AreEqual(Stretch.Uniform, image.Stretch);
            Assert.IsFalse(image.IsFocusable);
            Assert.IsNull(image.Source);
        }

        [TestMethod]
        public void Image_without_source_measures_fixed_size_plus_margin()
        {
            var image = new Image { Width = 40, Height = 30, Margin = new Thickness(5) };

            var size = image.MeasureLayout();

            Assert.AreEqual(50, size.Width, 0.001f);
            Assert.AreEqual(40, size.Height, 0.001f);
        }

        [TestMethod]
        public void Image_without_source_and_auto_size_measures_only_margin_and_minimums()
        {
            var image = new Image { Width = float.NaN, Height = float.NaN, MinWidth = 12, Margin = new Thickness(2) };

            var size = image.MeasureLayout();

            Assert.AreEqual(16, size.Width, 0.001f);
            Assert.AreEqual(4, size.Height, 0.001f);
        }

        [TestMethod]
        public void Image_gone_measures_empty()
        {
            var image = new Image { Width = 40, Height = 30, IsGone = true };

            var size = image.MeasureLayout();

            Assert.AreEqual(0, size.Width);
            Assert.AreEqual(0, size.Height);
        }

        [TestMethod]
        public void Image_button_forwards_image_properties_to_inner_image()
        {
            var button = new ImageButton { Stretch = Stretch.Fill, TintColor = Color.Red };

            var image = button.Content as Image;

            Assert.IsNotNull(image);
            Assert.AreEqual(Stretch.Fill, image.Stretch);
            Assert.AreEqual(Color.Red, image.TintColor);
            Assert.IsNull(button.Source);

            image.Stretch = Stretch.None;
            Assert.AreEqual(Stretch.None, button.Stretch);
        }

        [TestMethod]
        public void Image_button_is_clickable_and_focusable()
        {
            var button = new ImageButton();
            var clicks = 0;
            button.Click += (sender, args) => clicks++;

            button.OnKeyPressed(KeyboardCommand.Enter);
            button.OnClick();

            Assert.AreEqual(2, clicks);
            Assert.IsTrue(button.IsFocusable);
        }
    }

    [TestClass]
    public class ProgressIndicatorCoverageTests
    {
        [TestMethod]
        public void Progress_indicator_measure_defaults_width_to_max_size()
        {
            var indicator = new ProgressIndicator { MaxSize = 18, Height = 40 };

            var size = indicator.MeasureLayout();

            Assert.AreEqual(18, indicator.Width, 0.001f);
            Assert.AreEqual(18, size.Width, 0.001f);
            Assert.AreEqual(40, size.Height, 0.001f);
        }

        [TestMethod]
        public void Progress_indicator_keeps_explicit_width()
        {
            var indicator = new ProgressIndicator { Width = 50, Height = 40 };

            var size = indicator.MeasureLayout();

            Assert.AreEqual(50, size.Width, 0.001f);
        }

        [TestMethod]
        public void Progress_indicator_defaults_and_is_not_focusable()
        {
            var indicator = new ProgressIndicator();

            Assert.AreEqual(6, indicator.MinSize);
            Assert.AreEqual(12, indicator.MaxSize);
            Assert.IsFalse(indicator.IsFocusable);
        }

        [TestMethod]
        public void Progress_indicator_theme_switch_reseeds_untouched_values_and_keeps_overrides()
        {
            var themeA = PortableTheme.CreateDefault();
            themeA.ProgressIndicatorForeground = Color.Red;
            themeA.ProgressIndicatorHeight = 30;
            var themeB = PortableTheme.CreateDefault();
            themeB.ProgressIndicatorForeground = Color.Blue;
            themeB.ProgressIndicatorHeight = 44;

            var island = new ThemeIsland { Theme = themeA };
            var untouched = new ProgressIndicator();
            var overridden = new ProgressIndicator();
            var panel = new StackPanel();
            panel.AddChild(untouched);
            panel.AddChild(overridden);
            island.Content = panel;
            untouched.RefreshThemeResources();
            overridden.RefreshThemeResources();
            overridden.Foreground = Color.Orange;
            overridden.Height = 99;

            island.Theme = themeB;
            untouched.RefreshThemeResources();
            overridden.RefreshThemeResources();

            Assert.AreEqual(Color.Blue, untouched.Foreground);
            Assert.AreEqual(44, untouched.Height);
            Assert.AreEqual(Color.Orange, overridden.Foreground);
            Assert.AreEqual(99, overridden.Height);
        }
    }

    [TestClass]
    public class FlyOutCoverageTests
    {
        [TestInitialize]
        public void ResetState()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            if (ScreenEngine.Instance != null) ScreenEngine.Instance.FocusedControl = null;
        }

        [TestMethod]
        public void Flyout_is_a_clipping_non_focusable_container()
        {
            var flyOut = new InspectableFlyOut();

            Assert.IsFalse(flyOut.IsFocusable);
            Assert.IsTrue(flyOut.Clips);
        }

        [TestMethod]
        public void Flyout_lifecycle_events_fire_once_in_order()
        {
            var flyOut = new FlyOut(new PointF(0, 0), false);
            var log = new List<string>();
            flyOut.Showing += (_, _) => log.Add("showing");
            flyOut.Shown += (_, _) => log.Add("shown");
            flyOut.Dismissing += (_, _) => log.Add("dismissing");
            flyOut.Dismissed += (_, _) => log.Add("dismissed");

            // Dismiss before open is a no-op.
            flyOut.NotifyDismissing();
            flyOut.NotifyDismissed();

            flyOut.NotifyShowing();
            flyOut.NotifyShown();
            // Re-showing an open flyout is a no-op.
            flyOut.NotifyShowing();
            flyOut.NotifyShown();

            flyOut.NotifyDismissing();
            flyOut.NotifyDismissed();
            // Re-dismissing a closed flyout is a no-op.
            flyOut.NotifyDismissing();
            flyOut.NotifyDismissed();

            CollectionAssert.AreEqual(new[] { "showing", "shown", "dismissing", "dismissed" }, log);
        }

        [TestMethod]
        public void Flyout_mouse_down_outside_content_closes_and_dismissed_fires_after_animation()
        {
            var screen = new TestScreen();
            var content = new Border { Width = 50, Height = 20 };
            screen.ShowFlyOut(new PointF(20, 30), content, removeOnRelease: false, placement: FlyOutPlacement.Below);
            var flyOut = (FlyOut)content.Parent!;
            var dismissing = 0;
            var dismissed = 0;
            flyOut.Dismissing += (_, _) => dismissing++;
            flyOut.Dismissed += (_, _) => dismissed++;
            Assert.IsTrue(screen.IsFlyOutOpen);

            flyOut.OnMouseDown(new MouseEventArgs(new PointF(150, 150), MouseButton.Left));

            Assert.IsFalse(screen.IsFlyOutOpen);
            Assert.AreEqual(1, dismissing);
            Assert.AreEqual(0, dismissed);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(500);
            content.UpdateTimers();

            Assert.AreEqual(1, dismissed);
            Assert.IsNull(flyOut.Content);
            Assert.IsNull(flyOut.Parent);
        }

        [TestMethod]
        public void Flyout_remove_on_release_ignores_mouse_down_and_closes_on_mouse_up()
        {
            var screen = new TestScreen();
            var content = new Border { Width = 50, Height = 20 };
            screen.ShowFlyOut(new PointF(20, 30), content, removeOnRelease: true);
            var flyOut = (FlyOut)content.Parent!;

            flyOut.OnMouseDown(new MouseEventArgs(new PointF(150, 150), MouseButton.Left));
            Assert.IsTrue(screen.IsFlyOutOpen);

            flyOut.OnMouseUp(new MouseEventArgs(new PointF(150, 150), MouseButton.Left));
            Assert.IsFalse(screen.IsFlyOutOpen);
        }

        [TestMethod]
        public void Flyout_touch_down_closes_it()
        {
            var screen = new TestScreen();
            var content = new Border { Width = 50, Height = 20 };
            screen.ShowFlyOut(new PointF(20, 30), content, removeOnRelease: false);
            var flyOut = (FlyOut)content.Parent!;

            flyOut.OnTouchDown(new TouchEventArgs(new PointF(150, 150)));

            Assert.IsFalse(screen.IsFlyOutOpen);
        }

        [TestMethod]
        public void Flyout_replaced_by_another_dismisses_the_first_immediately()
        {
            var screen = new TestScreen();
            var firstContent = new Border { Width = 50, Height = 20 };
            screen.ShowFlyOut(new PointF(20, 30), firstContent, removeOnRelease: false);
            var first = (FlyOut)firstContent.Parent!;
            var dismissed = 0;
            first.Dismissed += (_, _) => dismissed++;

            screen.ShowFlyOut(new PointF(20, 30), new Border { Width = 10, Height = 10 }, removeOnRelease: false);

            Assert.AreEqual(1, dismissed);
            Assert.IsTrue(screen.IsFlyOutOpen);
            Assert.IsNull(first.Content);
        }

        [TestMethod]
        public void Flyout_dispose_detaches_content()
        {
            var content = new Border();
            var flyOut = new FlyOut(new PointF(0, 0), false) { Content = content };

            flyOut.Dispose();

            Assert.IsNull(flyOut.Content);
        }

        private sealed class InspectableFlyOut : FlyOut
        {
            public InspectableFlyOut() : base(new PointF(0, 0), false)
            {
            }

            public bool Clips => ClipsDescendants;
        }

        private sealed class TestScreen : Screen
        {
        }
    }

    [TestClass]
    public class ToolTipPopupCoverageTests
    {
        [TestMethod]
        public void Tooltip_popup_wraps_text_in_a_single_text_block()
        {
            var popup = new ToolTipPopup("Save the file");

            var text = popup.Content as TextBlock;

            Assert.IsNotNull(text);
            Assert.AreEqual("Save the file", text.Text);
            Assert.AreEqual(0, text.Margin.Left);
            Assert.IsFalse(popup.IsFocusable);
        }

        [TestMethod]
        public void Tooltip_popup_measure_includes_padding_and_border_box()
        {
            var popup = new ToolTipPopup(string.Empty) { Padding = new Thickness(8, 5, 8, 6), Margin = new Thickness(0) };

            var size = popup.MeasureLayout();
            var content = ((TextBlock)popup.Content!).MeasureLayout();

            Assert.AreEqual(content.Width + 16, size.Width, 0.001f);
            Assert.AreEqual(content.Height + 11, size.Height, 0.001f);
        }

        [TestMethod]
        public void Screen_tooltip_popup_is_positioned_at_anchor_plus_offset_and_cleared()
        {
            var screen = new TestScreen();
            var owner = new Button { Text = "Run", Width = 80, Height = 30 };
            screen.Content = owner;

            screen.ShowToolTip(owner, "Runs the job", new PointF(40, 50));

            Assert.IsTrue(screen.IsToolTipVisibleFor(owner));
            var offset = new ScreenEngineOptions().ToolTipOffset;
            Assert.AreEqual(40 + offset.X, screen.ToolTipRect.Left, 0.001f);
            Assert.AreEqual(50 + offset.Y, screen.ToolTipRect.Top, 0.001f);

            screen.UpdateToolTip(owner, new PointF(60, 70));
            Assert.AreEqual(60 + offset.X, screen.ToolTipRect.Left, 0.001f);

            screen.ClearToolTip(owner);
            Assert.IsFalse(screen.IsToolTipVisibleFor(owner));
            Assert.AreEqual(Rect.Empty, screen.ToolTipRect);
        }

        [TestMethod]
        public void Screen_tooltip_is_not_shown_for_empty_text_or_disabled_owner()
        {
            var screen = new TestScreen();
            var owner = new Button { Text = "Run" };
            screen.Content = owner;

            screen.ShowToolTip(owner, string.Empty, new PointF(0, 0));
            Assert.IsFalse(screen.IsToolTipVisibleFor(owner));

            owner.IsEnabled = false;
            screen.ShowToolTip(owner, "Disabled", new PointF(0, 0));
            Assert.IsFalse(screen.IsToolTipVisibleFor(owner));
        }

        [TestMethod]
        public void Screen_clear_tooltip_for_other_owner_keeps_current_tooltip()
        {
            var screen = new TestScreen();
            var owner = new Button { Text = "Run" };
            var other = new Button { Text = "Other" };
            var panel = new StackPanel();
            panel.AddChild(owner);
            panel.AddChild(other);
            screen.Content = panel;

            screen.ShowToolTip(owner, "Runs", new PointF(0, 0));
            screen.ClearToolTip(other);

            Assert.IsTrue(screen.IsToolTipVisibleFor(owner));
        }

        private sealed class TestScreen : Screen
        {
        }
    }

    [TestClass]
    public class BrushCoverageTests
    {
        [TestMethod]
        public void Image_brush_defaults()
        {
            var brush = new ImageBrush();

            Assert.IsNull(brush.Source);
            Assert.AreEqual(Stretch.Fill, brush.Stretch);
            Assert.AreEqual(ImageBrushTileMode.None, brush.TileMode);
            Assert.AreEqual(Color.White, brush.TintColor);
            Assert.IsNull(brush.SourceRect);
        }

        [TestMethod]
        public void Image_brush_without_source_draws_nothing_and_needs_no_device()
        {
            var brush = new ImageBrush();

            // Returns before touching the SpriteBatch, so a null batch must not throw.
            brush.Draw(null!, new BrushContext(new Rect(0, 0, 10, 10), 0, 1, null!));
        }

        [TestMethod]
        public void Image_brush_fill_stretch_covers_target_exactly()
        {
            var brush = new ImageBrush { Stretch = Stretch.Fill };

            var rect = brush.GetStretchedRect(new Rect(10, 20, 200, 100), 64, 16);

            Assert.AreEqual(10, rect.Left, 0.001f);
            Assert.AreEqual(20, rect.Top, 0.001f);
            Assert.AreEqual(200, rect.Width, 0.001f);
            Assert.AreEqual(100, rect.Height, 0.001f);
        }

        [TestMethod]
        public void Image_brush_stretch_with_empty_source_is_empty()
        {
            var brush = new ImageBrush { Stretch = Stretch.Uniform };

            Assert.AreEqual(Rect.Empty, brush.GetStretchedRect(new Rect(0, 0, 100, 100), 0, 10));
            Assert.AreEqual(Rect.Empty, brush.GetStretchedRect(new Rect(0, 0, 100, 100), 10, 0));
        }

        [TestMethod]
        public void Image_brush_uniform_to_fill_crops_source_centered_to_target_aspect()
        {
            // 200x100 target over a 100x100 source: keep full width, crop to the middle 50 rows.
            var wide = ImageBrush.GetUniformToFillSource(new Rect(0, 0, 200, 100), new Rectangle(0, 0, 100, 100));
            Assert.AreEqual(new Rectangle(0, 25, 100, 50), wide);

            // 50x100 target: keep full height, crop to the middle 50 columns.
            var tall = ImageBrush.GetUniformToFillSource(new Rect(0, 0, 50, 100), new Rectangle(0, 0, 100, 100));
            Assert.AreEqual(new Rectangle(25, 0, 50, 100), tall);

            // Source sub-rectangles stay inside their own bounds.
            var offset = ImageBrush.GetUniformToFillSource(new Rect(0, 0, 200, 100), new Rectangle(10, 10, 100, 100));
            Assert.AreEqual(new Rectangle(10, 35, 100, 50), offset);
        }

        [TestMethod]
        public void Image_brush_uniform_to_fill_with_degenerate_input_returns_source()
        {
            var source = new Rectangle(0, 0, 100, 100);

            Assert.AreEqual(source, ImageBrush.GetUniformToFillSource(new Rect(0, 0, 0, 100), source));
            Assert.AreEqual(Rectangle.Empty, ImageBrush.GetUniformToFillSource(new Rect(0, 0, 10, 10), Rectangle.Empty));
        }

        [TestMethod]
        public void Radial_gradient_brush_two_color_constructor_creates_center_and_edge_stops()
        {
            var brush = new RadialGradientBrush(Color.White, Color.Black);

            Assert.AreEqual(2, brush.Stops.Count);
            Assert.AreEqual(0, brush.Stops[0].Offset, 0.0001f);
            Assert.AreEqual(Color.White, brush.Stops[0].Color);
            Assert.AreEqual(1, brush.Stops[1].Offset, 0.0001f);
            Assert.AreEqual(Color.Black, brush.Stops[1].Color);
            Assert.AreEqual(0.5f, brush.Center.X, 0.0001f);
            Assert.AreEqual(0.5f, brush.Center.Y, 0.0001f);
            Assert.AreEqual(0.5f, brush.RadiusX, 0.0001f);
            Assert.AreEqual(0.5f, brush.RadiusY, 0.0001f);
        }

        [TestMethod]
        public void Radial_gradient_brush_null_stops_yield_empty_list()
        {
            var brush = new RadialGradientBrush((GradientStop[])null!);

            Assert.IsNotNull(brush.Stops);
            Assert.AreEqual(0, brush.Stops.Count);
        }

        [TestMethod]
        public void Radial_gradient_brush_cache_key_is_stable_and_tracks_radius_and_stops()
        {
            var brush = new RadialGradientBrush(Color.White, Color.Black);
            var equivalent = new RadialGradientBrush(Color.White, Color.Black);
            var differentRadius = new RadialGradientBrush(Color.White, Color.Black) { RadiusY = 0.8f };
            var differentStops = new RadialGradientBrush(Color.White, Color.Red);

            Assert.AreEqual(brush.CreateTextureCacheKey(), equivalent.CreateTextureCacheKey());
            Assert.AreNotEqual(brush.CreateTextureCacheKey(), differentRadius.CreateTextureCacheKey());
            Assert.AreNotEqual(brush.CreateTextureCacheKey(), differentStops.CreateTextureCacheKey());
        }

        [TestMethod]
        public void Radial_gradient_brush_cache_key_follows_stop_mutation()
        {
            var brush = new RadialGradientBrush(Color.White, Color.Black);
            var before = brush.CreateTextureCacheKey();

            brush.Stops.Add(new GradientStop(0.5f, Color.Red));

            Assert.AreNotEqual(before, brush.CreateTextureCacheKey());
        }

        [TestMethod]
        public void Radial_gradient_brush_skips_empty_rect_without_device()
        {
            var brush = new RadialGradientBrush(Color.White, Color.Black);

            brush.Draw(null!, new BrushContext(new Rect(0, 0, 0, 10), 0, 1, null!));
            brush.Draw(null!, new BrushContext(new Rect(0, 0, 10, -1), 0, 1, null!));
        }
    }

    [TestClass]
    public class BackdropManagerCoverageTests
    {
        [TestMethod]
        public void Backdrop_manager_requires_a_graphics_device()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new BackdropManager(null!));
        }
    }
}

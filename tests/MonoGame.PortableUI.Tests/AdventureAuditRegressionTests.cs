using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Regression tests for the fixes from the MonoGame.Adventure audit backlog
    /// (monogame.portableui.todo.md in the sibling repo).</summary>
    [TestClass]
    public class AdventureAuditRegressionTests
    {
        [TestMethod]
        public void StackPanel_center_aligned_child_on_stacking_axis_gets_finite_offset()
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var child = new Border
            {
                Width = 40,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.AddChild(child);

            panel.UpdateLayout(new Rect(0, 0, 200, 200));

            Assert.IsFalse(float.IsNaN(child.BoundingRect.Top), "Top must not be NaN");
            Assert.IsFalse(float.IsInfinity(child.BoundingRect.Top), "Top must be finite");
            Assert.AreEqual(30, child.BoundingRect.Height);
        }

        [TestMethod]
        public void StackPanel_bottom_aligned_child_on_stacking_axis_gets_finite_offset()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var child = new Border
            {
                Width = 40,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            panel.AddChild(child);

            panel.UpdateLayout(new Rect(0, 0, 200, 200));

            Assert.IsFalse(float.IsNaN(child.BoundingRect.Left));
            Assert.IsFalse(float.IsInfinity(child.BoundingRect.Left));
        }

        [TestMethod]
        public void ControlStyle_resolved_cache_invalidates_when_state_style_is_mutated()
        {
            var style = new ControlStyle();
            style.Hover.TextColor = Color.Red;

            var before = style.GetResolved(ControlVisualState.Hover);
            Assert.AreEqual(Color.Red, before.TextColor);

            style.Hover.TextColor = Color.Blue;

            var after = style.GetResolved(ControlVisualState.Hover);
            Assert.AreEqual(Color.Blue, after.TextColor);
        }

        [TestMethod]
        public void ControlStyle_resolved_cache_invalidates_when_state_slot_is_replaced()
        {
            var style = new ControlStyle();
            style.Normal.Background = new SolidColorBrush(Color.Black);

            var before = style.GetResolved(ControlVisualState.Hover);
            Assert.IsNotNull(before.Background);

            var replacement = new StateStyle { Background = new SolidColorBrush(Color.White) };
            style.Hover = replacement;

            var after = style.GetResolved(ControlVisualState.Hover);
            Assert.AreEqual(replacement.Background, after.Background);
        }

        [TestMethod]
        public void ControlStyle_resolved_cache_is_stable_without_mutation()
        {
            var style = new ControlStyle();
            style.Normal.TextColor = Color.White;

            var first = style.GetResolved(ControlVisualState.Pressed);
            var second = style.GetResolved(ControlVisualState.Pressed);

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void Tab_traversal_cycles_interactive_controls_and_skips_labels()
        {
            using var game = new Microsoft.Xna.Framework.Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(320, 240);
            var screen = new TestScreen();
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var first = new Button { Text = "first", Height = 24 };
            var label = new TextBlock { Text = "label" };
            var second = new Button { Text = "second", Height = 24 };
            panel.AddChild(first);
            panel.AddChild(label);
            panel.AddChild(second);
            screen.Content = panel;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            screen.FocusNextTabStop();
            Assert.IsTrue(first.IsFocused, "first stop gets focus from empty");

            screen.FocusNextTabStop();
            Assert.IsTrue(second.IsFocused, "label is skipped");

            screen.FocusNextTabStop();
            Assert.IsTrue(first.IsFocused, "wraps to the start");

            screen.FocusNextTabStop(backwards: true);
            Assert.IsTrue(second.IsFocused, "shift-tab wraps to the end");
        }

        [TestMethod]
        public void Tab_traversal_honors_explicit_tab_index_and_opt_out()
        {
            using var game = new Microsoft.Xna.Framework.Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(320, 240);
            var screen = new TestScreen();
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var documentOrder = new Button { Text = "doc", Height = 24 };
            var optedOut = new Button { Text = "skip me", Height = 24, IsTabStop = false };
            var explicitFirst = new Button { Text = "explicit", Height = 24, TabIndex = 0 };
            panel.AddChild(documentOrder);
            panel.AddChild(optedOut);
            panel.AddChild(explicitFirst);
            screen.Content = panel;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            screen.FocusNextTabStop();
            Assert.IsTrue(explicitFirst.IsFocused, "explicit TabIndex comes before document order");

            screen.FocusNextTabStop();
            Assert.IsTrue(documentOrder.IsFocused, "opted-out button is skipped");
        }

        [TestMethod]
        public void TextBlock_wrap_with_fixed_width_measures_multiple_lines()
        {
            var noWrap = new TextBlock { Text = "the quick brown fox jumps over the lazy dog" };
            var wrapped = new TextBlock
            {
                Text = "the quick brown fox jumps over the lazy dog",
                Width = 80,
                TextWrapping = TextWrapping.Wrap
            };

            var singleLine = noWrap.MeasureLayout();
            var multiLine = wrapped.MeasureLayout();

            Assert.IsTrue(multiLine.Height >= singleLine.Height * 2,
                $"expected at least two lines: wrapped {multiLine.Height} vs single {singleLine.Height}");
            Assert.AreEqual(80, multiLine.Width);
        }

        [TestMethod]
        public void TextBlock_wrap_respects_explicit_newlines()
        {
            var wrapped = new TextBlock
            {
                Text = "one\ntwo\nthree",
                Width = 400,
                TextWrapping = TextWrapping.Wrap
            };
            var single = new TextBlock { Text = "one", Width = 400, TextWrapping = TextWrapping.Wrap };

            var threeLines = wrapped.MeasureLayout();
            var oneLine = single.MeasureLayout();

            Assert.IsTrue(threeLines.Height >= oneLine.Height * 3 - 0.01f,
                $"expected three lines: {threeLines.Height} vs {oneLine.Height}");
        }

        [TestMethod]
        public void TextBlock_nowrap_measure_is_unchanged_by_trimming_setting()
        {
            var plain = new TextBlock { Text = "some label" };
            var trimming = new TextBlock { Text = "some label", TextTrimming = TextTrimming.Ellipsis };

            Assert.AreEqual(plain.MeasureLayout().Width, trimming.MeasureLayout().Width);
            Assert.AreEqual(plain.MeasureLayout().Height, trimming.MeasureLayout().Height);
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

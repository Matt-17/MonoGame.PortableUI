using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class InputRegressionTests
    {
        [TestMethod]
        public void Double_click_event_uses_configured_time_window()
        {
            var button = new Button { Text = "Double" };
            var doubleClicks = 0;
            button.DoubleClick += (sender, args) => doubleClicks++;

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            button.OnClick();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(250);
            button.OnClick();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(1000);
            button.OnClick();

            Assert.AreEqual(1, doubleClicks);
        }

        [TestMethod]
        public void Timer_elapsed_is_driven_by_screen_time()
        {
            var timer = new Timer(300);
            var elapsed = 0;
            timer.Elapsed += (sender, args) => elapsed++;

            ScreenSystem.TotalTime = TimeSpan.Zero;
            timer.Start();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(299);
            timer.Update();
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(300);
            timer.Update();

            Assert.AreEqual(1, elapsed);
            Assert.IsFalse(timer.IsRunning);
        }

        [TestMethod]
        public void Visual_tree_flattening_preserves_descendants_first_order()
        {
            var root = new StackPanel();
            var inner = new StackPanel();
            var leaf = new Button { Text = "Leaf" };
            var sibling = new TextBlock { Text = "Sibling" };
            inner.AddChild(leaf);
            root.AddChild(inner);
            root.AddChild(sibling);

            var list = VisualTreeHelper.GetVisualTreeAsList(root, false).ToArray();

            Assert.AreSame(leaf.Content, list[0]);
            Assert.AreSame(leaf, list[1]);
            Assert.AreSame(inner, list[2]);
            Assert.AreSame(sibling, list[3]);
            Assert.AreSame(root, list[4]);
        }

        [TestMethod]
        public void Visual_tree_flattening_can_skip_gone_subtrees()
        {
            var root = new StackPanel();
            var inner = new StackPanel();
            var leaf = new Button { Text = "Leaf" };
            var sibling = new TextBlock { Text = "Sibling" };
            inner.AddChild(leaf);
            root.AddChild(inner);
            root.AddChild(sibling);
            inner.IsGone = true;

            var visibleList = VisualTreeHelper.GetVisualTreeAsList(root, false).ToArray();
            var fullList = VisualTreeHelper.GetVisualTreeAsList(root, true).ToArray();

            CollectionAssert.AreEqual(new Control[] { sibling, root }, visibleList);
            CollectionAssert.AreEqual(new[] { leaf.Content, leaf, inner, sibling, root }, fullList);
        }

        [TestMethod]
        public void Hiding_an_ancestor_drops_focus_before_text_reaches_the_control()
        {
            var screen = new TestScreen();
            var page = new StackPanel();
            var textBox = new TextBox();
            page.AddChild(textBox);
            ScreenEngine.Instance!.FocusedControl = textBox;

            page.IsGone = true;
            screen.HandleTextInput('x');

            Assert.AreEqual("", textBox.Text);
            Assert.IsNull(ScreenEngine.Instance!.FocusedControl);
        }

        [TestMethod]
        public void Detaching_a_subtree_drops_focus_held_inside_it()
        {
            var host = new Border();
            var page = new StackPanel();
            var textBox = new TextBox();
            page.AddChild(textBox);
            host.Content = page;
            ScreenEngine.Instance!.FocusedControl = textBox;

            host.Content = null;

            Assert.IsNull(ScreenEngine.Instance!.FocusedControl);
        }

        [TestMethod]
        public void Screen_text_input_routes_localized_characters_to_focused_control()
        {
            var screen = new TestScreen();
            var textBox = new TextBox();
            ScreenEngine.Instance!.FocusedControl = textBox;

            screen.HandleTextInput('ä');
            screen.HandleTextInput('\b');

            Assert.AreEqual("ä", textBox.Text);
        }

        [TestMethod]
        public void Screen_update_consumes_virtual_input_source_for_pointer_clicks()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(120, 80);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            var button = new Button
            {
                Width = 80,
                Height = 40,
                Text = "Run"
            };
            var clicks = 0;
            button.Click += (sender, args) => clicks++;
            screen.InputSource = source;
            screen.Content = button;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            source.SetPointer(new PointF(10, 10));
            screen.Update();
            source.SetPointer(new PointF(10, 10), leftDown: true);
            screen.Update();
            source.SetPointer(new PointF(10, 10));
            screen.Update();

            Assert.AreEqual(1, clicks);
        }

        [TestMethod]
        public void Open_flyout_is_relaid_out_when_the_screen_shrinks()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 200);
            var screen = new TestScreen();
            screen.InputSource = new VirtualInputSource();
            screen.Content = new Border();
            engine.NavigateToScreen(screen);
            screen.Update();
            var popup = new Border { Width = 50, Height = 30 };
            screen.ShowFlyOut(new PointF(140, 150), popup, removeOnRelease: false, placement: FlyOutPlacement.Below);

            engine.SetScreenSize(100, 100);
            screen.Update();

            Assert.IsTrue(popup.BoundingRect.Right <= 100 && popup.BoundingRect.Bottom <= 100,
                $"popup must be clamped into the new screen, was {popup.BoundingRect}");
        }

        [TestMethod]
        public void Hover_is_cleared_while_a_flyout_is_open_and_restored_after()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(200, 200);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            var button = new Button
            {
                Width = 80,
                Height = 40,
                Text = "Menu",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            screen.InputSource = source;
            screen.Content = button;
            engine.NavigateToScreen(screen);
            source.SetPointer(new PointF(150, 150));
            screen.Update();
            source.SetPointer(new PointF(10, 10));
            screen.Update();
            Assert.IsTrue(button.IsMouseHovering);

            screen.ShowFlyOut(new PointF(100, 150), new Border { Width = 50, Height = 30 }, removeOnRelease: false);
            Assert.IsFalse(button.IsMouseHovering, "main tree hover must end when a popup takes input");

            screen.ClearFlyOut();
            Assert.IsTrue(button.IsMouseHovering, "hover must resync for a stationary pointer");
        }

        [TestMethod]
        public void Click_on_overlapping_siblings_goes_to_the_topmost_one()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(120, 80);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            var below = new Button { Width = 80, Height = 40, Text = "Below" };
            var above = new Button { Width = 80, Height = 40, Text = "Above" };
            var belowClicks = 0;
            var aboveClicks = 0;
            below.Click += (sender, args) => belowClicks++;
            above.Click += (sender, args) => aboveClicks++;
            var grid = new Grid();
            grid.Children.Add(below);
            grid.Children.Add(above);
            screen.InputSource = source;
            screen.Content = grid;
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            source.SetPointer(new PointF(10, 10));
            screen.Update();
            source.SetPointer(new PointF(10, 10), leftDown: true);
            screen.Update();
            source.SetPointer(new PointF(10, 10));
            screen.Update();

            Assert.AreEqual(1, aboveClicks);
            Assert.AreEqual(0, belowClicks);
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

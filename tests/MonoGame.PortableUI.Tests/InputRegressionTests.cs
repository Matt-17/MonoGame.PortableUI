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
        public void Screen_text_input_routes_localized_characters_to_focused_control()
        {
            var screen = new TestScreen();
            var textBox = new TextBox();
            ScreenEngine.FocusedControl = textBox;

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

        private sealed class TestScreen : Screen
        {
        }
    }
}

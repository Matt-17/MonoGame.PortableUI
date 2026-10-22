using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class KeyboardScrollTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private static (ScreenEngine Engine, ScrollViewer Viewer, StackPanel Panel) Create(Game game, float contentHeight)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 800);
            var panel = new StackPanel();
            panel.Children.Add(new Border { Height = contentHeight });
            var viewer = new ScrollViewer { Content = panel, ShowScrollBars = false };
            engine.NavigateToScreen(new TestScreen { Content = viewer });
            engine.Update(new GameTime());
            return (engine, viewer, panel);
        }

        private static void Frames(ScreenEngine engine, int count)
        {
            for (var i = 0; i < count; i++)
            {
                ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
                engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
            }
        }

        [TestMethod]
        public void The_covered_part_of_a_page_becomes_scrollable_above_the_keyboard()
        {
            using var game = new Game();
            var (engine, viewer, _) = Create(game, 1200);

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            viewer.ScrollTo(new PointF(0, 5000));

            Assert.AreEqual(700, viewer.Offset.Y, "400 of content below the screen plus 300 under the keyboard");
        }

        [TestMethod]
        public void Content_that_fits_above_the_keyboard_stays_still()
        {
            using var game = new Game();
            var (engine, viewer, _) = Create(game, 450);

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            viewer.ScrollTo(new PointF(0, 5000));
            Assert.AreEqual(0, viewer.Offset.Y);

            var (engine2, viewer2, _) = Create(game, 600);
            engine2.SetKeyboardInset(300);
            engine2.Update(new GameTime());
            viewer2.ScrollTo(new PointF(0, 5000));
            Assert.AreEqual(100, viewer2.Offset.Y, "only the part reaching under the keyboard");
        }

        [TestMethod]
        public void A_focused_field_comes_into_view_above_the_opening_keyboard()
        {
            using var game = new Game();
            var (engine, viewer, panel) = Create(game, 650);
            var box = new TextBox { Height = 40 };
            panel.Children.Add(box);
            engine.Update(new GameTime());
            box.Focus();
            engine.Update(new GameTime());
            Assert.AreEqual(0, viewer.Offset.Y);

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());

            Assert.IsTrue(box.BoundingRect.Bottom <= 500.5f, $"field bottom {box.BoundingRect.Bottom} is above the keyboard top 500");
        }

        [TestMethod]
        public void Focus_moving_to_a_covered_field_while_the_keyboard_is_open_brings_it_up()
        {
            using var game = new Game();
            var (engine, viewer, panel) = Create(game, 100);
            var first = new TextBox { Height = 40 };
            var spacer = new Border { Height = 500 };
            var second = new TextBox { Height = 40 };
            panel.Children.Add(first);
            panel.Children.Add(spacer);
            panel.Children.Add(second);
            engine.Update(new GameTime());
            first.Focus();
            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            Assert.IsTrue(second.BoundingRect.Top > 500, "starts under the keyboard");

            second.Focus();
            engine.Update(new GameTime());

            Assert.IsTrue(second.BoundingRect.Bottom <= 500.5f, $"field bottom {second.BoundingRect.Bottom} is above the keyboard top 500");
        }

        [TestMethod]
        public void Closing_the_keyboard_glides_back_without_a_jump()
        {
            using var game = new Game();
            var (engine, viewer, _) = Create(game, 1200);
            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            viewer.ScrollTo(new PointF(0, 700));

            engine.SetKeyboardInset(0);
            Frames(engine, 1);
            Assert.IsTrue(viewer.Offset.Y > 600, $"no jump: {viewer.Offset.Y}");

            Frames(engine, 60);
            Assert.AreEqual(400, viewer.Offset.Y, "settles at the end of the content");
            Assert.AreEqual(1200, viewer.Extent.Height);
        }

        [TestMethod]
        public void A_closing_keyboard_leaves_an_offset_inside_the_content_alone()
        {
            using var game = new Game();
            var (engine, viewer, _) = Create(game, 1200);
            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            viewer.ScrollTo(new PointF(0, 250));

            engine.SetKeyboardInset(0);
            Frames(engine, 2);

            Assert.AreEqual(250, viewer.Offset.Y);
            Assert.AreEqual(1200, viewer.Extent.Height);
        }

        [TestMethod]
        public void Only_the_outermost_vertical_viewer_extends()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 800);
            var innerPanel = new StackPanel();
            innerPanel.Children.Add(new Border { Height = 1000 });
            var inner = new ScrollViewer { Content = innerPanel, Height = 400, ShowScrollBars = false };
            var outerPanel = new StackPanel();
            outerPanel.Children.Add(new Border { Height = 600 });
            outerPanel.Children.Add(inner);
            var outer = new ScrollViewer { Content = outerPanel, ShowScrollBars = false };
            engine.NavigateToScreen(new TestScreen { Content = outer });
            engine.Update(new GameTime());

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());

            Assert.AreEqual(1000, inner.Extent.Height);
            Assert.AreEqual(1300, outer.Extent.Height);
        }

        [TestMethod]
        public void The_extension_can_be_turned_off()
        {
            using var game = new Game();
            var (engine, viewer, _) = Create(game, 1200);
            viewer.ScrollsAboveKeyboard = false;

            engine.SetKeyboardInset(300);
            engine.Update(new GameTime());
            viewer.ScrollTo(new PointF(0, 5000));

            Assert.AreEqual(400, viewer.Offset.Y);
        }
    }
}

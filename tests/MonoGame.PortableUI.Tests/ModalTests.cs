using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ModalTests
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

        private static (ScreenEngine Engine, TestScreen Screen, VirtualInputSource Source, Button Background) Setup(Game game)
        {
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            engine.TransitionDuration = TimeSpan.Zero;
            var screen = new TestScreen();
            var background = new Button { Text = "Behind", Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            screen.Content = background;
            engine.NavigateToScreen(screen);
            screen.Update();
            return (engine, screen, new VirtualInputSource(), background);
        }

        private static void Update(ScreenEngine engine)
        {
            engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
        }

        private static void Click(ScreenEngine engine, VirtualInputSource source, PointF at)
        {
            source.SetPointer(at);
            Update(engine);
            source.SetPointer(at, leftDown: true);
            Update(engine);
            source.SetPointer(at);
            Update(engine);
        }

        private static void Press(ScreenEngine engine, VirtualInputSource source, Keys key)
        {
            source.SetKeyboardState(new KeyboardState(key));
            Update(engine);
            source.SetKeyboardState(new KeyboardState());
            Update(engine);
        }

        private static ModalScreen Open(ScreenEngine engine, VirtualInputSource source, Control content, ModalOptions? options = null)
        {
            var modal = engine.ShowModal(content, options);
            modal.InputSource = source;
            Update(engine);
            return modal;
        }

        [TestMethod]
        public void Input_behind_the_modal_is_blocked_and_a_scrim_tap_cancels_it()
        {
            using var game = new Game();
            var (engine, _, source, background) = Setup(game);
            var behindClicks = 0;
            background.Click += (_, _) => behindClicks++;
            var modal = Open(engine, source, new Border { Width = 100, Height = 60 });
            ModalClosedEventArgs? closed = null;
            modal.Closed += (_, args) => closed = args;

            Click(engine, source, new PointF(20, 10));

            Assert.AreEqual(0, behindClicks, "the button behind the scrim must not receive the click");
            Assert.IsNotNull(closed);
            Assert.IsTrue(closed.Cancelled);
            Assert.IsFalse(modal.IsOpen);
        }

        [TestMethod]
        public void Clicking_inside_the_panel_does_not_dismiss()
        {
            using var game = new Game();
            var (engine, _, source, _) = Setup(game);
            var inner = new Button { Text = "OK", Width = 100, Height = 60 };
            var innerClicks = 0;
            inner.Click += (_, _) => innerClicks++;
            var modal = Open(engine, source, inner);
            var centre = new PointF(inner.ClippingRect.Left + 10, inner.ClippingRect.Top + 10);

            Click(engine, source, centre);

            Assert.AreEqual(1, innerClicks);
            Assert.IsTrue(modal.IsOpen);
        }

        [TestMethod]
        public void Escape_cancels_unless_the_modal_is_not_cancelable()
        {
            using var game = new Game();
            var (engine, _, source, _) = Setup(game);
            var locked = Open(engine, source, new Border { Width = 50, Height = 50 }, new ModalOptions { Cancelable = false });

            Press(engine, source, Keys.Escape);
            Assert.IsTrue(locked.IsOpen);

            locked.Close();
            Assert.IsFalse(locked.IsOpen);

            var cancelable = Open(engine, source, new Border { Width = 50, Height = 50 });
            Press(engine, source, Keys.Escape);
            Assert.IsFalse(cancelable.IsOpen);
        }

        [TestMethod]
        public void Keyboard_focus_stays_inside_the_modal()
        {
            using var game = new Game();
            var (engine, screen, source, background) = Setup(game);
            screen.InputSource = source;
            background.Focus();
            Press(engine, source, Keys.Down); // keyboard mode
            var first = new Button { Text = "A", Width = 60, Height = 30 };
            var second = new Button { Text = "B", Width = 60, Height = 30 };
            var stack = new StackPanel();
            stack.AddChild(first);
            stack.AddChild(second);
            Open(engine, source, stack);

            Assert.AreSame(first, ScreenEngine.Instance!.FocusedControl);
            for (var i = 0; i < 4; i++)
            {
                Press(engine, source, Keys.Tab);
                Assert.IsTrue(ReferenceEquals(ScreenEngine.Instance!.FocusedControl, first) || ReferenceEquals(ScreenEngine.Instance!.FocusedControl, second),
                    "Tab must cycle inside the modal");
            }
        }

        [TestMethod]
        public void Closing_a_modal_closes_the_ones_stacked_above_it_first()
        {
            using var game = new Game();
            var (engine, screen, source, _) = Setup(game);
            var order = new List<string>();
            var lower = Open(engine, source, new Border { Width = 50, Height = 50 });
            var upper = Open(engine, source, new Border { Width = 40, Height = 40 });
            lower.Closed += (_, _) => order.Add("lower");
            upper.Closed += (_, _) => order.Add("upper");

            lower.Close();

            CollectionAssert.AreEqual(new[] { "upper", "lower" }, order);
            Assert.AreSame(screen, engine.ActiveScreen);
        }

        [TestMethod]
        public void Bottom_sheet_sits_on_the_bottom_edge_with_top_corners_rounded()
        {
            using var game = new Game();
            var (engine, _, source, _) = Setup(game);
            var sheet = Open(engine, source, new Border(), new ModalOptions { Placement = ModalPlacement.BottomSheet, SheetHeight = 120, CornerRadius = 16 });

            Assert.AreEqual(300, sheet.Panel.BoundingRect.Bottom, 0.5f);
            Assert.AreEqual(120, sheet.Panel.BoundingRect.Height, 0.5f);
            Assert.AreEqual(400, sheet.Panel.BoundingRect.Width, 0.5f);
            Assert.AreEqual(16, sheet.Panel.CornerRadius.TopLeft, 0.001f);
            Assert.AreEqual(0, sheet.Panel.CornerRadius.BottomLeft, 0.001f);
            Assert.IsNotNull(sheet.Panel.EffectiveClip, "content is clipped to the rounded outline");
        }
    }
}

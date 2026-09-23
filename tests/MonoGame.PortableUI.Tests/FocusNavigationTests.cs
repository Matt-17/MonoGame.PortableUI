using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class FocusNavigationTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            ScreenEngine.FocusedControl = null;
        }

        private static (Game Game, ScreenEngine Engine, TestScreen Screen, VirtualInputSource Source) CreateScreen(Control content)
        {
            var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            screen.Content = content;
            engine.NavigateToScreen(screen);
            screen.Update();
            return (game, engine, screen, source);
        }

        private static void PressKey(TestScreen screen, VirtualInputSource source, Keys key)
        {
            source.SetKeyboardState(new KeyboardState(key));
            screen.Update();
            source.SetKeyboardState(new KeyboardState());
            screen.Update();
        }

        private static void PressPad(TestScreen screen, VirtualInputSource source, Buttons buttons)
        {
            source.SetGamePadState(Pad(buttons));
            screen.Update();
            source.SetGamePadState(Pad(0));
            screen.Update();
        }

        private static GamePadState Pad(Buttons buttons)
        {
            ButtonState State(Buttons button) => (buttons & button) != 0 ? ButtonState.Pressed : ButtonState.Released;
            return new GamePadState(
                new GamePadThumbSticks(),
                new GamePadTriggers(),
                new GamePadButtons(buttons),
                new GamePadDPad(State(Buttons.DPadUp), State(Buttons.DPadDown), State(Buttons.DPadLeft), State(Buttons.DPadRight)));
        }

        private static (Grid Grid, Button[,] Buttons) CreateButtonGrid()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            var buttons = new Button[2, 2];
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    buttons[row, column] = new Button { Text = $"{row}{column}", Width = 80, Height = 30 };
                    grid.AddChild(buttons[row, column], row, column);
                }
            }
            return (grid, buttons);
        }

        [TestMethod]
        public void Arrow_keys_move_focus_to_the_neighbour_in_that_direction()
        {
            var (grid, buttons) = CreateButtonGrid();
            var (game, _, screen, source) = CreateScreen(grid);
            using var _ = game;
            buttons[0, 0].Focus();

            PressKey(screen, source, Keys.Right);
            Assert.AreSame(buttons[0, 1], ScreenEngine.FocusedControl);

            PressKey(screen, source, Keys.Down);
            Assert.AreSame(buttons[1, 1], ScreenEngine.FocusedControl);

            PressKey(screen, source, Keys.Left);
            Assert.AreSame(buttons[1, 0], ScreenEngine.FocusedControl);

            // Nothing further left: focus stays.
            PressKey(screen, source, Keys.Left);
            Assert.AreSame(buttons[1, 0], ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Arrow_with_nothing_focused_focuses_the_first_stop()
        {
            var (grid, _) = CreateButtonGrid();
            var (game, _, screen, source) = CreateScreen(grid);
            using var _ = game;

            PressKey(screen, source, Keys.Down);

            Assert.IsNotNull(ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void List_box_keeps_vertical_arrows_but_horizontal_ones_leave_it()
        {
            var listBox = new ListBox { Width = 150, Height = 120 };
            listBox.Items.AddRange(new object[] { "a", "b", "c" });
            var button = new Button { Text = "Next", Width = 80, Height = 30 };
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.AddChild(listBox);
            stack.AddChild(button);
            var (game, _, screen, source) = CreateScreen(stack);
            using var _ = game;
            listBox.SelectedIndex = 0;
            listBox.Focus();

            PressKey(screen, source, Keys.Down);
            Assert.AreSame(listBox, ScreenEngine.FocusedControl);
            Assert.AreEqual(1, listBox.SelectedIndex);

            PressKey(screen, source, Keys.Right);
            Assert.AreSame(button, ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Gamepad_dpad_navigates_a_activates_and_b_requests_back()
        {
            var (grid, buttons) = CreateButtonGrid();
            var (game, _, screen, source) = CreateScreen(grid);
            using var _ = game;
            var clicks = 0;
            buttons[1, 0].Click += (_, _) => clicks++;
            var backRequests = 0;
            screen.BackRequested += (_, _) => backRequests++;
            buttons[0, 0].Focus();

            PressPad(screen, source, Buttons.DPadDown);
            Assert.AreSame(buttons[1, 0], ScreenEngine.FocusedControl);

            PressPad(screen, source, Buttons.A);
            Assert.AreEqual(1, clicks);

            PressPad(screen, source, Buttons.B);
            Assert.AreEqual(1, backRequests);
        }

        [TestMethod]
        public void Gamepad_b_closes_an_open_popup_before_requesting_back()
        {
            var (grid, buttons) = CreateButtonGrid();
            var (game, _, screen, source) = CreateScreen(grid);
            using var _ = game;
            var backRequests = 0;
            screen.BackRequested += (_, _) => backRequests++;
            buttons[0, 0].Focus();
            screen.ShowFlyOut(new PointF(10, 10), new Border { Width = 40, Height = 20 }, removeOnRelease: false);

            PressPad(screen, source, Buttons.B);

            Assert.IsFalse(screen.IsFlyOutOpen);
            Assert.AreEqual(0, backRequests);
        }

        [TestMethod]
        public void Shoulder_buttons_step_through_tab_stops()
        {
            var (grid, buttons) = CreateButtonGrid();
            var (game, _, screen, source) = CreateScreen(grid);
            using var _ = game;
            buttons[0, 0].Focus();

            PressPad(screen, source, Buttons.RightShoulder);
            var afterNext = ScreenEngine.FocusedControl;
            Assert.AreNotSame(buttons[0, 0], afterNext);

            PressPad(screen, source, Buttons.LeftShoulder);
            Assert.AreSame(buttons[0, 0], ScreenEngine.FocusedControl);
        }

        [TestMethod]
        public void Focus_moved_below_the_viewport_scrolls_it_into_view()
        {
            var stack = new StackPanel();
            var items = new Button[12];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = new Button { Text = $"Item {i}", Height = 40 };
                stack.AddChild(items[i]);
            }
            var viewer = new ScrollViewer { Content = stack, Height = 120, VerticalAlignment = VerticalAlignment.Top };
            var (game, _, screen, source) = CreateScreen(viewer);
            using var _ = game;
            items[2].Focus();

            PressKey(screen, source, Keys.Down);
            PressKey(screen, source, Keys.Down);

            Assert.AreSame(items[4], ScreenEngine.FocusedControl);
            Assert.IsTrue(viewer.Offset.Y > 0, $"expected the viewer to scroll, offset {viewer.Offset.Y}");
            Assert.IsTrue(items[4].BoundingRect.Bottom <= viewer.BoundingRect.Bottom + 0.5f);
        }

        [TestMethod]
        public void Candidates_behind_the_current_control_are_rejected()
        {
            var from = new Rect(100, 100, 50, 20);

            Assert.IsFalse(Screen.TryScoreFocusCandidate(from, new Rect(20, 100, 50, 20), FocusDirection.Right, out _));
            Assert.IsTrue(Screen.TryScoreFocusCandidate(from, new Rect(200, 100, 50, 20), FocusDirection.Right, out var aligned));
            Assert.IsTrue(Screen.TryScoreFocusCandidate(from, new Rect(170, 200, 50, 20), FocusDirection.Right, out var diagonal));
            Assert.IsTrue(aligned < diagonal, "a control in the same row beats a closer diagonal one");
        }
    }
}

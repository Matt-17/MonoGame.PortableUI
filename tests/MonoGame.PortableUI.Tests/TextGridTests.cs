using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class TextGridTests
    {
        private sealed class CellScreen : Screen
        {
            public CellScreen()
            {
                // One 8x16 cell at column 10, row 5 of an 80x25 grid on 640x400.
                Target = new Button { Width = 8, Height = 16, Margin = new Thickness(80, 80, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                Content = Target;
            }

            public Button Target { get; }
        }

        private static (UISurface Surface, VirtualInputSource Input, CellScreen Screen) Create(Game game, TextGrid grid, int width, int height)
        {
            var screen = new CellScreen();
            var input = new VirtualInputSource();
            var surface = new UISurface(game, screen, width, height) { InputSource = input, TextGrid = grid };
            return (surface, input, screen);
        }

        private static void Tick(UISurface surface)
        {
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            surface.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
        }

        [TestMethod]
        public void Pointer_snaps_to_the_centre_of_its_cell_in_corner_and_edge_cells()
        {
            using var game = new Game();
            var (surface, input, screen) = Create(game, TextGrid.Dos, 640, 400);
            using (surface)
            {
                foreach (var (raw, expected) in new[]
                {
                    (new PointF(0.5f, 0.5f), new PointF(4, 8)),          // top-left corner cell
                    (new PointF(639.5f, 399.5f), new PointF(636, 392)),  // bottom-right corner cell
                    (new PointF(639, 200), new PointF(636, 200)),        // right edge, row 12
                    (new PointF(83.9f, 95.9f), new PointF(84, 88))       // inside cell (10, 5)
                })
                {
                    input.SetPointer(raw);
                    Tick(surface);
                    Assert.AreEqual(expected, screen.LastMousePosition, $"raw {raw}");
                }
            }
        }

        [TestMethod]
        public void With_display_curvature_the_snap_happens_after_undoing_the_curve()
        {
            using var game = new Game();
            var (surface, input, screen) = Create(game, TextGrid.Dos, 640, 400);
            using (surface)
            {
                const float distortion = 0.12f;
                surface.PostEffects = new PostEffect[] { new CrtBarrelPostEffect { Distortion = distortion } };
                var area = new Rect(0, 0, 640, 400);
                // Near the curved top-left corner and on the curved left edge.
                foreach (var shown in new[] { new PointF(40, 30), new PointF(30, 200) })
                {
                    input.SetPointer(shown);
                    Tick(surface);
                    var ui = PostProcessManager.InverseBarrel(shown, area, distortion);
                    var expected = TextGrid.Dos.Snap(ui, area);
                    Assert.AreEqual(expected, screen.LastMousePosition, $"shown {shown}");
                    Assert.AreNotEqual(expected, TextGrid.Dos.Snap(shown, area), "snapping first would pick another cell here");
                }
            }
        }

        [TestMethod]
        public void Hover_and_click_act_on_the_visible_cell()
        {
            using var game = new Game();
            var (surface, input, screen) = Create(game, TextGrid.Dos, 640, 400);
            using (surface)
            {
                var clicks = 0;
                screen.Target.Click += (_, _) => clicks++;

                input.SetPointer(new PointF(87.9f, 95.9f)); // inside cell (10, 5): the button
                Tick(surface);
                Assert.IsTrue(screen.Target.IsMouseHovering);
                input.SetPointer(new PointF(87.9f, 95.9f), leftDown: true);
                Tick(surface);
                input.SetPointer(new PointF(87.9f, 95.9f));
                Tick(surface);
                Assert.AreEqual(1, clicks, "a click on the cell clicks the control under it");

                input.SetPointer(new PointF(88.1f, 90)); // cell (11, 5): next to it
                Tick(surface);
                Assert.IsFalse(screen.Target.IsMouseHovering, "the neighbouring cell does not hit it");
            }
        }

        [TestMethod]
        public void Captured_slow_motion_accumulates_fractions_of_a_cell()
        {
            using var game = new Game();
            var (surface, input, screen) = Create(game, TextGrid.Dos, 640, 400);
            using (surface)
            {
                var capture = new SurfacePointerCapture(surface, input);
                capture.Capture(new PointF(320.5f, 200.5f)); // column 40
                Tick(surface);
                Assert.AreEqual(324, screen.LastMousePosition.X);

                // 30 moves of 0.3 px each: none moves a cell on its own, together they move one.
                for (var i = 0; i < 30; i++)
                {
                    capture.Update(new Vector2(0.3f, 0), false, false, false, new KeyboardState());
                    Tick(surface);
                }
                Assert.AreEqual(332, screen.LastMousePosition.X, "column 41 after 9 px of slow motion");
            }
        }

        [TestMethod]
        public void Each_surface_has_its_own_grid()
        {
            using var game = new Game();
            var (dos, dosInput, dosScreen) = Create(game, TextGrid.Dos, 640, 400);
            var (c64, c64Input, c64Screen) = Create(game, TextGrid.C64, 320, 200);
            using (dos)
            using (c64)
            {
                dosInput.SetPointer(new PointF(101, 51));
                c64Input.SetPointer(new PointF(101, 51));
                Tick(dos);
                Tick(c64);
                Assert.AreEqual(new PointF(100, 56), dosScreen.LastMousePosition, "80x25 on 640x400: 8x16 cells");
                Assert.AreEqual(new PointF(100, 52), c64Screen.LastMousePosition, "40x25 on 320x200: 8x8 cells");
            }
        }

        [TestMethod]
        public void Insert_toggles_overwrite_which_replaces_the_character_at_the_caret()
        {
            var box = new TextBox { Text = "ABCD" };
            box.CursorPosition = 1;
            box.HandleCharPressed('x');
            Assert.AreEqual("AxBCD", box.Text, "insert mode inserts");

            box.HandleCommandPressed(KeyboardCommand.Insert, KeyboardModifiers.None);
            Assert.IsTrue(box.IsOverwriteMode);
            box.HandleCharPressed('y');
            Assert.AreEqual("AxyCD", box.Text, "overwrite replaces the character at the caret");

            box.CursorPosition = box.Text.Length;
            box.HandleCharPressed('z');
            Assert.AreEqual("AxyCDz", box.Text, "at the end it appends");
        }
    }
}

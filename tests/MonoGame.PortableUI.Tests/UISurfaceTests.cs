using MonoGame.PortableUI.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class UISurfaceTests
    {
        [TestMethod]
        public void UISurface_uses_non_primary_engine_and_fixed_virtual_size()
        {
            using var game = new Game();
            var primary = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            using var surface = new UISurface(game, new EmptyScreen(), 640, 400, PortableThemes.Resolve("dos").CreateTheme());

            Assert.AreSame(primary, ScreenEngine.Instance);
            Assert.AreNotSame(primary, surface.Engine);
            Assert.AreEqual(640, surface.Engine.ScreenRect.Width);
            Assert.AreEqual(400, surface.Engine.ScreenRect.Height);
            Assert.AreEqual("dos", PortableThemes.Resolve("dos").Id);
        }

        [TestMethod]
        public void Surface_focus_manager_activates_one_surface_at_a_time()
        {
            using var game = new Game();
            using var first = new UISurface(game, new EmptyScreen(), 320, 200);
            using var second = new UISurface(game, new EmptyScreen(), 320, 200);
            var focus = new SurfaceFocusManager();

            focus.Activate(first);

            Assert.AreSame(first, focus.ActiveSurface);
            Assert.IsTrue(first.HasKeyboardFocus);

            focus.Activate(second);

            Assert.IsFalse(first.HasKeyboardFocus);
            Assert.IsTrue(second.HasKeyboardFocus);
            Assert.AreSame(second, focus.ActiveSurface);
        }

        [TestMethod]
        public void ExternalBackdrop_flows_between_surface_and_screen()
        {
            using var game = new Game();
            var screen = new EmptyScreen();
            using var surface = new UISurface(game, screen, 320, 200);

            Assert.IsNull(surface.ExternalBackdrop);

            // the surface property is a pass-through to the screen (textures need a live
            // GraphicsDevice, so headless tests exercise the plumbing with null round-trips)
            screen.ExternalBackdrop = null;
            Assert.IsNull(surface.ExternalBackdrop);
            surface.ExternalBackdrop = null;
            Assert.IsNull(screen.ExternalBackdrop);
        }

        private sealed class EmptyScreen : Screen
        {
        }

        [TestMethod]
        public void Points_on_the_curved_crt_rim_are_not_on_the_display()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new EmptyScreen(), 400, 300);
            var corner = new PointF(3, 3);
            Assert.IsTrue(surface.IsPointOnDisplay(corner), "flat display: the whole surface is picture");

            surface.PostEffects = new PostEffect[] { new CrtBarrelPostEffect { Distortion = 0.1f } };
            Assert.IsFalse(surface.IsPointOnDisplay(corner), "the corner is the dark rim of the curved picture");
            Assert.IsTrue(surface.IsPointOnDisplay(new PointF(200, 150)), "the centre is picture");
            Assert.IsFalse(surface.IsPointOnDisplay(new PointF(-5, 150)), "off the surface");
        }

        private sealed class FieldScreen : Screen
        {
            public FieldScreen()
            {
                Field = new MonoGame.PortableUI.Controls.TextBox();
                Content = Field;
            }

            public MonoGame.PortableUI.Controls.TextBox Field { get; }
        }

        /// <summary>The game window raises TextInput once; every engine is subscribed to it.</summary>
        private static void TypeIntoWindow(char character, params ScreenEngine[] engines)
        {
            foreach (var engine in engines)
                engine.OnWindowTextInput(character);
        }

        [TestMethod]
        public void Window_text_reaches_exactly_one_engine_the_focused_surface_or_else_the_main_ui()
        {
            using var game = new Game();
            var main = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            var mainScreen = new FieldScreen();
            main.NavigateToScreen(mainScreen);
            using var a = new UISurface(game, new FieldScreen(), 320, 200);
            using var b = new UISurface(game, new FieldScreen(), 320, 200);
            var fieldA = ((FieldScreen)a.Screen).Field;
            var fieldB = ((FieldScreen)b.Screen).Field;
            // Every field keeps control focus in its own engine, the realistic worst case.
            mainScreen.Field.Focus();
            fieldA.Focus();
            fieldB.Focus();
            var engines = new[] { main, a.Engine, b.Engine };

            TypeIntoWindow('m', engines);
            Assert.AreEqual("m", mainScreen.Field.Text, "no surface focused: the main UI types");
            Assert.AreEqual("", fieldA.Text);
            Assert.AreEqual("", fieldB.Text);

            var focus = new SurfaceFocusManager();
            focus.Activate(b);
            TypeIntoWindow('x', engines);
            focus.RouteTextInput('x'); // the old advertised route must not type a second time
            Assert.AreEqual("x", fieldB.Text, "the active surface types exactly once");
            Assert.AreEqual("", fieldA.Text, "an inactive surface stays unchanged");
            Assert.AreEqual("m", mainScreen.Field.Text, "the main UI does not type while a surface has the keyboard");

            focus.Activate(a);
            Assert.IsFalse(b.HasKeyboardFocus);
            TypeIntoWindow('y', engines);
            Assert.AreEqual("y", fieldA.Text);
            Assert.AreEqual("x", fieldB.Text);

            focus.Activate(null);
            TypeIntoWindow('z', engines);
            Assert.AreEqual("mz", mainScreen.Field.Text, "released: back to the main UI");

            a.HasKeyboardFocus = true;
            a.Dispose();
            TypeIntoWindow('w', main, b.Engine);
            Assert.AreEqual("mzw", mainScreen.Field.Text, "a disposed surface gives the keyboard back");
        }
    }
}

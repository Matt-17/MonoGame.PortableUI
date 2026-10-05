using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Hosting many UISurfaces: on-demand drawing (#93), the post-effect switch (#97) and
    /// per-surface state that must not leak into other surfaces (#95).</summary>
    [TestClass]
    [DoNotParallelize] // drives the global ScreenSystem.TotalTime clock and ScreenEngine.ScaleFactor
    public class SurfaceHostingTests
    {
        private sealed class TestScreen : Screen
        {
        }

        [TestInitialize]
        public void Reset() => ScreenSystem.TotalTime = TimeSpan.Zero;

        private static void Advance(UISurface surface, double milliseconds)
        {
            var total = ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(milliseconds);
            surface.Update(new GameTime(total, TimeSpan.FromMilliseconds(milliseconds)));
        }

        [TestMethod]
        public void A_surface_without_a_frame_needs_one_now()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 320, 200);

            Assert.IsTrue(surface.NeedsRedraw);
            Assert.AreEqual(TimeSpan.Zero, surface.NextRedrawDue);
        }

        [TestMethod]
        public void Peeking_does_not_consume_a_request_and_undrawn_requests_stay_pending()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 320, 200);
            var engine = surface.Engine;
            Advance(surface, 16);
            engine.ConsumeRedrawRequest();
            Advance(surface, 500);
            engine.ConsumeRedrawRequest();
            Assert.IsFalse(engine.PeekRedrawRequest(), "idle after the first frames");

            surface.Invalidate();
            for (var i = 0; i < 5; i++)
            {
                Advance(surface, 100);
                Assert.IsTrue(engine.PeekRedrawRequest(), "an unseen surface keeps its request across updates");
            }
            Assert.AreEqual(TimeSpan.Zero, engine.NextRedrawDue);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Assert.IsFalse(engine.PeekRedrawRequest());
        }

        [TestMethod]
        public void A_scheduled_frame_is_reported_as_the_next_due_time_until_it_is_reached()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 320, 200);
            var engine = surface.Engine;
            Advance(surface, 16);
            engine.ConsumeRedrawRequest();
            Advance(surface, 500);
            engine.ConsumeRedrawRequest();

            var blink = ScreenSystem.TotalTime + TimeSpan.FromMilliseconds(530);
            engine.RequestRedrawAt(blink);

            Assert.IsFalse(engine.PeekRedrawRequest());
            Assert.AreEqual(blink, engine.NextRedrawDue);
            Advance(surface, 600);
            Assert.IsTrue(engine.PeekRedrawRequest(), "the caret flip is due");
            Assert.AreEqual(TimeSpan.Zero, engine.NextRedrawDue);
            Assert.IsTrue(engine.ConsumeRedrawRequest());
            Assert.AreEqual(TimeSpan.MaxValue, engine.NextRedrawDue, "idle again");
        }

        [TestMethod]
        public void PostEffectMode_controls_which_stages_draw_and_the_pointer_mapping()
        {
            using var game = new Game();
            var screen = new TestScreen();
            var theme = PortableThemes.Resolve("phosphor").CreateTheme();
            using var surface = new UISurface(game, screen, 400, 300, theme);
            Assert.IsTrue(theme.PostEffects.Count > 0, "precondition: the theme has a look");
            var barrel = new CrtBarrelPostEffect { Distortion = 0.1f };
            surface.PostEffects = new PostEffect[] { barrel };
            var corner = new PointF(3, 3);

            Assert.AreEqual(PostEffectMode.All, surface.PostEffectMode);
            Assert.IsFalse(surface.IsPointOnDisplay(corner));
            CollectionAssert.Contains(new System.Collections.Generic.List<PostEffect>(screen.GetScreenPostEffects()!), barrel);

            surface.PostEffectMode = PostEffectMode.ThemeOnly;
            Assert.IsTrue(surface.IsPointOnDisplay(corner), "no display stage: the host maps the curve");
            var effects = screen.GetScreenPostEffects();
            Assert.IsNotNull(effects);
            CollectionAssert.DoesNotContain(new System.Collections.Generic.List<PostEffect>(effects), barrel);

            surface.PostEffectMode = PostEffectMode.None;
            Assert.IsNull(screen.GetScreenPostEffects(), "flat: no post effects at all");
            Assert.AreEqual(corner, surface.MapDisplayToUi(corner));
        }

        [TestMethod]
        public void Switching_the_post_effect_mode_asks_for_a_frame()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 320, 200);
            surface.Engine.ConsumeRedrawRequest();
            Advance(surface, 500);
            surface.Engine.ConsumeRedrawRequest();

            surface.PostEffectMode = PostEffectMode.None;

            Assert.IsTrue(surface.Engine.PeekRedrawRequest());
        }

        [TestMethod]
        public void A_smaller_tile_renders_at_the_smallest_step_that_still_covers_it()
        {
            using var game = new Game();
            using var surface = new UISurface(game, new TestScreen(), 640, 400);

            Assert.AreEqual(1f, surface.DrawResolutionFor(640, 400), "full size");
            Assert.AreEqual(1f, surface.DrawResolutionFor(1280, 800), "a larger tile never renders above the surface's size");
            Assert.AreEqual(1f, surface.DrawResolutionFor(500, 312), "0.78 needs the full size");
            Assert.AreEqual(0.75f, surface.DrawResolutionFor(480, 300));
            Assert.AreEqual(0.5f, surface.DrawResolutionFor(320, 200), "exactly half");
            Assert.AreEqual(0.375f, surface.DrawResolutionFor(200, 125));
            Assert.AreEqual(0.125f, surface.DrawResolutionFor(40, 25));
            Assert.AreEqual(0.125f, surface.DrawResolutionFor(10, 6), "the smallest step is the floor");
            Assert.AreEqual(0.75f, surface.DrawResolutionFor(480, 100), "the larger axis decides");

            surface.LayoutScale = 2;
            Assert.AreEqual(0.5f, surface.DrawResolutionFor(640, 400), "steps are fractions of the pixel size (LayoutScale included)");
        }

        [TestMethod]
        public void Creating_a_surface_does_not_reset_the_global_scale_factor()
        {
            using var game = new Game();
            var previous = ScreenEngine.ScaleFactor;
            try
            {
                ScreenEngine.ScaleFactor = 2;
                using var surface = new UISurface(game, new TestScreen(), 320, 200);
                Assert.AreEqual(2, ScreenEngine.ScaleFactor);
            }
            finally
            {
                ScreenEngine.ScaleFactor = previous;
            }
        }

        [TestMethod]
        public void Controls_built_for_a_surface_take_the_surface_theme_not_the_main_one()
        {
            using var game = new Game();
            var mainTheme = PortableTheme.CreateDefault();
            ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false, Theme = mainTheme });
            var surfaceTheme = PortableThemes.Resolve("dos").CreateTheme();
            Assert.AreNotSame(mainTheme.RadioButtonDotBrush, surfaceTheme.RadioButtonDotBrush, "precondition: the themes differ");

            // Built while the main engine is ScreenEngine.Instance, as station code does.
            var radio = new RadioButton { Text = "A" };
            var button = new Button { Text = "B" };
            var panel = new StackPanel();
            panel.AddChild(radio);
            panel.AddChild(button);
            using var surface = new UISurface(game, new TestScreen { Content = panel }, 320, 200, surfaceTheme);
            Advance(surface, 16);

            Assert.AreSame(surfaceTheme.RadioButtonDotBrush, radio.DotBrush);
            Assert.AreEqual(surfaceTheme.CheckBoxTextColor, radio.TextColor);
        }

        [TestMethod]
        public void A_discarded_surface_does_not_stay_alive_through_its_radio_groups()
        {
            using var game = new Game();
            var radio = BuildAndDisposeSurfaceWithRadios(game);

            for (var i = 0; i < 3 && radio.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.IsFalse(radio.IsAlive, "the static group table must not keep the surface's tree alive");

            // Same-named groups keep working for live trees after the dead entries are pruned.
            var a = new RadioButton { RadioGroup = "station-mode" };
            var b = new RadioButton { RadioGroup = "station-mode" };
            var panel = new StackPanel();
            panel.AddChild(a);
            panel.AddChild(b);
            b.IsChecked = true;
            Assert.IsFalse(a.IsChecked);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference BuildAndDisposeSurfaceWithRadios(Game game)
        {
            var radio = new RadioButton { RadioGroup = "station-mode" };
            var other = new RadioButton { RadioGroup = "station-mode" };
            var panel = new StackPanel();
            panel.AddChild(radio);
            panel.AddChild(other);
            var surface = new UISurface(game, new TestScreen { Content = panel }, 320, 200);
            Advance(surface, 16);
            surface.Dispose();
            return new WeakReference(radio);
        }
    }
}

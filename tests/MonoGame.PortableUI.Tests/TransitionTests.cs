using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>Implicit transitions (control.Transitions) and the draw-time colour fades.</summary>
    [TestClass]
    public class TransitionTests
    {
        private sealed class TestControl : Control
        {
        }

        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        private static TimeSpan Ms(double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);

        private static void At(Control control, double milliseconds)
        {
            ScreenSystem.TotalTime = Ms(milliseconds);
            control.UpdateTimers();
        }

        [TestMethod]
        public void Without_a_transition_a_set_is_immediate()
        {
            var control = new TestControl();
            control.Scale = new Vector2(2);
            Assert.AreEqual(new Vector2(2), control.ShownScale);
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void An_eased_transition_moves_the_shown_value_while_the_getter_returns_the_target()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Scale, Ms(100), Easings.Linear);

            control.Scale = new Vector2(2);
            Assert.AreEqual(new Vector2(2), control.Scale, "getter: the new value");
            Assert.AreEqual(Vector2.One, control.ShownScale, "nothing moved yet");
            Assert.IsTrue(control.IsTransitioning);

            At(control, 50);
            Assert.AreEqual(1.5f, control.ShownScale.X, 0.001f);
            At(control, 100);
            Assert.AreEqual(new Vector2(2), control.ShownScale);
            Assert.IsFalse(control.IsTransitioning);
        }

        [TestMethod]
        public void Retargeting_starts_from_the_shown_value()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Translation, Ms(100), Easings.Linear);
            control.Translation = new Vector2(100, 0);
            At(control, 50);

            control.Translation = Vector2.Zero;
            At(control, 100);
            Assert.AreEqual(25, control.ShownTranslation.X, 0.01f, "halfway from 50 back to 0");
            At(control, 150);
            Assert.AreEqual(Vector2.Zero, control.ShownTranslation);
        }

        [TestMethod]
        public void A_retargeted_spring_keeps_its_velocity()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Translation, SpringSpec.Snappy);
            control.Translation = new Vector2(100, 0);
            At(control, 60);
            var before = control.ShownTranslation.X;

            control.Translation = Vector2.Zero;
            At(control, 76);
            Assert.IsTrue(control.ShownTranslation.X > before, $"still moving outwards: {before} -> {control.ShownTranslation.X}");

            for (var t = 76.0; t < 3000 && control.IsTransitioning; t += 1000 / 60.0)
                At(control, t);
            Assert.IsFalse(control.IsTransitioning);
            Assert.AreEqual(Vector2.Zero, control.ShownTranslation);
        }

        [TestMethod]
        public void Opacity_keeps_the_exact_double_target()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Opacity, Ms(100));
            control.Opacity = 0.3;
            Assert.AreEqual(0.3, control.Opacity);
            At(control, 50);
            Assert.IsTrue(control.ShownOpacity < 1 && control.ShownOpacity > 0.3);
            At(control, 100);
            Assert.AreEqual(0.3, control.ShownOpacity);
            Assert.AreEqual(0.3, control.Opacity);
        }

        [TestMethod]
        public void An_explicit_animation_takes_over_from_a_transition()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Scale, Ms(100), Easings.Linear);
            control.Scale = new Vector2(2);
            At(control, 50);

            control.Animate().Scale(3).Duration(Ms(100)).Ease(Easings.Linear).Start();
            Assert.IsFalse(control.IsTransitioning);
            At(control, 100);
            Assert.AreEqual(2.25f, control.ShownScale.X, 0.001f, "from 1.5 towards 3");
            At(control, 150);
            Assert.AreEqual(new Vector2(3), control.Scale);
        }

        [TestMethod]
        public void Removing_a_transition_jumps_to_its_target()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Scale, Ms(100));
            control.Scale = new Vector2(2);
            At(control, 30);

            control.Transitions.Remove(TransitionProperty.Scale);
            Assert.AreEqual(new Vector2(2), control.ShownScale);
            control.Scale = Vector2.One;
            Assert.AreEqual(Vector2.One, control.ShownScale, "no transition any more");
        }

        [TestMethod]
        public void CompleteTransitions_ends_the_subtree_at_its_targets()
        {
            var parent = new Grid();
            var child = new TestControl();
            parent.AddChild(child);
            child.Transitions.Add(TransitionProperty.Transform, SpringSpec.Gentle);
            child.Translation = new Vector2(40, 0);
            child.Opacity = 0;

            parent.CompleteTransitions();
            Assert.AreEqual(new Vector2(40, 0), child.ShownTranslation);
            Assert.AreEqual(0, child.ShownOpacity);
            Assert.IsFalse(child.IsTransitioning);
        }

        [TestMethod]
        public void A_running_spring_transition_allocates_nothing_per_frame()
        {
            var control = new TestControl();
            control.Transitions.Add(TransitionProperty.Scale | TransitionProperty.Translation, SpringSpec.Wobbly);
            control.Scale = new Vector2(1.2f);
            control.Translation = new Vector2(30, 10);
            At(control, 1);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var t = 2.0; t < 500; t += 1000 / 60.0)
            {
                At(control, t);
                if (t > 200 && t < 220)
                    control.Translation = Vector2.Zero; // retarget mid-flight
            }
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [TestMethod]
        public void A_colour_fade_blends_to_a_new_value_and_snaps_on_a_theme_switch()
        {
            var fade = new ColorFade();
            var spec = new Transition(Ms(100), Easings.Linear);
            Assert.AreEqual(Color.Black, fade.Resolve(Color.Black, spec, Ms(0), 1), "first value shows at once");

            var mid = fade.Resolve(Color.White, spec, Ms(0), 1);
            Assert.AreEqual(Color.Black, mid);
            mid = fade.Resolve(Color.White, spec, Ms(50), 1);
            Assert.AreEqual(128, mid.R, 2);
            Assert.IsTrue(fade.IsRunning);

            Assert.AreEqual(Color.Red, fade.Resolve(Color.Red, spec, Ms(60), 2), "theme switch");
            Assert.IsFalse(fade.IsRunning);
        }

        [TestMethod]
        public void A_brush_fade_from_no_brush_blends_from_transparent()
        {
            var fade = new BrushFade();
            var spec = new Transition(Ms(100), Easings.Linear);
            Assert.IsFalse(fade.Update(null, spec, Ms(0), 1), "first brush shows at once");

            Assert.IsTrue(fade.Update(new SolidColorBrush(Color.Red), spec, Ms(0), 1));
            Assert.IsTrue(fade.Update(new SolidColorBrush(Color.Red), spec, Ms(50), 1));
            Assert.IsTrue(fade.TryGetColor(out var color));
            Assert.AreEqual(255, color.R, 2, "red at half alpha, not dark red");
            Assert.AreEqual(128, color.A, 2);
        }

        [TestMethod]
        public void A_brush_fade_reports_the_blended_colour_midway()
        {
            var fade = new BrushFade();
            var spec = new Transition(Ms(100), Easings.Linear);
            var normal = new SolidColorBrush(Color.White.WithAlpha(0f));
            var hover = new SolidColorBrush(Color.White);
            fade.Update(normal, spec, Ms(0), 1);

            fade.Update(hover, spec, Ms(0), 1);
            Assert.IsTrue(fade.Update(hover, spec, Ms(50), 1));
            Assert.IsTrue(fade.TryGetColor(out var color));
            Assert.AreEqual(255, color.R, 2, "straight colour stays white, not grey");
            Assert.AreEqual(128, color.A, 2);

            Assert.IsFalse(fade.Update(hover, spec, Ms(100), 1), "done");
            Assert.IsFalse(fade.Update(new SolidColorBrush(Color.White), spec, Ms(120), 1), "an equal colour is no change");
        }

        [TestMethod]
        public void A_button_label_fades_with_the_style_transition_duration()
        {
            var button = new Button { Content = new TextBlock { Text = "OK" } };
            var label = (TextBlock)button.Content!;
            button.ChangeVisualState(); // hover, press, enable: the button pushes the state's text colour
            var style = button.ResolveStyle();
            Assert.IsNotNull(style);
            Assert.AreEqual(style!.TransitionDuration, label.OwnerTextColorTransition);
        }
    }
}

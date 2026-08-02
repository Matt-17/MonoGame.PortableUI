using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class AnimationRegressionTests
    {
        [TestInitialize]
        public void ResetTime()
        {
            ScreenSystem.TotalTime = TimeSpan.Zero;
            ScreenEngine.FocusedControl = null;
        }

        [TestMethod]
        public void Scale_animation_interpolates_from_current_value()
        {
            var control = new TestControl();

            control.Animate()
                .Scale(2)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .Start();

            Assert.AreEqual(1, control.Scale.X, 0.001f);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50);
            control.UpdateTimers();

            Assert.AreEqual(1.5f, control.Scale.X, 0.001f);
            Assert.AreEqual(1.5f, control.Scale.Y, 0.001f);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            control.UpdateTimers();

            Assert.AreEqual(2, control.Scale.X, 0.001f);
            Assert.AreEqual(2, control.Scale.Y, 0.001f);
        }

        [TestMethod]
        public void Completion_runs_once_when_animation_finishes()
        {
            var control = new TestControl();
            var completions = 0;

            control.Animate()
                .FadeTo(0)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .OnCompleted(() => completions++)
                .Start();

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50);
            control.UpdateTimers();
            Assert.AreEqual(0, completions);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            control.UpdateTimers();
            Assert.AreEqual(1, completions);
            Assert.AreEqual(0, control.Opacity, 0.001);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(200);
            control.UpdateTimers();
            Assert.AreEqual(1, completions);
        }

        [TestMethod]
        public void Cancel_controls_completion_and_final_value()
        {
            var control = new TestControl();
            var completions = 0;

            var canceled = control.Animate()
                .Scale(2)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .OnCompleted(() => completions++)
                .Start();
            canceled.Cancel();

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            control.UpdateTimers();

            Assert.AreEqual(0, completions);
            Assert.AreEqual(1, control.Scale.X, 0.001f);

            var completed = control.Animate()
                .Scale(3)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .OnCompleted(() => completions++)
                .Start();
            completed.Cancel(true);

            Assert.AreEqual(1, completions);
            Assert.AreEqual(3, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void New_animation_replaces_only_matching_property_tweens()
        {
            var control = new TestControl();

            control.Animate()
                .Scale(2)
                .FadeTo(0)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .Start();

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50);
            control.UpdateTimers();

            control.Animate()
                .Scale(3)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .Start();

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            control.UpdateTimers();

            Assert.AreEqual(0, control.Opacity, 0.001);
            Assert.AreEqual(2.25f, control.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Button_pressed_animation_does_not_change_layout()
        {
            var button = new Button
            {
                Text = "Run",
                Width = 100,
                Height = 30
            };
            button.UpdateLayout(new Rect(0, 0, 100, 30));
            var bounds = button.BoundingRect;

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton>()));
            button.OnMouseDown(new MouseEventArgs(new PointF(5, 5), MouseButton.Left));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(70);
            button.UpdateTimers();

            Assert.AreEqual(0.96f, button.Scale.X, 0.001f);
            Assert.AreEqual(28f / 30f, button.Scale.Y, 0.001f);
            Assert.AreEqual(0, button.Translation.Y, 0.001f);
            Assert.AreEqual(bounds, button.BoundingRect);

            button.OnMouseUp(new MouseEventArgs(new PointF(5, 5), MouseButton.Left));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(160);
            button.UpdateTimers();

            Assert.AreEqual(1, button.Scale.X, 0.001f);
            Assert.AreEqual(1, button.Scale.Y, 0.001f);
            Assert.AreEqual(0, button.Translation.Y, 0.001f);
            Assert.AreEqual(bounds, button.BoundingRect);
        }

        [TestMethod]
        public void Button_pressed_animation_uses_absolute_width_inset()
        {
            var button = new Button
            {
                Text = "Wide",
                Width = 800,
                Height = 30
            };
            button.UpdateLayout(new Rect(0, 0, 800, 30));
            var bounds = button.BoundingRect;

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton>()));
            button.OnMouseDown(new MouseEventArgs(new PointF(5, 5), MouseButton.Left));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(70);
            button.UpdateTimers();

            Assert.AreEqual(0.995f, button.Scale.X, 0.001f);
            Assert.AreEqual(28f / 30f, button.Scale.Y, 0.001f);
            Assert.AreEqual(bounds, button.BoundingRect);
        }

        [TestMethod]
        public void Button_mouse_leave_hides_pressed_animation_until_held_button_reenters()
        {
            var button = new Button
            {
                Text = "Leave",
                Width = 100,
                Height = 30
            };
            button.UpdateLayout(new Rect(0, 0, 100, 30));

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton>()));
            button.OnMouseDown(new MouseEventArgs(new PointF(5, 5), MouseButton.Left));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(70);
            button.UpdateTimers();
            Assert.AreEqual(0.96f, button.Scale.X, 0.001f);

            button.OnMouseLeave(new MouseEventArgs(new PointF(200, 5), new List<MouseButton> { MouseButton.Left }));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(160);
            button.UpdateTimers();

            Assert.AreEqual(1, button.Scale.X, 0.001f);
            Assert.AreEqual(0, button.Translation.Y, 0.001f);

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton> { MouseButton.Left }));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(230);
            button.UpdateTimers();

            Assert.AreEqual(0.96f, button.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Button_mouse_reenter_after_outside_release_keeps_pressed_animation_off()
        {
            var button = new Button
            {
                Text = "Release",
                Width = 100,
                Height = 30
            };
            button.UpdateLayout(new Rect(0, 0, 100, 30));

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton>()));
            button.OnMouseDown(new MouseEventArgs(new PointF(5, 5), MouseButton.Left));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(70);
            button.UpdateTimers();
            Assert.AreEqual(0.96f, button.Scale.X, 0.001f);

            button.OnMouseLeave(new MouseEventArgs(new PointF(200, 5), new List<MouseButton> { MouseButton.Left }));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(160);
            button.UpdateTimers();

            button.OnMouseEnter(new MouseEventArgs(new PointF(5, 5), new List<MouseButton>()));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(230);
            button.UpdateTimers();

            Assert.AreEqual(1, button.Scale.X, 0.001f);
        }

        [TestMethod]
        public void Expanded_easings_preserve_start_and_end_values()
        {
            Easing[] easings =
            {
                Easings.QuadIn,
                Easings.QuadOut,
                Easings.QuadInOut,
                Easings.ExpoIn,
                Easings.ExpoOut,
                Easings.ExpoInOut,
                Easings.BackIn,
                Easings.BackOut,
                Easings.BackInOut,
                Easings.ElasticOut,
                Easings.BounceOut
            };

            foreach (var easing in easings)
            {
                Assert.AreEqual(0, easing(-1), 0.001, easing.Method.Name);
                Assert.AreEqual(1, easing(2), 0.001, easing.Method.Name);
            }
        }

        [TestMethod]
        public void Color_animation_interpolates_text_color()
        {
            var textBlock = new TextBlock
            {
                TextColor = Color.Black
            };

            textBlock.Animate()
                .TextColorTo(Color.White)
                .Duration(TimeSpan.FromMilliseconds(100))
                .Ease(Easings.Linear)
                .Start();

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(50);
            textBlock.UpdateTimers();

            Assert.AreEqual(new Color(127, 127, 127, 255), textBlock.TextColor);

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(100);
            textBlock.UpdateTimers();

            Assert.AreEqual(Color.White, textBlock.TextColor);
        }

        [TestMethod]
        public void Crossfade_brush_clamps_progress()
        {
            var brush = new CrossFadeBrush(Color.Black, Color.White, -1);

            Assert.AreEqual(0, brush.Progress);

            brush.Progress = 2;

            Assert.AreEqual(1, brush.Progress);
        }

        private sealed class TestControl : Control
        {
        }
    }
}

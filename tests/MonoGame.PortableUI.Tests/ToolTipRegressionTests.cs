using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ToolTipRegressionTests
    {
        [TestInitialize]
        public void ResetState()
        {
            ScreenEngine.FocusedControl = null;
            ScreenSystem.TotalTime = TimeSpan.Zero;
        }

        [TestMethod]
        public void Tooltip_hover_delay_controls_visibility_and_mouse_leave_clears_it()
        {
            var screen = new TestScreen();
            var button = AttachButton(screen);
            button.ToolTip = "Run the action";

            button.OnMouseEnter(new MouseEventArgs(new PointF(12, 8), new List<MouseButton>()));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(499);
            button.UpdateTimers();

            Assert.IsFalse(screen.IsToolTipVisibleFor(button));

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(500);
            button.UpdateTimers();

            Assert.IsTrue(screen.IsToolTipVisibleFor(button));

            button.OnMouseLeave(new MouseEventArgs(new PointF(220, 8), new List<MouseButton>()));

            Assert.IsFalse(screen.IsToolTipVisibleFor(button));
        }

        [TestMethod]
        public void Tooltip_touch_long_press_shows_tooltip_and_suppresses_click()
        {
            var screen = new TestScreen();
            var button = AttachButton(screen);
            var clicks = 0;
            button.ToolTip = "Long press help";
            button.Click += (sender, args) => clicks++;

            button.OnTouchDown(new TouchEventArgs(new PointF(8, 8)));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(649);
            button.UpdateTimers();

            Assert.IsFalse(screen.IsToolTipVisibleFor(button));

            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(650);
            button.UpdateTimers();

            Assert.IsTrue(screen.IsToolTipVisibleFor(button));

            button.OnTouchUp(new TouchEventArgs(new PointF(8, 8)));

            Assert.AreEqual(0, clicks);
            Assert.IsFalse(screen.IsToolTipVisibleFor(button));
        }

        [TestMethod]
        public void Context_menu_takes_touch_long_press_precedence_over_tooltip()
        {
            var screen = new TestScreen();
            var button = AttachButton(screen);
            button.ToolTip = "Menu help";
            button.ContextMenu = new ContextMenu();

            button.OnTouchDown(new TouchEventArgs(new PointF(8, 8)));
            ScreenSystem.TotalTime = TimeSpan.FromMilliseconds(650);
            button.UpdateTimers();

            Assert.IsFalse(screen.IsToolTipVisibleFor(button));
        }

        [TestMethod]
        public void Popup_clamping_keeps_rect_inside_each_screen_edge()
        {
            var screenRect = new Rect(0, 0, 100, 80);

            var right = Screen.ClampPopupRect(new Rect(90, 20, 30, 10), screenRect, 4);
            var left = Screen.ClampPopupRect(new Rect(-20, 20, 30, 10), screenRect, 4);
            var top = Screen.ClampPopupRect(new Rect(20, -12, 30, 10), screenRect, 4);
            var bottom = Screen.ClampPopupRect(new Rect(20, 76, 30, 10), screenRect, 4);

            Assert.AreEqual(66, right.Left);
            Assert.AreEqual(4, left.Left);
            Assert.AreEqual(4, top.Top);
            Assert.AreEqual(66, bottom.Top);
        }

        [TestMethod]
        public void Disabling_control_clears_focus_inputs_and_active_tooltip()
        {
            var screen = new TestScreen();
            var button = AttachButton(screen);
            button.ToolTip = "Disabled help";
            button.Focus();
            screen.ShowToolTip(button, button.ToolTip!, new PointF(8, 8));
            button.OnMouseEnter(new MouseEventArgs(new PointF(8, 8), new List<MouseButton>()));
            button.OnMouseDown(new MouseEventArgs(new PointF(8, 8), MouseButton.Left));
            button.OnTouchDown(new TouchEventArgs(new PointF(8, 8)));

            button.IsEnabled = false;

            Assert.IsNull(ScreenEngine.FocusedControl);
            Assert.IsFalse(screen.IsToolTipVisibleFor(button));
            Assert.AreEqual(HoverStates.NotHovering, button.CurrentHoverState);
            Assert.AreEqual(TouchStates.Released, button.CurrentTouchState);
            Assert.AreEqual(ButtonState.Released, button.LeftMouseButtonState);
        }

        [TestMethod]
        public void Interactive_controls_enable_focus_visuals_by_default()
        {
            Assert.IsTrue(new Button().ShowFocusVisual);
            Assert.IsTrue(new TextBox().ShowFocusVisual);
            Assert.IsNotNull(new Button().FocusBorderBrush);
            Assert.IsNotNull(new Button().DisabledOverlayBrush);
        }

        private static InspectableButton AttachButton(TestScreen screen)
        {
            var button = new InspectableButton
            {
                Text = "Action",
                Width = 160,
                Height = 32
            };
            screen.Content = button;
            button.UpdateLayout(new Rect(0, 0, 160, 32));
            return button;
        }

        private sealed class TestScreen : Screen
        {
        }

        private sealed class InspectableButton : Button
        {
            public HoverStates CurrentHoverState => HoverState;
            public TouchStates CurrentTouchState => TouchState;
            public ButtonState LeftMouseButtonState => MouseButtonStates[MouseButton.Left];
        }
    }
}

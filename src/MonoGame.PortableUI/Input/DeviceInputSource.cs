using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Input
{
    public sealed class DeviceInputSource : IInputSource
    {
        private static readonly MouseButton[] EmptyButtons = new MouseButton[0];

        public static DeviceInputSource Instance { get; } = new DeviceInputSource();

        private readonly List<MouseButton> _pressedButtons = new List<MouseButton>(3);

        private DeviceInputSource()
        {
        }

        public PointF MousePosition => (PointF)Mouse.GetState().Position;

        /// <summary>
        ///     The buttons held now, plus (on DesktopGL) those pressed since the previous frame: a click pressed and
        ///     released between two polls, as during a slow frame, then reads as pressed for one frame instead of
        ///     never.
        /// </summary>
        public IReadOnlyCollection<MouseButton> PressedMouseButtons
        {
            get
            {
                var mouseState = Mouse.GetState();
                TakeClicks();
                // Polled every frame; callers snapshot the contents immediately, so reuse one list.
                var buttons = _pressedButtons;
                buttons.Clear();
                if (mouseState.LeftButton == ButtonState.Pressed || _leftClicked)
                    buttons.Add(MouseButton.Left);
                if (mouseState.RightButton == ButtonState.Pressed || _rightClicked)
                    buttons.Add(MouseButton.Right);
                if (mouseState.MiddleButton == ButtonState.Pressed || _middleClicked)
                    buttons.Add(MouseButton.Middle);
                return buttons.Count == 0 ? EmptyButtons : buttons;
            }
        }

        private bool _leftClicked, _rightClicked, _middleClicked;
        private System.TimeSpan _clicksTime = System.TimeSpan.MinValue;

        /// <summary>
        ///     Takes the presses SDL counted since the last frame, once per frame (keyed by
        ///     <see cref="ScreenSystem.TotalTime"/>), so every screen and surface reading this frame sees them.
        /// </summary>
        private void TakeClicks()
        {
            var now = ScreenSystem.TotalTime;
            if (now == _clicksTime)
                return;
            _clicksTime = now;
#if !ANDROID
            if (SdlMouseClicks.TryInstall())
            {
                _leftClicked = SdlMouseClicks.Take(SdlMouseClicks.Left) > 0;
                _rightClicked = SdlMouseClicks.Take(SdlMouseClicks.Right) > 0;
                _middleClicked = SdlMouseClicks.Take(SdlMouseClicks.Middle) > 0;
            }
#endif
        }

        /// <summary>Forgets presses made while the window was inactive and nobody read the device.</summary>
        internal void DiscardClicks()
        {
            _leftClicked = _rightClicked = _middleClicked = false;
#if !ANDROID
            SdlMouseClicks.Discard();
#endif
        }

        public int ScrollWheelValue => Mouse.GetState().ScrollWheelValue;

        public int HorizontalScrollWheelValue => Mouse.GetState().HorizontalScrollWheelValue;

        private TouchCollection _touches;
        private System.TimeSpan _touchesTime = System.TimeSpan.MinValue;

        /// <summary>
        ///     The touch state for the current frame. <c>TouchPanel.GetState()</c> consumes
        ///     pressed events, so it is read once per frame (keyed by <see cref="ScreenSystem.TotalTime"/>)
        ///     and shared by every screen and surface that polls during that frame.
        /// </summary>
        public TouchCollection Touches
        {
            get
            {
                var now = ScreenSystem.TotalTime;
                if (now != _touchesTime)
                {
                    _touchesTime = now;
#if ANDROID
                    _touches = AndroidInputBridge.IsAttached ? AndroidInputBridge.ReadTouches() : TouchPanel.GetState();
#else
                    _touches = TouchPanel.GetState();
#endif
                }
                return _touches;
            }
        }

#if ANDROID
        public KeyboardState KeyboardState => AndroidInputBridge.IsAttached ? AndroidInputBridge.KeyboardState : Keyboard.GetState();
#else
        public KeyboardState KeyboardState => Keyboard.GetState();
#endif

        public GamePadState GamePad => Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
    }
}

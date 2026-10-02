#if ANDROID
using System;
using System.Collections.Generic;
using Android.Views;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     Thread-safe touch and key input for Android, read by <see cref="DeviceInputSource"/> instead of
    ///     MonoGame's <c>TouchPanel</c>/<c>Keyboard</c>. Events arrive on the UI thread and are queued
    ///     under a lock; the game thread drains them once per frame. Every event wakes an idle
    ///     <see cref="RenderMode.OnDemand"/> loop at once, so the idle interval can be long without
    ///     delaying the first touch.
    /// </summary>
    /// <remarks>
    ///     Required when the game loop runs off the UI thread (<c>AndroidGameActivity.RenderOnUIThread
    ///     = false</c>, the battery-friendly setup): MonoGame's touch and key lists are not synchronized
    ///     and its touch listener, set from the game thread, does not take effect. Call
    ///     <see cref="Attach"/> once with the game view, e.g. in <c>Game.Initialize</c>. Touches then no
    ///     longer reach <c>TouchPanel</c>.
    /// </remarks>
    public static class AndroidInputBridge
    {
        private static readonly TouchEventQueue Touches = new TouchEventQueue();
        private static readonly object KeyGate = new object();
        private static readonly HashSet<Keys> PressedKeys = new HashSet<Keys>();
        private static int _keysVersion;
        private static ScreenEngine? _engine;

        // Game thread only.
        private static KeyboardState _keyboardState;
        private static int _keyboardVersion = -1;

        /// <summary>Set by <see cref="AndroidOnScreenKeyboard"/>: it delivers Backspace/Delete/Enter as
        /// commands, so they must not also show up as held keys (they would act twice).</summary>
        internal static volatile bool EditingKeysRoutedAsCommands;

        /// <summary>Whether <see cref="Attach"/> was called; <see cref="DeviceInputSource"/> then reads from here.</summary>
        public static bool IsAttached { get; private set; }

        /// <summary>Takes over touch and key input of <paramref name="view"/>. Safe from any thread.</summary>
        public static void Attach(View view, ScreenEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            IsAttached = true;
            AndroidUiThread.Run(view, () =>
            {
                view.SetOnTouchListener(new TouchListener(view));
                view.KeyPress += OnKeyPress;
            });
        }

        /// <summary>This frame's touches (see <see cref="TouchEventQueue"/>). Game thread, once per frame.</summary>
        internal static TouchCollection ReadTouches() => Touches.Read();

        private sealed class TouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly View _view;

            public TouchListener(View view)
            {
                _view = view;
            }

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null)
                    return false;
                // View pixels -> back-buffer pixels (equal when AndroidSurfaceSize.Follow is used).
                var scaleX = _view.Width > 0 && TouchPanel.DisplayWidth > 0 ? TouchPanel.DisplayWidth / (float)_view.Width : 1f;
                var scaleY = _view.Height > 0 && TouchPanel.DisplayHeight > 0 ? TouchPanel.DisplayHeight / (float)_view.Height : 1f;
                switch (e.ActionMasked)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.PointerDown:
                        Add(e, e.ActionIndex, TouchEventQueue.Kind.Down, scaleX, scaleY);
                        break;
                    case MotionEventActions.Up:
                    case MotionEventActions.PointerUp:
                        Add(e, e.ActionIndex, TouchEventQueue.Kind.Up, scaleX, scaleY);
                        break;
                    case MotionEventActions.Move:
                        for (var i = 0; i < e.PointerCount; i++)
                            Add(e, i, TouchEventQueue.Kind.Move, scaleX, scaleY);
                        break;
                    case MotionEventActions.Cancel:
                        for (var i = 0; i < e.PointerCount; i++)
                            Add(e, i, TouchEventQueue.Kind.Up, scaleX, scaleY);
                        break;
                }
                _engine?.WakeUp();
                return true;
            }

            private static void Add(MotionEvent e, int index, TouchEventQueue.Kind kind, float scaleX, float scaleY)
                => Touches.Add(kind, e.GetPointerId(index), new Vector2(e.GetX(index) * scaleX, e.GetY(index) * scaleY));
        }

        private static void OnKeyPress(object? sender, View.KeyEventArgs e)
        {
            if (EditingKeysRoutedAsCommands && e.KeyCode is Keycode.Del or Keycode.ForwardDel or Keycode.Enter or Keycode.NumpadEnter)
                return; // the on-screen keyboard owns these (and their Handled flag)
            // Observe only: MonoGame and AndroidOnScreenKeyboard still see the event.
            e.Handled = false;
            if (e.Event is not { } keyEvent || !TryMapKey(e.KeyCode, out var key))
                return;
            lock (KeyGate)
            {
                var changed = keyEvent.Action switch
                {
                    KeyEventActions.Down => PressedKeys.Add(key),
                    KeyEventActions.Up => PressedKeys.Remove(key),
                    _ => false
                };
                if (changed)
                    _keysVersion++;
            }
            _engine?.WakeUp();
        }

        /// <summary>Keys held on a hardware or soft keyboard; rebuilt only when it changed. Game thread only.</summary>
        internal static KeyboardState KeyboardState
        {
            get
            {
                lock (KeyGate)
                {
                    if (_keyboardVersion != _keysVersion)
                    {
                        _keyboardVersion = _keysVersion;
                        var keys = new Keys[PressedKeys.Count];
                        PressedKeys.CopyTo(keys);
                        _keyboardState = new KeyboardState(keys);
                    }
                    return _keyboardState;
                }
            }
        }

        private static bool TryMapKey(Keycode code, out Keys key)
        {
            if (code >= Keycode.A && code <= Keycode.Z)
            {
                key = Keys.A + (code - Keycode.A);
                return true;
            }
            if (code >= Keycode.Num0 && code <= Keycode.Num9)
            {
                key = Keys.D0 + (code - Keycode.Num0);
                return true;
            }
            if (code >= Keycode.F1 && code <= Keycode.F12)
            {
                key = Keys.F1 + (code - Keycode.F1);
                return true;
            }
            key = code switch
            {
                Keycode.DpadUp => Keys.Up,
                Keycode.DpadDown => Keys.Down,
                Keycode.DpadLeft => Keys.Left,
                Keycode.DpadRight => Keys.Right,
                Keycode.DpadCenter => Keys.Enter,
                Keycode.Enter or Keycode.NumpadEnter => Keys.Enter,
                Keycode.Space => Keys.Space,
                Keycode.Tab => Keys.Tab,
                Keycode.Escape => Keys.Escape,
                Keycode.Del => Keys.Back,
                Keycode.ForwardDel => Keys.Delete,
                Keycode.MoveHome => Keys.Home,
                Keycode.MoveEnd => Keys.End,
                Keycode.PageUp => Keys.PageUp,
                Keycode.PageDown => Keys.PageDown,
                Keycode.Insert => Keys.Insert,
                Keycode.ShiftLeft => Keys.LeftShift,
                Keycode.ShiftRight => Keys.RightShift,
                Keycode.CtrlLeft => Keys.LeftControl,
                Keycode.CtrlRight => Keys.RightControl,
                Keycode.AltLeft => Keys.LeftAlt,
                Keycode.AltRight => Keys.RightAlt,
                _ => Keys.None
            };
            return key != Keys.None;
        }
    }
}
#endif

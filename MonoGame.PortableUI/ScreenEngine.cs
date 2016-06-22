using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI
{
    public class ScreenEngine
    {
        public Game Game { get; set; }
        private readonly ScreenComponent _component;
        private static Control _focusedControl;
        private readonly Dictionary<string, IKeyboard> _keyboards;
        internal IKeyboard CurrentKeyboard;

        private ScreenEngine(Game game)
        {
            Game = game;
            ScreenHistory = new Stack<Screen>();
            _component = new ScreenComponent(this, game);
            _keyboards = new Dictionary<string, IKeyboard>();
            ScaleFactor = 1;
            Component.Initialize();

        }

        public static float ScaleFactor { get; set; }

        public static Control FocusedControl
        {
            get { return _focusedControl; }
            set
            {
                var oldElement = _focusedControl;
                _focusedControl = value;
                oldElement?.OnLostFocus(new LostFocusEventArgs(_focusedControl));
                _focusedControl?.OnGotFocus(new GotFocusEventArgs(oldElement));
            }
        }

        public Rect ScreenRect { get; set; }

        internal ScreenComponent Component => _component;

        public static DrawableGameComponent ScreenComponent
        {
            get
            {
                if (Instance == null)
                    throw new TypeInitializationException("ScreenEngine", new ArgumentNullException());
                return Instance.Component;
            }
        }

        public static ScreenEngine Instance { get; private set; }

        public static ScreenEngine Initialize(Game game)
        {
            Instance = new ScreenEngine(game);
            return Instance;
        }

        public void RegisterKeyboard(IKeyboard keyboard, string inputScope = "default")
        {
            _keyboards.Add(inputScope, keyboard);
        }

        public void UnregisterKeyboard(string inputScope = "default")
        {
            _keyboards.Remove(inputScope);
        }

        internal void RequestKeyboard(string inputScope)
        {
            inputScope = inputScope ?? "default";
            CurrentKeyboard = _keyboards[inputScope];
            CurrentKeyboard.Control.UpdateLayout(new Rect(0, ScreenRect.Height - CurrentKeyboard.Height, ScreenRect.Width, CurrentKeyboard.Height));
            ActiveScreen.ShowKeyboard();
            CurrentKeyboard.OnKeyboardAppear();
        }

        internal void HideKeyboard()
        {
            if (CurrentKeyboard == null)
                return;
            ActiveScreen.HideKeyboard();
            CurrentKeyboard.OnKeyboardDisappear();
            CurrentKeyboard = null;
        }

        public void SetScreenSize(int width, int height)
        {
            ScreenRect = new Rect(width, height);
            ActiveScreen?.InvalidateLayout(true);
        }

        public Stack<Screen> ScreenHistory { get; }
        public Screen ActiveScreen => ScreenHistory.Count > 0 ? ScreenHistory.Peek() : null;

        public void NavigateToScreen<T>(T screen) where T : Screen
        {
            FocusedControl = null;
            screen.ScreenEngine = this;
            ScreenHistory.Push(screen);
        }

        public void NavigateBack()
        {
            FocusedControl = null;
            var screen = Instance.ScreenHistory.Pop();
            screen.ScreenEngine = null;
        }

        public void Update(GameTime gameTime)
        {
            ScreenSystem.TotalTime = gameTime.TotalGameTime;
            ActiveScreen.Update();
        }
    }
}
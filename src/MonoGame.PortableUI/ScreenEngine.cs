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
        private static Control _focusedControl;
        private readonly Dictionary<string, IKeyboard> _keyboards;

        //probably better if it's internal. making it public for a small hack
        public IKeyboard CurrentKeyboard;

        private ScreenEngine(Game game, ScreenEngineOptions options)
        {
            Game = game;
            Options = options;
            ScreenHistory = new Stack<Screen>();
            Component = new ScreenComponent(this, game);
            _keyboards = new Dictionary<string, IKeyboard>();
            ScaleFactor = 1;
            if (!game.Components.Contains(Component) && options.AddComponentToGame)
                game.Components.Add(Component);
        }

        public static float ScaleFactor { get; set; }
        public ScreenEngineOptions Options { get; }

        public static Control FocusedControl
        {
            get { return _focusedControl; }
            set
            {
                if (_focusedControl == value)
                    return;
                var oldElement = _focusedControl;
                _focusedControl = value;
                oldElement?.OnLostFocus(new LostFocusEventArgs(_focusedControl));
                _focusedControl?.OnGotFocus(new GotFocusEventArgs(oldElement));
            }
        }

        public Rect ScreenRect { get; set; }

        internal ScreenComponent Component { get; }

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
            return Initialize(game, new ScreenEngineOptions());
        }

        public static ScreenEngine Initialize(Game game, bool addComponent)
        {
            return Initialize(game, new ScreenEngineOptions { AddComponentToGame = addComponent });
        }

        public static ScreenEngine Initialize(Game game, ScreenEngineOptions options)
        {
            Instance = new ScreenEngine(game, options ?? new ScreenEngineOptions());
            return Instance;
        }

        public void RegisterKeyboard(IKeyboard keyboard, string inputScope = "default")
        {
            _keyboards[inputScope ?? "default"] = keyboard;
        }

        public void UnregisterKeyboard(string inputScope = "default")
        {
            if (_keyboards.ContainsKey(inputScope))
                _keyboards.Remove(inputScope);
        }

        //probably better if it's internal. making it public for a small hack
        public void RequestKeyboard(string inputScope)
        {
            inputScope = inputScope ?? "default";
            if (!_keyboards.TryGetValue(inputScope, out var keyboard))
                return;
            CurrentKeyboard = keyboard;
            CurrentKeyboard?.Control.UpdateLayout(new Rect(0, ScreenRect.Height - CurrentKeyboard.Height, ScreenRect.Width, CurrentKeyboard.Height));
            ActiveScreen?.ShowKeyboard();
            CurrentKeyboard?.OnKeyboardAppear();
        }

        //probably better if it's internal. making it public for a small hack
        public void HideKeyboard()
        {
            if (CurrentKeyboard == null)
                return;
            ActiveScreen?.HideKeyboard();
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
            if (ScreenHistory.Count == 0)
                return;
            FocusedControl = null;
            var screen = ScreenHistory.Pop();
            screen.ScreenEngine = null;
        }

        public void Update(GameTime gameTime)
        {
            ScreenSystem.TotalTime = gameTime.TotalGameTime;
            ActiveScreen?.Update();
        }
    }
}

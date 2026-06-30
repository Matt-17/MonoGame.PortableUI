using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public abstract class Screen : FrameworkElement
    {

        protected Dictionary<MouseButton, ButtonState> MouseButtonStates { get; } = new Dictionary<MouseButton, ButtonState>
        {
            {MouseButton.Left, ButtonState.Released},
            {MouseButton.Middle, ButtonState.Released},
            {MouseButton.Right, ButtonState.Released},
        };

        public override FrameworkElement Parent
        {
            get { return null; }
            internal set { }
        }

        private Grid _mainGrid;

        internal PointF LastMousePosition;
        internal PointF LastTouchPosition;
        internal int LastScrollWheelValue;
        private FlyOut _flyOut;
        private ContextMenu _activeContextMenu;
        private Keys[] _lastPressedKeys = Array.Empty<Keys>();

        protected Screen()
        {
            _mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition {Height = GridLength.Auto}
                }
            };
            _mainGrid.Parent = this;
        }

        public bool Initialized { get; set; }

        public Rect ScreenRect => ScreenEngine?.ScreenRect ?? Rect.Empty;

        protected internal ScreenEngine ScreenEngine { get; set; }

        public Control Content
        {
            get { return _mainGrid.Children.Count > 0 ? _mainGrid.Children[0] : null; }
            set
            {
                if (_mainGrid.Children.Count == 0)
                    _mainGrid.AddChild(value);
                else
                    _mainGrid.Children[0] = value;
                InvalidateLayout(true);
            }
        }

        private FlyOut FlyOut
        {
            get { return _flyOut; }
            set
            {
                if (_flyOut != null)
                {
                    _activeContextMenu?.OnClosing();
                    _flyOut.NotifyDismissing();
                    _flyOut.Parent = null;
                    _flyOut.Dispose();
                    _flyOut.NotifyDismissed();
                    _activeContextMenu?.OnClosed();
                    _activeContextMenu = null;
                }
                _flyOut = value;
                if (_flyOut != null)
                {
                    _flyOut.NotifyShowing();
                    _flyOut.Parent = this;
                    _flyOut.NotifyShown();
                }
            }
        }


        public override void InvalidateLayout(bool boundsChanged)
        {
            _mainGrid?.UpdateLayout(ScreenRect);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            yield return _mainGrid;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            if (BackgroundBrush != null)
            {
                spriteBatch.Begin();
                BackgroundBrush.Draw(spriteBatch, ScreenRect);
                spriteBatch.End();
            }
            spriteBatch.GraphicsDevice.ScissorRectangle = _mainGrid.BoundingRect;

            spriteBatch.Begin(SpriteSortMode.Immediate, rasterizerState: new RasterizerState { ScissorTestEnable = true, MultiSampleAntiAlias = true }, effect: ScreenEngine.Options.Effect);
            
            DrawControl(spriteBatch, _mainGrid);
            spriteBatch.End();

            if (FlyOut != null)
            {
                spriteBatch.Begin();
                DrawControl(spriteBatch, FlyOut);
                spriteBatch.End();
            }
        }

        internal void OnNavigationFrom(object sender)
        {
            var list = VisualTreeHelper.GetVisualTreeAsList(_mainGrid);
            foreach (var control in list)
            {
                control.ResetInputs();
            }
        }
        
        internal void CreateContextMenu(PointF position, ContextMenu content, bool optimizeForTouch)
        {
            content.OnOpening();
            _activeContextMenu = content;
            FlyOut = new FlyOut(position, content.ContextMenuType == ContextMenuTypes.OpenAndHold)
            {
                Content = content.CreateControl(this, optimizeForTouch)
            };
            FlyOut.UpdateLayout(ScreenRect);
            content.OnOpened();
        }

        private static void DrawControl(SpriteBatch spriteBatch, Control control)
        {
            if (!control.IsVisible || control.IsGone)
                return;

            //if (!Visible) return;

            // Controlgröße ermitteln
            //Rectangle controlArea = new Rectangle(AbsolutePosition, ActualSize);
            //Rectangle localRenderMask = controlArea.Intersection(renderMask);

            // Scissor-Filter aktivieren
            //batch.Begin(rasterizerState: new RasterizerState() { ScissorTestEnable = true }, samplerState: SamplerState.LinearWrap, transformMatrix: AbsoluteTransformation);
            //OnDraw(batch, controlArea, gameTime);
            //batch.End();


            //invalidDrawing = false;
            control.OnDraw(spriteBatch, control.ClippingRect);

            var oldRect = new Rect(spriteBatch.GraphicsDevice.ScissorRectangle);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect ^ control.ClippingRect;
            foreach (var c in control.GetDescendants())
            {
                DrawControl(spriteBatch, c);
            }
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
        }

        internal void Update()
        {
            if (!Initialized)
            {
                InvalidateLayout(true);
                Initialized = true;
            }

            var mouseState = Mouse.GetState();
            TouchLocation touchState = default(TouchLocation);
            var touchCollection = TouchPanel.GetState();
            var hasTouch = touchCollection.Count > 0;
            if (hasTouch)
            {
                touchState = touchCollection[0];
            }
            var touchPosition = hasTouch ? (PointF)touchState.Position.ToPoint() : LastTouchPosition;
            var mousePosition = (PointF)mouseState.Position;

            Control content;

            if (FlyOut != null)
                content = FlyOut;
            else
                content = _mainGrid;

            foreach (var control in VisualTreeHelper.GetVisualTreeAsList(content, false))
                control.UpdateTimers();

            HandleKeyboardInput();

            if (mousePosition != LastMousePosition)
            {
                List<MouseButton> buttons = new List<MouseButton>();
                if (mouseState.LeftButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Left);
                if (mouseState.RightButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Right);
                if (mouseState.MiddleButton == ButtonState.Pressed)
                    buttons.Add(MouseButton.Middle);
                var args = new MouseEventArgs(mousePosition, buttons);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position) && !c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseEnter(a); }, (c, a) => c.BoundingRect.Contains(a.Position));
                VisualTreeHelper.IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseMove(a); }, null);
                VisualTreeHelper.IterateVisualTree(content, args, (c, a) => !c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseLeave(a); }, (c, a) => c.BoundingRect.Contains(LastMousePosition));
                LastMousePosition = mousePosition;
            }

            HandleMouseButton(mouseState.LeftButton, ButtonState.Pressed, MouseButton.Left, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(mouseState.LeftButton, ButtonState.Released, MouseButton.Left, mousePosition, content, (c, a) => c.OnMouseUp(a));
            HandleMouseButton(mouseState.RightButton, ButtonState.Pressed, MouseButton.Right, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(mouseState.RightButton, ButtonState.Released, MouseButton.Right, mousePosition, content, (c, a) => c.OnMouseUp(a));
            HandleMouseButton(mouseState.MiddleButton, ButtonState.Pressed, MouseButton.Middle, mousePosition, content, (c, a) => c.OnMouseDown(a));
            HandleMouseButton(mouseState.MiddleButton, ButtonState.Released, MouseButton.Middle, mousePosition, content, (c, a) => c.OnMouseUp(a));
            if (mouseState.ScrollWheelValue != LastScrollWheelValue)
            {
                var args = new ScrollWheelChangedEventArgs(mousePosition, mouseState.ScrollWheelValue - LastScrollWheelValue);

                VisualTreeHelper.IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.Position), (c, a) => { c.OnScrollWheelChanged(a); }, null);

                LastScrollWheelValue = mouseState.ScrollWheelValue;
            }


            if (hasTouch && touchState.State == TouchLocationState.Pressed)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchDown(a); },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
            if (hasTouch && touchState.State == TouchLocationState.Released)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchUp(a); },
                    null
                    );
            }
            if (hasTouch && touchState.State == TouchLocationState.Moved && touchPosition != LastTouchPosition)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position) || c.BoundingRect.Contains(LastTouchPosition),
                    (c, a) =>
                    {
                        if (c.BoundingRect.Contains(a.Position))
                            c.OnTouchMove(a);
                        else
                            c.OnTouchCancel(a);
                    },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
        }

        private void HandleKeyboardInput()
        {
            var keyboardState = Keyboard.GetState();
            var pressedKeys = keyboardState.GetPressedKeys();
            var focusedControl = ScreenEngine.FocusedControl;

            if (focusedControl != null)
            {
                foreach (var key in pressedKeys)
                {
                    if (_lastPressedKeys.Contains(key))
                        continue;

                    var command = TryGetKeyboardCommand(key);
                    if (command.HasValue)
                    {
                        focusedControl.OnKeyPressed(command.Value);
                        continue;
                    }

                    var character = TryGetCharacter(key, keyboardState);
                    if (character.HasValue)
                        focusedControl.OnKeyPressed(character.Value);
                }
            }

            _lastPressedKeys = pressedKeys;
        }

        private static KeyboardCommand? TryGetKeyboardCommand(Keys key)
        {
            switch (key)
            {
                case Keys.Back:
                    return KeyboardCommand.Backspace;
                case Keys.Enter:
                    return KeyboardCommand.Enter;
                case Keys.Left:
                    return KeyboardCommand.CursorLeft;
                case Keys.Right:
                    return KeyboardCommand.CursorRight;
                case Keys.Up:
                    return KeyboardCommand.CursorUp;
                case Keys.Down:
                    return KeyboardCommand.CursorDown;
                default:
                    return null;
            }
        }

        private static char? TryGetCharacter(Keys key, KeyboardState keyboardState)
        {
            var shifted = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            var keyValue = (int)key;

            if (keyValue >= (int)Keys.A && keyValue <= (int)Keys.Z)
            {
                var letter = (char)('a' + keyValue - (int)Keys.A);
                return shifted ? char.ToUpperInvariant(letter) : letter;
            }

            if (keyValue >= (int)Keys.D0 && keyValue <= (int)Keys.D9)
            {
                const string normal = "0123456789";
                const string shiftedDigits = ")!@#$%^&*(";
                var index = keyValue - (int)Keys.D0;
                return shifted ? shiftedDigits[index] : normal[index];
            }

            if (keyValue >= (int)Keys.NumPad0 && keyValue <= (int)Keys.NumPad9)
                return (char)('0' + keyValue - (int)Keys.NumPad0);

            switch (key)
            {
                case Keys.Space:
                    return ' ';
                case Keys.Decimal:
                    return '.';
                case Keys.Add:
                    return '+';
                case Keys.Subtract:
                    return '-';
                case Keys.Multiply:
                    return '*';
                case Keys.Divide:
                    return '/';
                case Keys.OemComma:
                    return shifted ? '<' : ',';
                case Keys.OemPeriod:
                    return shifted ? '>' : '.';
                case Keys.OemMinus:
                    return shifted ? '_' : '-';
                case Keys.OemPlus:
                    return shifted ? '+' : '=';
                case Keys.OemQuestion:
                    return shifted ? '?' : '/';
                case Keys.OemSemicolon:
                    return shifted ? ':' : ';';
                case Keys.OemQuotes:
                    return shifted ? '"' : '\'';
                case Keys.OemOpenBrackets:
                    return shifted ? '{' : '[';
                case Keys.OemCloseBrackets:
                    return shifted ? '}' : ']';
                case Keys.OemPipe:
                    return shifted ? '|' : '\\';
                case Keys.OemTilde:
                    return shifted ? '~' : '`';
                default:
                    return null;
            }
        }

        private void HandleMouseButton(ButtonState buttonState, ButtonState newState, MouseButton button, PointF position, Control control, Action<Control, MouseEventArgs> action)
        {
            if (buttonState != newState || MouseButtonStates[button] == newState)
                return;
            MouseButtonStates[button] = newState;
            var args = new MouseEventArgs(position, button);
            VisualTreeHelper.IterateVisualTree(control, args,
                (c, a) => c.BoundingRect.Contains(a.Position),
                action,
                null
            );
        }

        public void ClearFlyOut()
        {
            FlyOut = null;
        }

        public void ShowKeyboard()
        {
            if (ScreenEngine.CurrentKeyboard != null && !_mainGrid.Children.Contains(ScreenEngine.CurrentKeyboard.Control))
                _mainGrid.AddChild(ScreenEngine.CurrentKeyboard.Control, 1);
            _mainGrid.InvalidateLayout(true);
        }

        public void HideKeyboard()
        {
            if (ScreenEngine.CurrentKeyboard == null)
                return;
            _mainGrid.Children.Remove(ScreenEngine.CurrentKeyboard.Control);
            _mainGrid.InvalidateLayout(true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Animation;
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

        public override FrameworkElement? Parent
        {
            get { return null; }
            internal set { }
        }

        private readonly Grid _mainGrid;

        internal PointF LastMousePosition;
        internal PointF LastTouchPosition;
        internal int LastScrollWheelValue;
        private FlyOut? _flyOut;
        private FlyOut? _dismissingFlyOut;
        private ToolTipPopup? _toolTip;
        private ToolTipPopup? _dismissingToolTip;
        private Control? _toolTipOwner;
        private string? _toolTipText;
        private PointF _toolTipAnchorPosition;
        private ContextMenu? _activeContextMenu;
        private ContextMenu? _dismissingContextMenu;
        private FlyOutAnimationStyle _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
        private Control? _capturedMouseControl;
        private Keys[] _lastPressedKeys = Array.Empty<Keys>();
        private static readonly ScreenEngineOptions DefaultOptions = new ScreenEngineOptions();
        private static readonly RasterizerState ScissorRasterizer = new RasterizerState { ScissorTestEnable = true, MultiSampleAntiAlias = true };

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

        protected internal ScreenEngine? ScreenEngine { get; set; }

        public Control? Content
        {
            get { return _mainGrid.Children.Count > 0 ? _mainGrid.Children[0] : null; }
            set
            {
                if (value == null)
                {
                    _mainGrid.Children.Clear();
                    InvalidateLayout(true);
                    return;
                }

                if (_mainGrid.Children.Count == 0)
                    _mainGrid.AddChild(value);
                else
                    _mainGrid.Children[0] = value;
                InvalidateLayout(true);
            }
        }

        private FlyOut? FlyOut
        {
            get { return _flyOut; }
            set
            {
                if (_flyOut != null)
                {
                    RemoveFlyOutNow(_flyOut, _activeContextMenu);
                    _activeContextMenu = null;
                    _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
                }
                if (_dismissingFlyOut != null)
                {
                    RemoveFlyOutNow(_dismissingFlyOut, _dismissingContextMenu);
                    _dismissingFlyOut = null;
                    _dismissingContextMenu = null;
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

            DrawControlTree(spriteBatch, _mainGrid, _mainGrid.BoundingRect);

            if (FlyOut != null)
                DrawControlTree(spriteBatch, FlyOut, GetOverlayScissor(FlyOut));
            if (_dismissingFlyOut != null)
                DrawControlTree(spriteBatch, _dismissingFlyOut, GetOverlayScissor(_dismissingFlyOut));

            if (_toolTip != null)
                DrawControlTree(spriteBatch, _toolTip, GetOverlayScissor(_toolTip));
            if (_dismissingToolTip != null)
                DrawControlTree(spriteBatch, _dismissingToolTip, GetOverlayScissor(_dismissingToolTip));
        }

        internal void OnNavigationFrom(object? sender)
        {
            var list = VisualTreeHelper.GetVisualTreeAsList(_mainGrid);
            foreach (var control in list)
            {
                control.ResetInputs();
            }
            _capturedMouseControl = null;
        }
        
        internal void CreateContextMenu(PointF position, ContextMenu content, bool optimizeForTouch)
        {
            ClearToolTip();
            content.OnOpening();
            FlyOut = new FlyOut(position, content.ContextMenuType == ContextMenuTypes.OpenAndHold)
            {
                Content = content.CreateControl(this, optimizeForTouch)
            };
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
            _activeContextMenu = content;
            FlyOut.UpdateLayout(ScreenRect);
            AnimateFlyOutIn(FlyOut, _activeFlyOutAnimationStyle);
            content.OnOpened();
        }

        internal void ShowFlyOut(PointF position, Control content, bool removeOnRelease)
        {
            ClearToolTip();
            FlyOut = new FlyOut(position, removeOnRelease)
            {
                Content = content
            };
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.DropDown;
            FlyOut.UpdateLayout(ScreenRect);
            AnimateFlyOutIn(FlyOut, _activeFlyOutAnimationStyle);
        }

        internal void ShowToolTip(Control owner, string text, PointF anchorPosition)
        {
            if (string.IsNullOrEmpty(text) || !owner.IsEnabled || !owner.IsVisible || owner.IsGone)
                return;

            if (_toolTipOwner != owner || _toolTipText != text)
                ClearToolTip();

            var created = _toolTip == null;
            if (created)
            {
                _dismissingToolTip = null;
                _toolTip = new ToolTipPopup(text);
                _toolTip.Parent = this;
                _toolTipOwner = owner;
                _toolTipText = text;
            }

            _toolTipAnchorPosition = anchorPosition;
            UpdateToolTipLayout();
            if (created)
                AnimatePopupIn(_toolTip!, 0.98, new Vector2(0, 4), TimeSpan.FromMilliseconds(100));
        }

        internal void UpdateToolTip(Control owner, PointF anchorPosition)
        {
            if (_toolTipOwner != owner || _toolTip == null)
                return;

            _toolTipAnchorPosition = anchorPosition;
            UpdateToolTipLayout();
        }

        internal void ClearToolTip(Control? owner = null)
        {
            if (owner != null && _toolTipOwner != owner)
                return;

            if (_toolTip != null)
            {
                var toolTip = _toolTip;
                toolTip.Parent = null;
                _dismissingToolTip = toolTip;
                AnimatePopupOut(toolTip, 0.98, new Vector2(0, 4), TimeSpan.FromMilliseconds(80), () =>
                {
                    if (_dismissingToolTip == toolTip)
                        _dismissingToolTip = null;
                });
            }
            _toolTip = null;
            _toolTipOwner = null;
            _toolTipText = null;
        }

        internal bool IsToolTipVisibleFor(Control owner)
        {
            return _toolTipOwner == owner && _toolTip != null;
        }

        internal Rect ToolTipRect => _toolTip?.BoundingRect ?? Rect.Empty;

        private void UpdateToolTipLayout()
        {
            if (_toolTip == null)
                return;

            var options = ScreenEngine?.Options ?? DefaultOptions;
            var size = _toolTip.MeasureLayout();
            var position = _toolTipAnchorPosition + options.ToolTipOffset;
            var preferredRect = new Rect(position, size);
            var layoutRect = ClampPopupRect(preferredRect, ScreenRect, options.ToolTipScreenPadding);
            _toolTip.UpdateLayout(layoutRect);
        }

        private void DrawControlTree(SpriteBatch spriteBatch, Control control, Rect scissorRect)
        {
            if (scissorRect.Width <= 0 || scissorRect.Height <= 0)
                scissorRect = GetOverlayScissor(control);

            spriteBatch.GraphicsDevice.ScissorRectangle = ToScissorRectangle(scissorRect);
            spriteBatch.Begin(SpriteSortMode.Immediate, rasterizerState: ScissorRasterizer, effect: ScreenEngine?.Options.Effect);
            DrawControl(spriteBatch, control, RenderContext.Root(scissorRect));
            spriteBatch.End();
        }

        private Rect GetOverlayScissor(Control control)
        {
            if (ScreenRect.Width > 0 && ScreenRect.Height > 0)
                return ScreenRect;

            if (control.ClippingRect.Width > 0 && control.ClippingRect.Height > 0)
                return control.ClippingRect;

            return control.BoundingRect;
        }

        private static void DrawControl(SpriteBatch spriteBatch, Control control, RenderContext parentContext)
        {
            if (!control.IsVisible || control.IsGone)
                return;

            var context = parentContext.ForControl(control);
            if (context.ScissorRect.Width <= 0 || context.ScissorRect.Height <= 0)
                return;

            var oldRect = new Rect(spriteBatch.GraphicsDevice.ScissorRectangle);
            control.SetRenderState(context.Opacity, context.Scale);
            control.OnDraw(spriteBatch, context.RenderRect);
            spriteBatch.GraphicsDevice.ScissorRectangle = ToScissorRectangle(context.ScissorRect);
            foreach (var c in control.GetDescendants())
            {
                DrawControl(spriteBatch, c, context);
            }
            control.OnDrawOverlay(spriteBatch, context.RenderRect);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
        }

        private static Rectangle ToScissorRectangle(Rect rect)
        {
            var left = (int)Math.Floor(rect.Left);
            var top = (int)Math.Floor(rect.Top);
            var right = (int)Math.Ceiling(rect.Right);
            var bottom = (int)Math.Ceiling(rect.Bottom);
            return new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private readonly struct RenderContext
        {
            private readonly Matrix _transform;

            private RenderContext(Matrix transform, Vector2 scale, float opacity, Rect scissorRect, Rect renderRect)
            {
                _transform = transform;
                Scale = scale;
                Opacity = opacity;
                ScissorRect = scissorRect;
                RenderRect = renderRect;
            }

            public Vector2 Scale { get; }
            public float Opacity { get; }
            public Rect ScissorRect { get; }
            public Rect RenderRect { get; }

            public static RenderContext Root(Rect scissorRect)
            {
                return new RenderContext(Matrix.Identity, Vector2.One, 1, scissorRect, scissorRect);
            }

            public RenderContext ForControl(Control control)
            {
                var transform = CreateControlTransform(control) * _transform;
                var renderRect = TransformRect(control.ClippingRect, transform);
                var scissorRect = ScissorRect ^ renderRect;
                var scale = new Vector2(Scale.X * control.Scale.X, Scale.Y * control.Scale.Y);
                var opacity = Opacity * MathHelper.Clamp((float)control.Opacity, 0, 1);
                return new RenderContext(transform, scale, opacity, scissorRect, renderRect);
            }

            private static Matrix CreateControlTransform(Control control)
            {
                if (control.Scale == Vector2.One && control.Translation == Vector2.Zero)
                    return Matrix.Identity;

                var rect = control.ClippingRect;
                var origin = new Vector2(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                return Matrix.CreateTranslation(-origin.X, -origin.Y, 0)
                    * Matrix.CreateScale(control.Scale.X, control.Scale.Y, 1)
                    * Matrix.CreateTranslation(origin.X + control.Translation.X, origin.Y + control.Translation.Y, 0);
            }

            private static Rect TransformRect(Rect rect, Matrix transform)
            {
                var topLeft = Vector2.Transform(new Vector2(rect.Left, rect.Top), transform);
                var topRight = Vector2.Transform(new Vector2(rect.Right, rect.Top), transform);
                var bottomLeft = Vector2.Transform(new Vector2(rect.Left, rect.Bottom), transform);
                var bottomRight = Vector2.Transform(new Vector2(rect.Right, rect.Bottom), transform);

                var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
                var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
                var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
                var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));
                return new Rect(left, top, right - left, bottom - top);
            }
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
            if (_toolTip != null)
            {
                foreach (var control in VisualTreeHelper.GetVisualTreeAsList(_toolTip, false))
                    control.UpdateTimers();
            }
            if (_dismissingFlyOut != null)
            {
                foreach (var control in VisualTreeHelper.GetVisualTreeAsList(_dismissingFlyOut, false))
                    control.UpdateTimers();
            }
            if (_dismissingToolTip != null)
            {
                foreach (var control in VisualTreeHelper.GetVisualTreeAsList(_dismissingToolTip, false))
                    control.UpdateTimers();
            }

            HandleKeyboardInput();

            if (mousePosition != LastMousePosition)
            {
                var buttons = GetPressedMouseButtons(mouseState);
                if (!RouteCapturedMouseMove(mousePosition, buttons))
                {
                    var args = new MouseEventArgs(mousePosition, buttons);
                    VisualTreeHelper.IterateVisualTree(content, args,
                        (c, a) => c.BoundingRect.Contains(a.Position) && !c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseEnter(a); }, (c, a) => c.BoundingRect.Contains(a.Position));
                    VisualTreeHelper.IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseMove(a); }, null);
                    VisualTreeHelper.IterateVisualTree(content, args, (c, a) => !c.BoundingRect.Contains(a.Position) && c.BoundingRect.Contains(LastMousePosition), (c, a) => { c.OnMouseLeave(a); }, (c, a) => c.BoundingRect.Contains(LastMousePosition));
                }
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
            var modifiers = GetKeyboardModifiers(keyboardState);

            if (focusedControl != null)
            {
                foreach (var key in pressedKeys)
                {
                    if (_lastPressedKeys.Contains(key))
                        continue;

                    var command = TryGetKeyboardCommand(key, modifiers);
                    if (command.HasValue)
                    {
                        focusedControl.OnKeyPressed(command.Value, modifiers);
                        continue;
                    }

                    if ((modifiers & (KeyboardModifiers.Control | KeyboardModifiers.Alt)) != KeyboardModifiers.None)
                        continue;

                    var character = TryGetCharacter(key, keyboardState);
                    if (character.HasValue)
                        focusedControl.OnKeyPressed(character.Value, modifiers);
                }
            }

            _lastPressedKeys = pressedKeys;
        }

        private static KeyboardModifiers GetKeyboardModifiers(KeyboardState keyboardState)
        {
            var modifiers = KeyboardModifiers.None;
            if (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                modifiers |= KeyboardModifiers.Shift;
            if (keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl))
                modifiers |= KeyboardModifiers.Control;
            if (keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                modifiers |= KeyboardModifiers.Alt;
            return modifiers;
        }

        private static KeyboardCommand? TryGetKeyboardCommand(Keys key, KeyboardModifiers modifiers)
        {
            if ((modifiers & KeyboardModifiers.Control) != 0)
            {
                switch (key)
                {
                    case Keys.A:
                        return KeyboardCommand.SelectAll;
                    case Keys.C:
                        return KeyboardCommand.Copy;
                    case Keys.X:
                        return KeyboardCommand.Cut;
                    case Keys.V:
                        return KeyboardCommand.Paste;
                }
            }

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
                case Keys.Delete:
                    return KeyboardCommand.Delete;
                case Keys.Home:
                    return KeyboardCommand.Home;
                case Keys.End:
                    return KeyboardCommand.End;
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
            if (RouteCapturedMouseInput(args, action))
                return;

            VisualTreeHelper.IterateVisualTree(control, args,
                (c, a) => c.BoundingRect.Contains(a.Position),
                action,
                null
            );
        }

        internal Control? CapturedMouseControl => _capturedMouseControl;

        internal void CaptureMouse(Control control)
        {
            if (control.Screen != this)
                return;

            _capturedMouseControl = control;
        }

        internal void ReleaseMouse(Control control)
        {
            if (_capturedMouseControl == control)
                _capturedMouseControl = null;
        }

        internal bool RouteCapturedMouseMove(PointF position, List<MouseButton> buttons)
        {
            var args = new MouseEventArgs(position, buttons);
            return RouteCapturedMouseInput(args, (control, eventArgs) => control.OnMouseMove(eventArgs));
        }

        internal bool RouteCapturedMouseUp(PointF position, MouseButton button)
        {
            var args = new MouseEventArgs(position, button);
            return RouteCapturedMouseInput(args, (control, eventArgs) => control.OnMouseUp(eventArgs));
        }

        private bool RouteCapturedMouseInput(MouseEventArgs args, Action<Control, MouseEventArgs> action)
        {
            var captured = _capturedMouseControl;
            if (captured == null)
                return false;

            if (captured.IsGone || !captured.IsVisible || !captured.IsEnabled)
            {
                _capturedMouseControl = null;
                return false;
            }

            action(captured, args);
            return true;
        }

        private static List<MouseButton> GetPressedMouseButtons(MouseState mouseState)
        {
            var buttons = new List<MouseButton>();
            if (mouseState.LeftButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Left);
            if (mouseState.RightButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Right);
            if (mouseState.MiddleButton == ButtonState.Pressed)
                buttons.Add(MouseButton.Middle);
            return buttons;
        }

        public void ClearFlyOut()
        {
            if (_flyOut == null)
                return;

            var flyOut = _flyOut;
            var contextMenu = _activeContextMenu;
            var animationStyle = _activeFlyOutAnimationStyle;
            _flyOut = null;
            _activeContextMenu = null;
            _activeFlyOutAnimationStyle = FlyOutAnimationStyle.Popup;
            _dismissingFlyOut = flyOut;
            _dismissingContextMenu = contextMenu;
            contextMenu?.OnClosing();
            flyOut.NotifyDismissing();
            AnimateFlyOutOut(flyOut, animationStyle, () =>
            {
                if (_dismissingFlyOut != flyOut)
                    return;

                flyOut.Parent = null;
                flyOut.Dispose();
                flyOut.NotifyDismissed();
                contextMenu?.OnClosed();
                _dismissingFlyOut = null;
                if (_dismissingContextMenu == contextMenu)
                    _dismissingContextMenu = null;
            });
        }

        internal Control? FlyOutContent => FlyOut?.Content;

        private enum FlyOutAnimationStyle
        {
            Popup,
            DropDown
        }

        private static void AnimateFlyOutIn(FlyOut flyOut, FlyOutAnimationStyle style)
        {
            var animatedControl = flyOut.Content ?? flyOut;
            if (style == FlyOutAnimationStyle.DropDown)
                AnimateDropDownIn(animatedControl, 0.96f, TimeSpan.FromMilliseconds(120));
            else
                AnimatePopupIn(animatedControl, 0.96, new Vector2(0, 6), TimeSpan.FromMilliseconds(120));
        }

        private static void AnimateFlyOutOut(FlyOut flyOut, FlyOutAnimationStyle style, Action completed)
        {
            var animatedControl = flyOut.Content ?? flyOut;
            if (style == FlyOutAnimationStyle.DropDown)
                AnimateDropDownOut(animatedControl, 0.96f, TimeSpan.FromMilliseconds(90), completed);
            else
                AnimatePopupOut(animatedControl, 0.96, new Vector2(0, 6), TimeSpan.FromMilliseconds(90), completed);
        }

        private static void AnimatePopupIn(Control control, double startScale, Vector2 startTranslation, TimeSpan duration)
        {
            control.Opacity = 0;
            control.Scale = new Vector2((float)startScale, (float)startScale);
            control.Translation = startTranslation;
            control.Animate()
                .FadeTo(1)
                .Scale(1)
                .TranslateTo(Vector2.Zero)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private static void AnimateDropDownIn(Control control, float startScaleY, TimeSpan duration)
        {
            control.Opacity = 0;
            control.Scale = new Vector2(1, startScaleY);
            control.Translation = GetTopAnchoredScaleTranslation(control, startScaleY);
            control.Animate()
                .FadeTo(1)
                .Scale(Vector2.One)
                .TranslateTo(Vector2.Zero)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .Start();
        }

        private static void AnimateDropDownOut(Control control, float targetScaleY, TimeSpan duration, Action completed)
        {
            control.Animate()
                .FadeTo(0)
                .Scale(new Vector2(1, targetScaleY))
                .TranslateTo(GetTopAnchoredScaleTranslation(control, targetScaleY))
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .OnCompleted(completed)
                .Start();
        }

        private static Vector2 GetTopAnchoredScaleTranslation(Control control, float scaleY)
        {
            var height = control.ClippingRect.Height > 0 ? control.ClippingRect.Height : control.BoundingRect.Height;
            return new Vector2(0, -height * (1 - scaleY) / 2);
        }

        private static void AnimatePopupOut(Control control, double targetScale, Vector2 targetTranslation, TimeSpan duration, Action completed)
        {
            control.Animate()
                .FadeTo(0)
                .Scale(targetScale)
                .TranslateTo(targetTranslation)
                .Duration(duration)
                .Ease(Easings.CubicOut)
                .OnCompleted(completed)
                .Start();
        }

        private static void RemoveFlyOutNow(FlyOut flyOut, ContextMenu? contextMenu)
        {
            contextMenu?.OnClosing();
            flyOut.NotifyDismissing();
            flyOut.Parent = null;
            flyOut.Dispose();
            flyOut.NotifyDismissed();
            contextMenu?.OnClosed();
        }

        internal static Rect ClampPopupRect(Rect preferredRect, Rect screenRect, float padding)
        {
            if (screenRect.Width <= 0 || screenRect.Height <= 0)
                return preferredRect;

            var result = preferredRect;
            var availableWidth = Math.Max(0, screenRect.Width - padding * 2);
            var availableHeight = Math.Max(0, screenRect.Height - padding * 2);

            if (availableWidth > 0 && result.Width > availableWidth)
                result.Width = availableWidth;
            if (availableHeight > 0 && result.Height > availableHeight)
                result.Height = availableHeight;

            var minLeft = screenRect.Left + padding;
            var maxLeft = screenRect.Right - padding - result.Width;
            result.Left = maxLeft < minLeft ? minLeft : Math.Max(minLeft, Math.Min(result.Left, maxLeft));

            var minTop = screenRect.Top + padding;
            var maxTop = screenRect.Bottom - padding - result.Height;
            result.Top = maxTop < minTop ? minTop : Math.Max(minTop, Math.Min(result.Top, maxTop));

            return result;
        }

        public void ShowKeyboard()
        {
            var currentKeyboard = ScreenEngine?.CurrentKeyboard;
            if (currentKeyboard != null && !_mainGrid.Children.Contains(currentKeyboard.Control))
                _mainGrid.AddChild(currentKeyboard.Control, 1);
            _mainGrid.InvalidateLayout(true);
        }

        public void HideKeyboard()
        {
            var currentKeyboard = ScreenEngine?.CurrentKeyboard;
            if (currentKeyboard == null)
                return;
            _mainGrid.Children.Remove(currentKeyboard.Control);
            _mainGrid.InvalidateLayout(true);
        }
    }
}

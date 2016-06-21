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
            get { return _mainGrid.Children[0]; }
            set
            {
                //if (_mainGrid != null)
                //    _mainGrid.Parent = null;
                if (_mainGrid.Children.Count == 0)
                    _mainGrid.AddChild(value);
                else
                    _mainGrid.Children[0] = value;
                //if (_mainGrid != null)
                //    _mainGrid.Parent = this;
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
                    _flyOut.Parent = null;
                    _flyOut.Dispose();
                }
                _flyOut = value;
                if (_flyOut != null)
                    _flyOut.Parent = this;
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

            spriteBatch.Begin(SpriteSortMode.Immediate, rasterizerState: new RasterizerState { ScissorTestEnable = true });
            
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
            FlyOut = new FlyOut(position, content.ContextMenuType == ContextMenuTypes.OpenAndHold)
            {
                Content = content.CreateControl(this, optimizeForTouch)
            };
            FlyOut.UpdateLayout(ScreenRect);
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
            TouchLocation touchState;
            var touchCollection = TouchPanel.GetState();
            if (touchCollection.Count > 0)
            {
                touchState = touchCollection[0];
            }
            var touchPosition = (PointF)touchState.Position.ToPoint();
            var mousePosition = (PointF)mouseState.Position;

            Control content;

            if (FlyOut != null)
                content = FlyOut;
            else
                content = _mainGrid;

            //var content = FlyOut ?? Content;
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


            if (touchState.State == TouchLocationState.Pressed)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchDown(a); },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
            if (touchState.State == TouchLocationState.Released)
            {
                var args = new TouchEventArgs(touchPosition);
                VisualTreeHelper.IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchUp(a); },
                    null
                    );
            }
            if (touchState.State == TouchLocationState.Moved && touchPosition != LastTouchPosition)
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
            _mainGrid.AddChild(ScreenEngine.CurrentKeyboard.Control, 1);
            _mainGrid.InvalidateLayout(true);
        }

        public void HideKeyboard()
        {
            _mainGrid.Children.Remove(ScreenEngine.CurrentKeyboard.Control);
            _mainGrid.InvalidateLayout(true);
        }
    }
}
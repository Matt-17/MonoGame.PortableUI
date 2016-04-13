using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Control
    {
        public static int WrapContent = -1;
        
        protected Control()
        {
            SnapToPixel = true;
            Opacity = 1;
            IsEnabled = true;
            IsVisible = true;
            Parent = null;
            Scale = new Vector2(1, 1);
            Translation = new Vector2();
            Margin = new Thickness(0);
            Width = WrapContent;
            Height = WrapContent;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }


        #region Events

        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        public event EventHandler MouseMove;
        public event EventHandler MouseLeftDown;
        public event EventHandler MouseRightDown;
        public event EventHandler MouseLeftUp;
        public event EventHandler MouseRightUp;
        public event EventHandler TouchDown;
        public event EventHandler TouchUp;
        public event EventHandler TouchMove;
        public event EventHandler TouchCancel;

        #endregion

        public Control Parent

        {
            get { return _parent; }
            set {
                if (_parent != null && value != null)
                        throw new MultipleParentException();
                _parent = value; }
        }

        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public float MeasuredHeight
        {
            get { return (float) (Height + Margin.Top + Margin.Bottom); }
        }

        public float MeasuredWidth
        {
            get { return (float)(Width + Margin.Left + Margin.Right); }
        }

        public Thickness Margin { get; set; }

        public Rect Rect { get; set; }

        public Vector2 Scale { get; set; }
        public Vector2 Translation { get; set; }

        public Color BackgroundColor { get; set; }

        public double Opacity { get; set; }
        public bool IsVisible { get; set; }
        public bool IsEnabled { get; set; }

        public void Update(TimeSpan elapsed)
        {
            OnUpdate(elapsed, boundingRect);
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }

        private Point? _lastMousePosition;
        private bool _lastMouseButtonState;
        private Point? _lastTouchPosition;
        private bool _ignoreTouch;
        private bool _lastMouseButtonRightState;
        private Control _parent;

        private void HandleMouse(Rectangle rect)
        {
            var mouseState = Mouse.GetState();
            var position = mouseState.Position;

            if (rect.Contains(position))
            {
                if (_lastMousePosition == null)
                {
                    OnMouseEnter();
                    if (mouseState.LeftButton == ButtonState.Pressed)
                        _lastMouseButtonState = true;
                    _lastMousePosition = position;
                }

                if (_lastMousePosition != position)
                {
                    OnMouseMove();
                    _lastMousePosition = position;
                }

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!_lastMouseButtonState)
                    {
                        OnMouseLeftDown();
                        _lastMouseButtonState = true;
                    }
                }
                else
                {
                    if (_lastMouseButtonState)
                    {
                        OnMouseLeftUp();
                        _lastMouseButtonState = false;
                    }
                }
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (!_lastMouseButtonRightState)
                    {
                        OnMouseRightDown();
                        _lastMouseButtonRightState = true;
                    }
                }
                else
                {
                    if (_lastMouseButtonRightState)
                    {
                        OnMouseRightUp();
                        _lastMouseButtonRightState = false;
                    }
                }
            }
            else if (_lastMousePosition != null)
            {
                OnMouseLeave();
                _lastMousePosition = null;
            }
        }

        protected internal virtual void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            HandleMouse(rect);
            HandleTouch(rect);
        }

        private void HandleTouch(Rectangle rect)
        {
            var collection = TouchPanel.GetState();
            if (!_ignoreTouch && collection.Count == 1)
            {
                var touch = collection[0];
                var position = touch.Position.ToPoint();
                if (rect.Contains(position))
                {
                    if (_lastTouchPosition == null)
                    {
                        OnTouchDown();
                        _lastTouchPosition = position;
                    }
                    else
                    {
                        if (_lastTouchPosition != position)
                        {
                            OnTouchMove();
                            _lastTouchPosition = position;
                        }
                    }
                }
                else
                {
                    OnTouchCancel();
                    _ignoreTouch = true;
                    _lastTouchPosition = null;
                }
            }
            else if (collection.Count == 0)
            {
                _ignoreTouch = false;
                if (_lastTouchPosition != null)
                {
                    OnTouchUp();
                    _lastTouchPosition = null;
                }
            }
        }

        Rectangle boundingRect => new Rectangle((int)Left, (int)Top, (int)Width, (int)Height);
        public bool SnapToPixel { get; set; }

        public void Draw(SpriteBatch spriteBatch)
        {
            OnDraw(spriteBatch, boundingRect);
        }

        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
        }

        #region Event handlers
        protected virtual void OnMouseEnter()
        {
            MouseEnter?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMouseLeave()
        {
            MouseLeave?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMouseLeftDown()
        {
            MouseLeftDown?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMouseLeftUp()
        {
            MouseLeftUp?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnMouseRightDown()
        {
            MouseRightDown?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMouseRightUp()
        {
            MouseRightUp?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTouchDown()
        {
            TouchDown?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTouchUp()
        {
            TouchUp?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTouchMove()
        {
            TouchMove?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMouseMove()
        {
            MouseMove?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTouchCancel()
        {
            TouchCancel?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        public enum ControlAlignment
        {
            Left,
            Center,
            Right,
            Stretch
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Exceptions;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Control : IUIElement
    {
        public static int Auto = -1;
        private bool _ignoreTouch;
        private Point? _lastTouchPosition;
        private Control _parent;


        internal bool LastMouseRightButtonState;
        internal bool LastMouseLeftButtonState;
        internal Point? LastMousePosition;

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
            Width = Auto;
            Height = Auto;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Position = new Point(0, 0);
        }

        public Control Parent
        {
            get { return _parent; }
            internal set
            {
                if (_parent != null && value != null)
                    throw new MultipleParentException();
                _parent = value;
            }
        }

        //public Size ClientRect { get; set; }

        public float Width { get; set; }
        public float Height { get; set; }

        //public float RenderedWidth => (Width + Margin.Left + Margin.Right) * ScreenEngine.ScaleFactor;

        //public float RenderedHeight => (Height + Margin.Top + Margin.Bottom) * ScreenEngine.ScaleFactor;

        public Rect BoundingRect { get; private set; }

        public float MeasuredHeight
        {
            get { return (Height + Margin.Top + Margin.Bottom); }
        }

        public float MeasuredWidth
        {
            get { return (Width + Margin.Left + Margin.Right); }
        }

        public Thickness Margin { get; set; }

        //public Border Rect { get; set; }

        public Vector2 Scale { get; set; }

        public Vector2 Translation { get; set; }

        public Color BackgroundColor { get; set; }

        public double Opacity { get; set; }

        public bool IsVisible { get; set; }

        public bool IsEnabled { get; set; }

        public HorizontalAlignment HorizontalAlignment { get; set; }

        public VerticalAlignment VerticalAlignment { get; set; }

        public bool SnapToPixel { get; set; }

        private Rect RenderedBoundingRect => new Rect(RenderedPosition.X, RenderedPosition.Y, BoundingRect.Width * ScreenEngine.ScaleFactor, BoundingRect.Height * ScreenEngine.ScaleFactor);

        internal Point RenderedPosition
        {
            get { return new Point((int)((Position.X + Margin.Left) * ScreenEngine.ScaleFactor), (int)((Position.Y + Margin.Top) * ScreenEngine.ScaleFactor)); }
        }

        internal Point Position { get; set; }

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

        public void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            //if (!Visible) return;

            // Controlgröße ermitteln
            //Rectangle controlArea = new Rectangle(AbsolutePosition, ActualSize);
            //Rectangle localRenderMask = controlArea.Intersection(renderMask);

            // Scissor-Filter aktivieren
            //batch.GraphicsDevice.ScissorRectangle = localRenderMask.Transform(AbsoluteTransformation);
            //batch.Begin(rasterizerState: new RasterizerState() { ScissorTestEnable = true }, samplerState: SamplerState.LinearWrap, transformMatrix: AbsoluteTransformation);
            //OnDraw(batch, controlArea, gameTime);
            //batch.End();

            OnBeforeDraw(spriteBatch, rect);
            spriteBatch.Begin(/* Scissor und sowas ; Matrix */);
            OnDraw(spriteBatch, rect);
            spriteBatch.End();
            OnAfterDraw(spriteBatch, rect);

            //invalidDrawing = false;
        }

        protected internal virtual void OnBeforeDraw(SpriteBatch spriteBatch, Rect renderedBoundingRect) { }
        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rect rect) { }
        protected internal virtual void OnAfterDraw(SpriteBatch spriteBatch, Rect renderedBoundingRect) { }

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

        #region Event handlers

        protected internal virtual void OnMouseEnter()
        {
            MouseEnter?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseLeave()
        {
            MouseLeave?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseLeftDown()
        {
            MouseLeftDown?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseLeftUp()
        {
            MouseLeftUp?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseRightDown()
        {
            MouseRightDown?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseRightUp()
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

        protected internal virtual void OnMouseMove()
        {
            MouseMove?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnTouchCancel()
        {
            TouchCancel?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        public virtual void UpdateLayout(Rect boundingRect)
        {
            BoundingRect = boundingRect;
        }

        public virtual void InvalidateLayout(bool boundsChanged)
        {
            Parent.InvalidateLayout(boundsChanged);
        }

        public virtual Size MeasureLayout(Size availableSize)
        {
            
            return new Size(Math.Max(0, Width), Math.Max(0, Height)) + Margin;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
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
        private IUIElement _parent;


        internal bool LastMouseRightButtonState;
        internal bool LastMouseLeftButtonState;
        internal PointF? LastMousePosition;
        internal bool IgnoreTouch;
        internal PointF? LastTouchPosition;

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
            Width = Size.Auto;
            Height = Size.Auto;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            Position = new PointF(0, 0);
        }

        public IUIElement Parent
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

        internal PointF RenderedPosition
        {
            get { return new PointF((int)((Position.X + Margin.Left) * ScreenEngine.ScaleFactor), (int)((Position.Y + Margin.Top) * ScreenEngine.ScaleFactor)); }
        }

        internal PointF Position { get; set; }


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

        protected internal virtual void OnTouchDown()
        {
            TouchDown?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnTouchUp()
        {
            TouchUp?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnTouchMove()
        {
            TouchMove?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnMouseMove()
        {
            MouseMove?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnTouchCancel()
        {
            TouchCancel?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        internal Rect ClientRect { get; set; }

        public virtual void UpdateLayout(Rect boundingRect)
        {
            BoundingRect = MeasureLayout((Size)boundingRect);
        }

        public virtual void InvalidateLayout(bool boundsChanged)
        {
            Parent?.InvalidateLayout(boundsChanged);
        }

        public virtual IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        public virtual Size MeasureLayout(Size availableSize)
        {
            availableSize -= Margin;
            var width = Width;
            var height = Height;
            if (width.IsAuto())
                width = HorizontalAlignment == HorizontalAlignment.Stretch ? availableSize.Width : 0;

            if (height.IsAuto())
                height = VerticalAlignment == VerticalAlignment.Stretch ? availableSize.Height : 0;

            return new Size(width, height) + Margin;
        }
    }
}
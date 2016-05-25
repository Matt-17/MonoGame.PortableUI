using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Exceptions;
using MonoGame.PortableUI.Media;

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
        private float _width;
        private float _height;
        private bool _isVisible;
        private bool _isGone;

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

        public float Width
        {
            get { return _width; }
            set
            {
                if (Math.Abs(_width - value) < float.Epsilon)
                    return;
                _width = value;
                InvalidateLayout(true);
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                if (Math.Abs(_height - value) < float.Epsilon)
                    return;
                _height = value;
                InvalidateLayout(true);
            }
        }

        //public float RenderedWidth => (Width + Margin.Left + Margin.Right) * ScreenEngine.ScaleFactor;

        //public float RenderedHeight => (Height + Margin.Top + Margin.Bottom) * ScreenEngine.ScaleFactor;

        public Rect BoundingRect { get; protected set; }

        public Thickness Margin { get; set; }

        //public Border Rect { get; set; }

        public Vector2 Scale { get; set; }

        public Vector2 Translation { get; set; }

        public Brush BackgroundBrush { get; set; }

        public double Opacity { get; set; }

        //TODO invalidate layout after visibility changed
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                InvalidateLayout(false);
            }
        }

        //TODO invalidate layout after isGone changed
        public bool IsGone
        {
            get { return _isGone; }
            set
            {
                _isGone = value;
                InvalidateLayout(true);
            }                        
        }

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

        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            BackgroundBrush?.Draw(spriteBatch, rect - Margin);
        }

        #region Events

        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        public event MouseMoveEventHandler MouseMove;
        public event MouseButtonEventHandler MouseLeftDown;
        public event MouseButtonEventHandler MouseRightDown;
        public event MouseButtonEventHandler MouseLeftUp;
        public event MouseButtonEventHandler MouseRightUp;
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

        protected internal virtual void OnMouseLeftDown(Point position)
        {
            MouseLeftDown?.Invoke(this, new MouseButtonEventHandlerArgs(position));
        }

        protected internal virtual void OnMouseLeftUp(Point position)
        {
            MouseLeftUp?.Invoke(this, new MouseButtonEventHandlerArgs(position));
        }

        protected internal virtual void OnMouseRightDown(Point position)
        {
            MouseRightDown?.Invoke(this, new MouseButtonEventHandlerArgs(position));
        }

        protected internal virtual void OnMouseRightUp(Point position)
        {
            MouseRightUp?.Invoke(this, new MouseButtonEventHandlerArgs(position));
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

        protected internal virtual void OnMouseMove(Point point)
        {
            MouseMove?.Invoke(this, new MouseMoveEventHandlerArgs(point));
        }

        protected internal virtual void OnTouchCancel()
        {
            TouchCancel?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        public virtual void UpdateLayout(Rect rect)
        {
            if (IsGone)
                BoundingRect = Rect.Empty;

            var measuredSize = MeasureLayout();
            var offset = rect.Offset;

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Stretch:
                    if (!Height.IsFixed() && rect.Height.IsFixed()) measuredSize.Height = rect.Height;
                    break;
                case VerticalAlignment.Center:
                    offset.Y += (rect.Height - measuredSize.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    offset.Y += rect.Height - measuredSize.Height;
                    break;
            }

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Stretch:
                    if (!Width.IsFixed() && rect.Width.IsFixed()) measuredSize.Width = rect.Width;
                    break;
                case HorizontalAlignment.Center:
                    offset.X += (rect.Width - measuredSize.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    offset.X += rect.Width - measuredSize.Width;
                    break;
            }

            BoundingRect = new Rect(offset, measuredSize);
        }

        public virtual void InvalidateLayout(bool boundsChanged)
        {
            Parent?.InvalidateLayout(boundsChanged);
        }

        public virtual IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        public virtual Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : 0;
            var height = Height.IsFixed() ? Height : 0;

            return new Size(width, height) + Margin;
        }
    }

    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventHandlerArgs args);

    public class MouseButtonEventHandlerArgs
    {
        public Point AbsolutePoint { get; set; }

        public MouseButtonEventHandlerArgs(Point absolutePoint)
        {
            AbsolutePoint = absolutePoint;
        }
    }

    public delegate void MouseMoveEventHandler(object sender, MouseMoveEventHandlerArgs args);

    public class MouseMoveEventHandlerArgs
    {
        public Point AbsolutePoint { get; set; }

        public MouseMoveEventHandlerArgs(Point absolutePoint)
        {
            AbsolutePoint = absolutePoint;
        }
    }
}
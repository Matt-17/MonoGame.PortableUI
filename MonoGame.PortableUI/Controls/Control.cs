using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Exceptions;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Control : IUIElement
    {
        private readonly Timer _longPressTimer;
        private float _height;
        private bool _isGone;
        private bool _isVisible;
        private IUIElement _parent;
        private float _width;
        internal bool LastMouseLeftButtonState;

        internal bool LastMouseRightButtonState;

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

            _longPressTimer = new Timer(300);
            _longPressTimer.Elapsed += _longPressTimer_Elapsed;
        }

        public object Tag { get; set; }


        protected HoverStates HoverState { get; set; }
        protected ButtonStates LeftButtonState { get; set; }
        protected ButtonStates RightButtonState { get; set; }
        protected TouchStates TouchState { get; set; }

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
        public Rect ClientRect { get; protected set; }
        public Rect ClippingRect { get; protected set; }

        public Thickness Margin { get; set; }

        //public Border Rect { get; set; }

        public Vector2 Scale { get; set; }

        public Vector2 Translation { get; set; }

        public double Opacity { get; set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                _isVisible = value;
                InvalidateLayout(false);
            }
        }

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

        internal PointF RenderedPosition
        {
            get { return new PointF((int)((Position.X + Margin.Left) * ScreenEngine.ScaleFactor), (int)((Position.Y + Margin.Top) * ScreenEngine.ScaleFactor)); }
        }

        internal PointF Position { get; set; }

        public Brush BackgroundBrush { get; set; }

        public virtual void InvalidateLayout(bool boundsChanged)
        {
            Parent?.InvalidateLayout(boundsChanged);
        }

        public virtual IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        public event EventHandler StateChanged;

        private void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        private void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler LongPress;

        private void _longPressTimer_Elapsed(object sender, EventArgs e)
        {
            _longPressTimer?.Stop();
            LeftButtonState = ButtonStates.Released;
            TouchState = TouchStates.Released;
            OnStateChanged();
            LongPress?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            BackgroundBrush?.Draw(spriteBatch, rect - Margin);
        }


        public virtual void UpdateLayout(Rect rect)
        {
            if (IsGone)
                BoundingRect = Rect.Empty;

            var measuredSize = MeasureLayout();
            var offset = rect.Offset;

            BoundingRect = GetRectForAlignment(rect, measuredSize, offset);
        }

        protected Rect GetRectForAlignment(Rect rect, Size measuredSize, PointF offset)
        {
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
            return new Rect(offset, measuredSize);
        }

        public virtual Size MeasureLayout()
        {
            if (IsGone)
                return Size.Empty;

            var width = Width.IsFixed() ? Width : 0;
            var height = Height.IsFixed() ? Height : 0;

            return new Size(width, height) + Margin;
        }

        protected virtual void OnLongPress()
        {
            LongPress?.Invoke(this, EventArgs.Empty);
        }

        #region Events

        public event MouseMoveEventHandler MouseEnter;
        public event MouseMoveEventHandler MouseLeave;
        public event MouseMoveEventHandler MouseMove;
        public event MouseButtonEventHandler MouseLeftDown;
        public event MouseButtonEventHandler MouseRightDown;
        public event MouseButtonEventHandler MouseLeftUp;
        public event MouseButtonEventHandler MouseRightUp;
        public event TouchEventHandler TouchDown;
        public event TouchEventHandler TouchUp;
        public event TouchEventHandler TouchMove;
        public event TouchEventHandler TouchCancel;

        #endregion

        #region Event handlers

        internal virtual void OnMouseEnter(MouseMoveEventHandlerArgs args)
        {
            HoverState = HoverStates.Hovering;
            MouseEnter?.Invoke(this, args);
            OnStateChanged();
        }

        internal virtual void OnMouseLeave(MouseMoveEventHandlerArgs args)
        {
            HoverState = HoverStates.NotHovering;
            LeftButtonState = ButtonStates.Released;
            MouseLeave?.Invoke(this, args);
            OnStateChanged();
        }

        internal virtual void OnMouseLeftDown(MouseButtonEventHandlerArgs args)
        {
            LeftButtonState = ButtonStates.Pressed;
            MouseLeftDown?.Invoke(this, args);
            OnStateChanged();
            if (LongPress != null)
                _longPressTimer?.Start();
            if (Click != null)
                args.Handled = true;
        }


        internal virtual void OnMouseLeftUp(MouseButtonEventHandlerArgs args)
        {
            if (LeftButtonState == ButtonStates.Pressed)
            {
                _longPressTimer.Stop();
                LeftButtonState = ButtonStates.Released;
                MouseLeftUp?.Invoke(this, args);
                OnStateChanged();
                OnClick();
            }
            else
                MouseLeftUp?.Invoke(this, args);
            if (Click != null)
                args.Handled = true;
        }

        internal virtual void OnMouseRightDown(Point position)
        {
            RightButtonState = ButtonStates.Pressed;
            MouseRightDown?.Invoke(this, new MouseButtonEventHandlerArgs(position));
            OnStateChanged();
        }

        internal virtual void OnMouseRightUp(Point position)
        {

            if (RightButtonState == ButtonStates.Pressed)
            {
                RightButtonState = ButtonStates.Released;
                MouseRightUp?.Invoke(this, new MouseButtonEventHandlerArgs(position));
                OnStateChanged();
                OnRightClick();
            }
            else
                MouseRightUp?.Invoke(this, new MouseButtonEventHandlerArgs(position));
        }

        internal virtual void OnTouchDown(TouchEventHandlerArgs args)
        {
            TouchState = TouchStates.Touched;
            TouchDown?.Invoke(this, args);
            OnStateChanged();
            if (LongPress != null)
                _longPressTimer?.Start();

            if (Click != null)
                args.Handled = true;
        }

        internal virtual void OnTouchUp(TouchEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            if (TouchState == TouchStates.Touched)
            {
                TouchState = TouchStates.Released;
                TouchUp?.Invoke(this, args);
                OnStateChanged();
                OnClick();
            }
            else
                TouchUp?.Invoke(this, args);

            if (Click != null)
                args.Handled = true;
        }

        internal virtual void OnTouchMove(TouchEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            TouchMove?.Invoke(this, args);
        }

        internal virtual void OnMouseMove(MouseMoveEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            MouseMove?.Invoke(this, args);
        }

        internal virtual void OnTouchCancel(TouchEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            TouchState = TouchStates.Released;
            TouchCancel?.Invoke(this, args);
            OnStateChanged();
        }

        #endregion

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

    }

    public delegate void TouchEventHandler(object sender, TouchEventHandlerArgs args);

    public class TouchEventHandlerArgs : BaseEventHandlerArgs
    {
        public TouchEventHandlerArgs(PointF position)
        {
            Position = position;
        }

        public PointF Position { get; set; }
    }

    public class FlyOut : ContentControl
    {
        private Control content;

        public FlyOut(Control content)
        {
            this.content = content;
        }
    }

    public delegate void MouseButtonEventHandler(object sender, MouseButtonEventHandlerArgs args);

    public class MouseButtonEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseButtonEventHandlerArgs(PointF absolutePoint)
        {
            AbsolutePoint = absolutePoint;
        }

        public PointF AbsolutePoint { get; set; }
    }

    public class BaseEventHandlerArgs
    {
        public bool Handled { get; set; }
    }

    public delegate void MouseMoveEventHandler(object sender, MouseMoveEventHandlerArgs args);

    public class MouseMoveEventHandlerArgs : BaseEventHandlerArgs
    {
        public MouseMoveEventHandlerArgs(Point oldPoint, Point newPoint)
        {
            OldPoint = oldPoint;
            AbsolutePoint = newPoint;
        }

        public Point OldPoint { get; set; }
        public Point AbsolutePoint { get; set; }
    }
}
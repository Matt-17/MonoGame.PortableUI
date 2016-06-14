using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Exceptions;

namespace MonoGame.PortableUI.Controls
{
    public abstract class Control : FrameworkElement
    {
        private readonly Timer _longPressTimer;

        private Control _contextMenu;
        private float _height;
        private bool _isGone;
        private bool _isVisible;
        private FrameworkElement _parent;
        private float _width;

        protected Control()
        {
            SnapToPixel = true;
            Opacity = 1;
            IsEnabled = true;
            IsVisible = true;
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

        protected Dictionary<MouseButton, ButtonState> MouseButtonStates { get; } = new Dictionary<MouseButton, ButtonState>
        {
            {MouseButton.Left, ButtonState.Released},
            {MouseButton.Middle, ButtonState.Released},
            {MouseButton.Right, ButtonState.Released}
        };

        public bool IsFocused
        {
            get { return ScreenEngine.FocusedControl == this; }
            set { ScreenEngine.FocusedControl = value ? this : null; }
        }

        internal Screen Screen
        {
            get { return Parent as Screen ?? (Parent as Control)?.Screen; }
        }

        public object Tag { get; set; }

        public Control ContextMenu
        {
            get { return _contextMenu; }
            set
            {
                LongTouch -= ShowContextMenu;
                RightClick -= ShowContextMenu;
                _contextMenu = value;
                if (_contextMenu == null)
                    return;
                LongTouch += ShowContextMenu;
                RightClick += ShowContextMenu;
            }
        }

        protected HoverStates HoverState { get; set; }
        protected TouchStates TouchState { get; set; }

        public override FrameworkElement Parent
        {
            get { return _parent; }
            internal set
            {
                if (_parent != null && value != null)
                    throw new MultipleParentException();
                _parent = value;
            }
        }

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
            get { return new PointF((int) ((Position.X + Margin.Left)*ScreenEngine.ScaleFactor), (int) ((Position.Y + Margin.Top)*ScreenEngine.ScaleFactor)); }
        }

        internal PointF Position { get; set; }

        private void ShowContextMenu(object sender, EventArgs e)
        {
            var boundingRect = BoundingRect - Margin;
            var pointF = boundingRect;
            pointF.Top -= ContextMenu.MeasureLayout().Height;
            Screen.CreateFlyOut(pointF, ContextMenu);
        }


        public override void InvalidateLayout(bool boundsChanged)
        {
            Parent?.InvalidateLayout(boundsChanged);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        public event EventHandler StateChanged;

        public virtual void OnClick()
        {
            //if (this is TextBox)
            //    ScreenManager.FocusedControl = this;
            Click?.Invoke(this, EventArgs.Empty);
        }

        public virtual void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
        }

        private void _longPressTimer_Elapsed(object sender, EventArgs e)
        {
            _longPressTimer?.Stop();
            TouchState = TouchStates.Released;
            OnStateChanged();
            LongTouch?.Invoke(this, EventArgs.Empty);
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
                    offset.Y += (rect.Height - measuredSize.Height)/2;
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
                    offset.X += (rect.Width - measuredSize.Width)/2;
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

        protected virtual void OnLongTouch()
        {
            LongTouch?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Events

        public event MouseMoveEventHandler MouseEnter;
        public event MouseMoveEventHandler MouseLeave;
        public event MouseMoveEventHandler MouseMove;
        public event MouseButtonEventHandler MouseDown;
        public event MouseButtonEventHandler MouseUp;
        public event ScrollWheelChangedEventHandler ScrollWheelChanged;
        public event TouchEventHandler TouchDown;
        public event TouchEventHandler TouchUp;
        public event TouchEventHandler TouchMove;
        public event TouchEventHandler TouchCancel;

        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler LongTouch;
        public event KeyPressedEventHandler KeyPressed;

        #endregion

        #region Event handlers

        internal void OnMouseEnter(MouseMoveEventHandlerArgs args)
        {
            HoverState = HoverStates.Hovering;
            MouseEnter?.Invoke(this, args);
            OnStateChanged();
        }

        internal void OnMouseLeave(MouseMoveEventHandlerArgs args)
        {
            HoverState = HoverStates.NotHovering;
            MouseLeave?.Invoke(this, args);
            OnStateChanged();
        }

        internal void OnMouseDown(MouseButtonEventHandlerArgs args)
        {
            MouseButtonStates[args.Button] = ButtonState.Pressed;
            Debug.WriteLine("Pressed");
            MouseDown?.Invoke(this, args);
            OnStateChanged();
            if ((Click != null && args.Button == MouseButton.Left) || (RightClick != null && args.Button == MouseButton.Right))
                args.Handled = true;
        }


        internal void OnMouseUp(MouseButtonEventHandlerArgs args)
        {
            Debug.WriteLine("Released");
            if (MouseButtonStates[args.Button] == ButtonState.Pressed)
            {
                MouseButtonStates[args.Button] = ButtonState.Released;
                MouseUp?.Invoke(this, args);
                OnStateChanged();
                if (Click != null && args.Button == MouseButton.Left)
                {
                    OnClick();
                    args.Handled = true;
                }
                if (RightClick != null && args.Button == MouseButton.Right)
                {
                    OnRightClick();
                    args.Handled = true;
                }
            }
            else
                MouseUp?.Invoke(this, args);
        }

        internal void OnTouchDown(TouchEventHandlerArgs args)
        {
            TouchState = TouchStates.Touched;
            TouchDown?.Invoke(this, args);
            OnStateChanged();
            if (LongTouch != null)
                _longPressTimer?.Start();

            if (Click != null)
                args.Handled = true;
        }

        internal void OnTouchUp(TouchEventHandlerArgs args)
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

        internal void OnTouchMove(TouchEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            TouchMove?.Invoke(this, args);
        }

        internal void OnMouseMove(MouseMoveEventHandlerArgs args)
        {
            MouseMove?.Invoke(this, args);
        }

        internal void OnTouchCancel(TouchEventHandlerArgs args)
        {
            _longPressTimer.Stop();
            TouchState = TouchStates.Released;
            TouchCancel?.Invoke(this, args);
            OnStateChanged();
        }

        public void OnKeyPressed(string key)
        {
            KeyPressed?.Invoke(this, new KeyPressedEventHandlerArgs(key));
        }


        public void OnKeyPressed(KeyboardCommand key)
        {
            KeyPressed?.Invoke(this, new KeyPressedEventHandlerArgs(key));
        }

        #endregion

        protected internal virtual void OnScrollWheelChanged(ScrollWheelChangedEventHandlerArgs args)
        {
            ScrollWheelChanged?.Invoke(this, args);
        }
    }

    public delegate void ScrollWheelChangedEventHandler(object sender, ScrollWheelChangedEventHandlerArgs args);
}
﻿using System;
using System.Collections.Generic;
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
    public abstract class Control : UIElement
    {
        private readonly Timer _longPressTimer;

        private ContextMenu _contextMenu;
        private float _height;
        private FrameworkElement _parent;
        private float _width;
        private bool _suppressUpdate;
        public bool HandleTouchDownEnter { get; set; }

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
        }

        internal Screen Screen
        {
            get { return Parent as Screen ?? (Parent as Control)?.Screen; }
        }

        public object Tag { get; set; }

        public ContextMenu ContextMenu
        {
            get { return _contextMenu; }
            set
            {
                LongTouch -= ShowContextMenuTouch;
                MouseDown -= ShowContextMenuDown;
                RightClick -= ShowContextMenuClick;
                _contextMenu = value;
                if (_contextMenu == null)
                    return;
                LongTouch += ShowContextMenuTouch;
                MouseDown += ShowContextMenuDown;
                RightClick += ShowContextMenuClick;
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

        public void SuppressUpdate(bool suppress)
        {
            _suppressUpdate = suppress;
        }

        public bool IsEnabled { get; set; }

        public HorizontalAlignment HorizontalAlignment { get; set; }

        public VerticalAlignment VerticalAlignment { get; set; }

        public bool SnapToPixel { get; set; }
        
        internal PointF Position { get; set; }

        private void ShowContextMenuTouch(object sender, EventArgs e) { ShowContextMenu(true); }
        private void ShowContextMenuClick(object sender, EventArgs args) { if (ContextMenu.ContextMenuType == ContextMenuTypes.OpenAndClick) ShowContextMenu(false); }
        private void ShowContextMenuDown(object sender, MouseEventArgs args) { if (args.Buttons.Any(x => x == MouseButton.Right) && ContextMenu.ContextMenuType == ContextMenuTypes.OpenAndHold) ShowContextMenu(false); }

        private void ShowContextMenu(bool optimizeForTouch)
        {
            var boundingRect = BoundingRect - Margin;
            var pointF = boundingRect;
            Screen.CreateContextMenu(pointF.Offset, ContextMenu, optimizeForTouch);
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            if (_suppressUpdate)
                return;
            Parent?.InvalidateLayout(boundsChanged);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            return Enumerable.Empty<Control>();
        }

        public virtual void OnClick()
        {
            //if (this is TextBox)
            //    ScreenEngine.FocusedControl = this;
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
            ChangeVisualState();
            LongTouch?.Invoke(this, EventArgs.Empty);
        }

        protected internal virtual void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            BackgroundBrush?.Draw(spriteBatch, rect);
        }


        public virtual void UpdateLayout(Rect rect)
        {
            if (_suppressUpdate)
                return;

            if (IsGone)
                BoundingRect = Rect.Empty;

            var measuredSize = MeasureLayout();
            var offset = rect.Offset;

            BoundingRect = GetRectForAlignment(rect, measuredSize, offset);
            ClippingRect = BoundingRect - Margin;
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

        protected virtual void OnLongTouch()
        {
            LongTouch?.Invoke(this, EventArgs.Empty);
        }

        internal virtual void ChangeVisualState()
        {
        }

        #region Events

        public event MouseEventHandler MouseEnter;
        public event MouseEventHandler MouseLeave;
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        public event ScrollWheelChangedEventHandler ScrollWheelChanged;
        public event TouchEventHandler TouchDown;
        public event TouchEventHandler TouchUp;
        public event TouchEventHandler TouchMove;
        public event TouchEventHandler TouchCancel;

        public event EventHandler Click;
        public event EventHandler RightClick;
        public event EventHandler LongTouch;
        public event KeyEventHandler KeyPressed;
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;

        public event GotFocusEventHandler GotFocus;
        public event LostFocusEventHandler LostFocus;

        #endregion

        #region Event handlers

        internal void OnMouseEnter(MouseEventArgs args)
        {
            HoverState = HoverStates.Hovering;
            foreach (var button in MouseButtonStates.Where(x => x.Value == ButtonState.Pressed).ToList())
            {
                if (!args.Buttons.Contains(button.Key))
                    MouseButtonStates[button.Key] = ButtonState.Released;
            }
            MouseEnter?.Invoke(this, args);
            ChangeVisualState();
        }

        internal void OnMouseLeave(MouseEventArgs args)
        {
            HoverState = HoverStates.NotHovering;
            MouseLeave?.Invoke(this, args);
            ChangeVisualState();
        }

        internal void OnMouseDown(MouseEventArgs args)
        {
            foreach (var button in args.Buttons)
                MouseButtonStates[button] = ButtonState.Pressed;
            Focus();
            MouseDown?.Invoke(this, args);
            ChangeVisualState();
            if ((Click != null && args.Buttons.Contains(MouseButton.Left)) || (RightClick != null && args.Buttons.Contains(MouseButton.Right)))
                args.Handled = true;
        }


        internal void OnMouseUp(MouseEventArgs args)
        {
            //foreach (var button in args.Buttons)
            var changed = new List<MouseButton>();
            foreach (var button in args.Buttons)
            {
                if (MouseButtonStates[button] == ButtonState.Pressed)
                {
                    MouseButtonStates[button] = ButtonState.Released;
                    changed.Add(button);
                }

            }
            MouseUp?.Invoke(this, args);
            ChangeVisualState();
            if (Click != null && changed.Contains(MouseButton.Left))
            {
                OnClick();
                args.Handled = true;
            }
            if (RightClick != null && changed.Contains(MouseButton.Right))
            {
                OnRightClick();
                args.Handled = true;
            }
        }

        internal void OnTouchDown(TouchEventArgs args)
        {
            TouchState = TouchStates.Touched;
            TouchDown?.Invoke(this, args);
            ChangeVisualState();
            if (LongTouch != null)
                _longPressTimer?.Start();

            if (Click != null)
                args.Handled = true;
        }

        internal void OnTouchUp(TouchEventArgs args)
        {
            _longPressTimer.Stop();
            if (TouchState == TouchStates.Touched)
            {
                TouchState = TouchStates.Released;
                TouchUp?.Invoke(this, args);
                ChangeVisualState();

                if (Click != null)
                {
                    OnClick();
                    args.Handled = true;
                }
            }
            else
                TouchUp?.Invoke(this, args);
        }

        internal void OnTouchMove(TouchEventArgs args)
        {
            if (HandleTouchDownEnter)
            {
                TouchState = TouchStates.Touched;
                ChangeVisualState();
            }
            TouchMove?.Invoke(this, args);
        }

        internal void OnMouseMove(MouseEventArgs args)
        {
            MouseMove?.Invoke(this, args);
        }

        internal void OnTouchCancel(TouchEventArgs args)
        {
            _longPressTimer.Stop();
            TouchState = TouchStates.Released;
            TouchCancel?.Invoke(this, args);
            ChangeVisualState();
        }

        protected internal void OnKeyPressed(string key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        protected internal void OnKeyPressed(char key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        protected internal void OnKeyPressed(KeyboardCommand key)
        {
            KeyPressed?.Invoke(this, new KeyEventArgs(key));
        }

        #endregion

        protected internal virtual void OnScrollWheelChanged(ScrollWheelChangedEventArgs args)
        {
            ScrollWheelChanged?.Invoke(this, args);
        }

        public void ResetInputs()
        {
            TouchState = TouchStates.Released;
            MouseButtonStates[MouseButton.Left] = ButtonState.Released;
            MouseButtonStates[MouseButton.Middle] = ButtonState.Released;
            MouseButtonStates[MouseButton.Right] = ButtonState.Released;
            HoverState = HoverStates.NotHovering;
            ChangeVisualState();
        }

        protected internal virtual void OnGotFocus(GotFocusEventArgs args)
        {
            GotFocus?.Invoke(this, args);
        }

        protected internal virtual void OnLostFocus(LostFocusEventArgs args)
        {
            LostFocus?.Invoke(this, args);
        }

        protected virtual void OnKeyDown(KeyEventArgs args)
        {
            KeyDown?.Invoke(this, args);
        }

        protected virtual void OnKeyUp(KeyEventArgs args)
        {
            KeyUp?.Invoke(this, args);
        }

        public void Focus()
        {
            ScreenEngine.FocusedControl = this;
        }
    }
}
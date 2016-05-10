using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     9-Tile Button
    /// </summary>
    public class Button : ContentControl
    {
        public enum HoverStates
        {
            NotHovering,
            Hovering
        }

        public enum ButtonStates
        {
            Released,
            Pressed,
        }
        private bool _touch;

        protected Color CurrentBackgroundColor;

        protected internal Control Template;

        public Button()
        {
            Padding = new Thickness(8);
            //var grid = new Grid();
            //grid.AddChild(new Rect {BackgroundColor = Color.DarkMagenta});
            //grid.AddChild(new ContentPresenter(this));
            //Template = grid;

        }

        public HoverStates HoverState { get; set; }
        public ButtonStates LeftButtonState { get; set; }
        public ButtonStates RightButtonState { get; set; }

        public string Text
        {
            get
            {
                var textBlock = Content as TextBlock;
                return textBlock?.Text;
            }
            set
            {
                var textBlock = Content as TextBlock;
                if (textBlock == null)
                {
                    textBlock = new TextBlock();
                    Content = textBlock;
                }
                textBlock.Text = value;
            }
        }

        protected internal override void OnMouseEnter()
        {
            base.OnMouseEnter();
            HoverState = HoverStates.Hovering;
        }

        protected internal override void OnMouseLeave()
        {
            base.OnMouseLeave();
            HoverState = HoverStates.NotHovering;
        }

        protected internal override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            LeftButtonState = ButtonStates.Pressed;
        }

        protected internal override void OnMouseLeftUp()
        {
            base.OnMouseLeftUp();
            if (LeftButtonState == ButtonStates.Pressed)
            {
                LeftButtonState = ButtonStates.Released;
                OnClick();
            }

        }

        protected internal override void OnMouseRightDown()
        {
            base.OnMouseRightDown();
            RightButtonState = ButtonStates.Pressed;
        }

        protected internal override void OnMouseRightUp()
        {
            base.OnMouseRightUp();
            if (RightButtonState == ButtonStates.Pressed)
            {
                RightButtonState = ButtonStates.Released;
                OnRightClick();
            }
        }

        protected override void OnTouchDown()
        {
            base.OnTouchDown();
            _touch = true;
        }

        protected override void OnTouchUp()
        {
            base.OnTouchUp();
            _touch = false;
            OnClick();
        }

        protected override void OnTouchCancel()
        {
            base.OnTouchCancel();
            _touch = false;
        }

        public event EventHandler Click;
        public event EventHandler RightClick;

        //protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        //{
        //    // if center
        //    if (_touch || _mouseHover && _leftMouseButtonDown && _lastMouseHover)
        //        Status = ButtonStatus.Pressed;
        //    else if (_mouseHover)
        //    {
        //        Status = ButtonStatus.Hover;
        //        _lastMouseHover = true;
        //    }
        //    else
        //        Status = ButtonStatus.Normal;
        //    base.OnUpdate(elapsed, rect);

        //    Color textColor;
        //    switch (Status)
        //    {
        //        case ButtonStatus.Normal:
        //            textColor = Color.White;
        //            CurrentBackgroundColor = BackgroundColor;
        //            break;
        //        case ButtonStatus.Hover:
        //            textColor = Color.Black;
        //            CurrentBackgroundColor = Color.Goldenrod;
        //            break;
        //        case ButtonStatus.Pressed:
        //            textColor = Color.Black;
        //            CurrentBackgroundColor = Color.DarkGoldenrod;
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }
        //    var textBlock = Content as TextBlock;
        //    if (textBlock != null)
        //        textBlock.TextColor = textColor;

        //    Content?.OnUpdate(elapsed, rect - Padding);
        //}

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            var clientRect = rect - Margin;
            var color = LeftButtonState == ButtonStates.Pressed
                ? Color.LimeGreen
                : (HoverState == HoverStates.Hovering ? Color.GreenYellow : BackgroundColor);
            spriteBatch.Draw(ScreenEngine.Pixel, clientRect, color);
            Content?.OnDraw(spriteBatch, clientRect - Padding);
        }

        protected virtual void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
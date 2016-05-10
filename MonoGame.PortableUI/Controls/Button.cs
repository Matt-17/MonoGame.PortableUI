using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Button
    /// </summary>
    public class Button : ContentControl
    {
        protected Color CurrentBackgroundColor;
        protected internal Control Template;

        public Button()
        {
            Padding = new Thickness(8);
            BackgroundColor = Color.White;
            HoverColor = new Color(0, 0, 0, 0.2f);
            PressedColor = new Color(0, 0, 0, 0.4f);
            //var grid = new Grid();
            //grid.AddChild(new Rect {BackgroundColor = Color.DarkMagenta});
            //grid.AddChild(new ContentPresenter(this));
            //Template = grid;
        }

        public HoverStates HoverState { get; set; }
        public ButtonStates LeftButtonState { get; set; }
        public ButtonStates RightButtonState { get; set; }
        public TouchStates TouchState { get; set; }

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

        public Color HoverColor { get; set; }

        public Color PressedColor { get; set; }

        protected internal override void OnMouseEnter()
        {
            base.OnMouseEnter();
            HoverState = HoverStates.Hovering;
        }

        protected internal override void OnMouseLeave()
        {
            base.OnMouseLeave();
            HoverState = HoverStates.NotHovering;
            LeftButtonState = ButtonStates.Released;
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
            TouchState = TouchStates.Touched;
        }

        protected override void OnTouchUp()
        {
            base.OnTouchUp();
            TouchState = TouchStates.Released;
            OnClick();
        }

        protected override void OnTouchCancel()
        {
            base.OnTouchCancel();
            TouchState = TouchStates.Released;
        }

        public event EventHandler Click;
        public event EventHandler RightClick;

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            var clientRect = rect - Margin;
            spriteBatch.Draw(ScreenEngine.Pixel, clientRect, BackgroundColor);
            if (LeftButtonState == ButtonStates.Pressed)
                spriteBatch.Draw(ScreenEngine.Pixel, clientRect, PressedColor);
            else if (HoverState == HoverStates.Hovering)
                spriteBatch.Draw(ScreenEngine.Pixel, clientRect, HoverColor);

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
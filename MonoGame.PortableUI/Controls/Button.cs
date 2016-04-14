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
        private bool _lastMouseHover;
        private bool _leftMouseButtonDown;

        private bool _mouseHover;
        private bool _mouseRightButtonDown;
        private bool _touch;

        protected Color CurrentBackgroundColor;

        protected internal Control Template;

        public Button()
        {
            Padding = new Thickness(8);
            var grid = new Grid() {};
            grid.AddChild(new Rect() {BackgroundColor = Color.DarkMagenta});
            grid.AddChild(new ContentPresenter(this));
            Template = grid;

        }

        public ButtonStatus Status { get; set; }

        public string Text
        {
            get
            {
                var textBlock = Child as TextBlock;
                return textBlock?.Text;
            }
            set
            {
                var textBlock = Child as TextBlock;
                if (textBlock == null)
                {
                    textBlock = new TextBlock();
                    Child = textBlock;
                }
                textBlock.Text = value;
            }
        }

        protected override void OnMouseEnter()
        {
            base.OnMouseEnter();
            _mouseHover = true;
        }

        protected override void OnMouseLeave()
        {
            base.OnMouseLeave();
            _mouseHover = false;
        }

        protected override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            _leftMouseButtonDown = true;
        }

        protected override void OnMouseLeftUp()
        {
            base.OnMouseLeftUp();
            _leftMouseButtonDown = false;
            if (Status == ButtonStatus.Pressed)
                OnClick();
        }

        protected override void OnMouseRightDown()
        {
            base.OnMouseRightDown();
            _mouseRightButtonDown = true;
        }

        protected override void OnMouseRightUp()
        {
            base.OnMouseRightUp();
            if (_mouseRightButtonDown)
                OnRightClick();
            _mouseRightButtonDown = false;
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

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            // if center
            if (_touch || _mouseHover && _leftMouseButtonDown && _lastMouseHover)
                Status = ButtonStatus.Pressed;
            else if (_mouseHover)
            {
                Status = ButtonStatus.MouseOver;
                _lastMouseHover = true;
            }
            else
                Status = ButtonStatus.Normal;
            base.OnUpdate(elapsed, rect);

            Color textColor;
            switch (Status)
            {
                case ButtonStatus.Normal:
                    textColor = Color.White;
                    CurrentBackgroundColor = BackgroundColor;
                    break;
                case ButtonStatus.MouseOver:
                    textColor = Color.Black;
                    CurrentBackgroundColor = Color.Goldenrod;
                    break;
                case ButtonStatus.Pressed:
                    textColor = Color.Black;
                    CurrentBackgroundColor = Color.DarkGoldenrod;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var textBlock = Child as TextBlock;
            if (textBlock != null)
                textBlock.TextColor = textColor;

            Child?.OnUpdate(elapsed, rect - Padding);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            rect = rect - Margin;
            spriteBatch.Draw(ScreenEngine.Pixel, rect, CurrentBackgroundColor);

            Child?.OnDraw(spriteBatch, rect - Padding);
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
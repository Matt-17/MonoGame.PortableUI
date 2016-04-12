using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    /// 9-Tile Button 
    /// </summary>
    public class Button : ContentControl
    {
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
                    textBlock = new TextBlock(Game);
                    Child = textBlock;
                }
                textBlock.Text = value;
            }
        }

        private bool _mouseHover;
        private bool _leftMouseButtonDown;
        private bool _mouseRightButtonDown;

        public Button(Game game) : base(game)
        {
            MouseEnter += (sender, e) => _mouseHover = true;
            MouseLeave += (sender, e) => _mouseHover = false;
            MouseLeftDown += (sender, e) => _leftMouseButtonDown = true;
            MouseLeftUp += (sender, e) =>
            {
                _leftMouseButtonDown = false;
                if (Status == ButtonStatus.Pressed)
                    OnClick();
            };
            MouseRightDown += (sender, e) => _mouseRightButtonDown = true;
            MouseRightUp += (sender, e) =>
            {
                if (_mouseRightButtonDown)
                    OnRightClick();
                _mouseRightButtonDown = false;
            };
            TouchDown += (sender, e) => _touch = true;
            TouchCancel += (sender, e) => _touch = false;
            TouchUp += (sender, e) =>
            {
                _touch = false;
                OnClick();
            };

        }

        private bool _lastMouseHover;

        public event EventHandler Click;
        public event EventHandler RightClick;
        private bool _touch;

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

        protected Color CurrentBackgroundColor;

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rectangle rect)
        {
            rect = rect - Margin;
            spriteBatch.Draw(BackgroundTexture, rect, CurrentBackgroundColor);

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
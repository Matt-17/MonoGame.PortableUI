using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Button
    /// </summary>
    public class Button : ContentControl
    {
        protected internal Control Template;
        private Color _textColor;
        private Color? _pressedTextColor;
        private Color? _hoverTextColor;

        public Button()
        {
            StateChanged += OnStateChanged;
            Padding = new Thickness(8);
            BackgroundBrush = Color.White;
            HoverColor = new Color(0, 0, 0, 0.2f);
            PressedColor = new Color(0, 0, 0, 0.4f);
            TextColor = Color.Black;
            //var grid = new Grid();
            //grid.AddChild(new Rect { BackgroundBrush = Color.DarkMagenta });
            //grid.AddChild(new ContentPresenter(this));
            //Template = grid;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var clientRect = rect;
            if (HoverState == HoverStates.Hovering && MouseButtonStates[MouseButton.Left] == ButtonState.Pressed || TouchState == TouchStates.Touched)
                PressedColor.Draw(spriteBatch, clientRect);
            else if (HoverState == HoverStates.Hovering)
                HoverColor.Draw(spriteBatch, clientRect);
        }

        #region 

        public Brush HoverColor { get; set; }
        public Brush PressedColor { get; set; }

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
                    textBlock.TextAlignment = TextAlignment.Center;
                    Content = textBlock;
                }
                textBlock.Text = value;
            }
        }

        public Color TextColor
        {
            get { return _textColor; }
            set
            {
                _textColor = value;
                OnStateChanged();
                InvalidateLayout(false);
            }
        }

        public Color? PressedTextColor
        {
            get { return _pressedTextColor; }
            set
            {
                _pressedTextColor = value;
                OnStateChanged();
                InvalidateLayout(false);
            }
        }

        public Color? HoverTextColor
        {
            get { return _hoverTextColor; }
            set
            {
                _hoverTextColor = value;
                OnStateChanged();       
                InvalidateLayout(false);
            }
        }

        protected virtual void OnStateChanged(object sender, EventArgs eventArgs)
        {
            var textBlock = Content as TextBlock;
            if (textBlock == null)
                return;

            var color = TextColor;
            if (HoverState == HoverStates.Hovering && HoverTextColor != null)
                color = (Color)HoverTextColor;
            if ((MouseButtonStates[MouseButton.Left] == ButtonState.Pressed || TouchState == TouchStates.Touched) && PressedTextColor != null)
                color = (Color)PressedTextColor;
            textBlock.TextColor = color;
        }

        #endregion
    }
}
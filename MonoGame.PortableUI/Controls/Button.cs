using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
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
            var clientRect = rect - Margin;
            if (LeftButtonState == ButtonStates.Pressed)
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

        public Color TextColor { get; set; }
        public Color? PressedTextColor { get; set; }
        public Color? HoverTextColor { get; set; }

        public Image Image
        {
            get
            {
                var image = Content as Image;
                return image;
            }
            set
            {
                if (!value.Width.IsFixed() && Width.IsFixed())
                    value.Width = Width;
                if (!value.Height.IsFixed() && Height.IsFixed())
                    value.Height = Height;

                Content = value;
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
            if ((LeftButtonState == ButtonStates.Pressed || TouchState == TouchStates.Touched) && PressedTextColor != null)
                color = (Color)PressedTextColor;
            textBlock.TextColor = color;
        }

        #endregion
    }
}
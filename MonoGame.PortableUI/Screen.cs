using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    public abstract class Screen
    {
        protected Screen()
        {
            BackgroundColor = Color.Transparent;
        }

        public Color BackgroundColor { get; set; }

        internal ScreenManager ScreenEngine { get; set; }

        public Control Content { get; set; }

        protected internal void Draw(SpriteBatch spriteBatch)
        {
            if (BackgroundColor != Color.Transparent)
                spriteBatch.GraphicsDevice.Clear(BackgroundColor);
            if (Content != null)
            {
                spriteBatch.Begin();
                Content.Draw(spriteBatch);
                spriteBatch.End();
            }
        }

        public void Update(TimeSpan elapsed)
        {
            Content?.Update(elapsed);
        }
    }
}
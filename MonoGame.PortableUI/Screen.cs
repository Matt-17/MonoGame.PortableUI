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

        internal void Draw(SpriteBatch spriteBatch)
        {
            if (BackgroundColor != Color.Transparent)
                spriteBatch.GraphicsDevice.Clear(BackgroundColor);
            if (Content == null)
                return;
            var content = Content;
            DrawControl(spriteBatch, content);
            var panel = Content as Panel;
            if (panel != null)
                foreach (var child in panel.Children)
                    DrawControl(spriteBatch, child);
            var contentControl = Content as ContentControl;
            if (contentControl != null)
                DrawControl(spriteBatch, contentControl.Content);
        }

        private static void DrawControl(SpriteBatch spriteBatch, Control content)
        {
            spriteBatch.Begin();
            content.Draw(spriteBatch);
            spriteBatch.End();
        }

        internal void Update(TimeSpan elapsed)
        {




            Content?.Update(elapsed);
        }
    }
}
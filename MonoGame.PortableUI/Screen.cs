using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            Content.Draw(spriteBatch, new Common.Rect(ScreenEngine.Game.GraphicsDevice.Viewport.Width, ScreenEngine.Game.GraphicsDevice.Viewport.Height));
        }

        internal void Update(TimeSpan elapsed)
        {
            var controls = GetVisualTreeAsList(Content);
            foreach (var control in controls)
            {
                HandleMouse(control);

            }    
            //Content?.Update(elapsed);
        }

        private IEnumerable<Control> GetVisualTreeAsList(Control content)
        {
            var panel = content as Panel;
            if (panel != null)
            {
                foreach (var child in panel.Children.Reverse().Select(GetVisualTreeAsList).SelectMany(x => x))
                    yield return child;
            }
            var contentControl = content as ContentControl;
            if (contentControl != null)
            {
                foreach (var child in GetVisualTreeAsList(contentControl.Content))
                {
                    yield return child;
                }
            }
            yield return content;
        }



        private void HandleMouse(Control control)
        {
            var mouseState = Mouse.GetState();
            var position = mouseState.Position;
            var rect = new Rectangle(0, 0, (int) control.BoundingRect.Width, (int) control.BoundingRect.Height);
            if (rect.Contains(position))
            {
                if (control.LastMousePosition == null)
                {
                    control.OnMouseEnter();
                    if (mouseState.LeftButton == ButtonState.Pressed)
                        control.LastMouseLeftButtonState = true;
                    control.LastMousePosition = position;
                }

                if (control.LastMousePosition != position)
                {
                    control.OnMouseMove();
                    control.LastMousePosition = position;
                }

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!control.LastMouseLeftButtonState)
                    {
                        control.OnMouseLeftDown();
                        control.LastMouseLeftButtonState = true;
                    }
                }
                else
                {
                    if (control.LastMouseLeftButtonState)
                    {
                        control.OnMouseLeftUp();
                        control.LastMouseLeftButtonState = false;
                    }
                }
                if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (!control.LastMouseRightButtonState)
                    {
                        control.OnMouseRightDown();
                        control.LastMouseRightButtonState = true;
                    }
                }
                else
                {
                    if (control.LastMouseRightButtonState)
                    {
                        control.OnMouseRightUp();
                        control.LastMouseRightButtonState = false;
                    }
                }
            }
            else if (control.LastMousePosition != null)
            {
                control.OnMouseLeave();
                control.LastMousePosition = null;
            }
        }
    }
}
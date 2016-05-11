using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    public abstract class Screen : IUIElement
    {
        private Control _content;

        protected Screen()
        {
            BackgroundColor = Color.Transparent;
        }

        //public int Width => ScreenEngine.GraphicsDevice.Viewport.Width;
        //public int Height => ScreenEngine.GraphicsDevice.Viewport.Height;

        public Color BackgroundColor { get; set; }

        internal ScreenManager ScreenEngine { get; set; }

        public Control Content
        {
            get { return _content; }
            set
            {
                if (_content != null)
                    _content.Parent = null;
                _content = value;
                if (_content != null)
                    _content.Parent = this;
                InvalidateLayout(true);
            }
        }

        private IEnumerable<Control> GetVisualTreeAsList(Control content)
        {
            var descendants = content.GetDescendants();
            foreach (var child in descendants.SelectMany(GetVisualTreeAsList))
            {
                yield return child;
            }
            yield return content;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            if (BackgroundColor != Color.Transparent)
                spriteBatch.GraphicsDevice.Clear(BackgroundColor);
            Content?.Draw(spriteBatch, Content.BoundingRect);
        }

        internal void Update(TimeSpan elapsed)
        {
            var visualTreeAsList = GetVisualTreeAsList(Content);
            foreach (var control in visualTreeAsList)
            {              
                HandleMouse(control);
            }
        }      

        private void HandleMouse(Control control)
        {
            var mouseState = Mouse.GetState();
            var position = mouseState.Position;
            Rect rect = control.ClientRect - control.Margin;
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

        public void InvalidateLayout(bool boundsChanged)
        {
            Content?.UpdateLayout(new Rect(800, 480));
        }

        public IEnumerable<Control> GetDescendants()
        {
            yield return Content;
        }
    }
}
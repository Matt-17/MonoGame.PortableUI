using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public abstract class Screen : IUIElement
    {
        private Control _content;

        protected Screen()
        {
            BackgroundBrush = null;
        }

        public int Width => ScreenEngine.GraphicsDevice.Viewport.Width;
        public int Height => ScreenEngine.GraphicsDevice.Viewport.Height;

        public Brush BackgroundBrush { get; set; }

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

        private static IEnumerable<Control> GetVisualTreeAsList(Control content)
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
            if (BackgroundBrush != null )
            {
                spriteBatch.Begin();
                BackgroundBrush.Draw(spriteBatch, new Rect(Width, Height));
                spriteBatch.End();
            }
            Content?.Draw(spriteBatch, Content.BoundingRect);
            //var visualTreeAsList = GetVisualTreeAsList(Content);
            //foreach (var control in visualTreeAsList)
            //{
            //    Draw(spriteBatch);            
            //}
        }

        internal void Update(TimeSpan elapsed)
        {
            var visualTreeAsList = GetVisualTreeAsList(Content);
            foreach (var control in visualTreeAsList)
            {
                HandleMouse(control);
                HandleTouch(control);
            }
        }

        #region Input methods  

        private void HandleMouse(Control control)
        {
            var mouseState = Mouse.GetState();
            var position = mouseState.Position;
            Rect rect = control.BoundingRect - control.Margin;
            if (!rect.Contains(position))
            {
                if (control.LastMousePosition == null)
                    return;
                control.OnMouseLeave();
                control.LastMousePosition = null;
                return;
            }
            if (control.LastMousePosition == null)
            {
                control.OnMouseEnter();
                if (mouseState.LeftButton == ButtonState.Pressed)
                    control.LastMouseLeftButtonState = true;
                control.LastMousePosition = position;
            }

            if (control.LastMousePosition != position)
            {
                control.OnMouseMove(position);
                control.LastMousePosition = position;
            }

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!control.LastMouseLeftButtonState)
                {
                    control.OnMouseLeftDown(position);
                    control.LastMouseLeftButtonState = true;
                }
            }
            else
            {
                if (control.LastMouseLeftButtonState)
                {
                    control.OnMouseLeftUp(position);
                    control.LastMouseLeftButtonState = false;
                }
            }

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                if (!control.LastMouseRightButtonState)
                {
                    control.OnMouseRightDown(position);
                    control.LastMouseRightButtonState = true;
                }
            }
            else
            {
                if (control.LastMouseRightButtonState)
                {
                    control.OnMouseRightUp(position);
                    control.LastMouseRightButtonState = false;
                }
            }
        }

        private void HandleTouch(Control control)
        {
            var rect = control.BoundingRect;
            var collection = TouchPanel.GetState();
            if (control.IgnoreTouch || collection.Count != 1)
            {
                if (collection.Count > 0)
                    return;

                control.IgnoreTouch = false;
                if (control.LastTouchPosition == null)
                    return;

                control.OnTouchUp();
                control.LastTouchPosition = null;
                return;
            }
            var touch = collection[0];
            var position = touch.Position.ToPoint();
            if (!rect.Contains(position))
            {
                control.OnTouchCancel();
                control.IgnoreTouch = true;
                control.LastTouchPosition = null;
                return;
            }

            if (control.LastTouchPosition == null)
            {
                control.OnTouchDown();
                control.LastTouchPosition = position;
            }
            else
            {
                if (control.LastTouchPosition == position)
                    return;
                control.OnTouchMove();
                control.LastTouchPosition = position;
            }
        }

        #endregion

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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public abstract class Screen : FrameworkElement
    {
        private readonly ButtonState[] _mouseStates =
        {
            ButtonState.Released, // Left button
            ButtonState.Released, // Middle button
            ButtonState.Released // Right button
        };

        public override FrameworkElement Parent
        {
            get { return null; }
            internal set { }
        }

        private Control _content;

        internal PointF LastMousePosition;
        internal PointF LastTouchPosition;
        private FlyOut _flyOut;

        protected Screen()
        {
        }

        public bool Initialized { get; set; }

        public int Width => ScreenManager?.Width ?? 0;
        public int Height => ScreenManager?.Height ?? 0;

        internal ScreenManager ScreenManager { get; set; }

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

        internal FlyOut FlyOut
        {
            get { return _flyOut; }
            set
            {
                if (_flyOut != null)
                {
                    _flyOut.Parent = null;
                    _flyOut.Dispose();
                }
                _flyOut = value;
                if (_flyOut != null)
                    _flyOut.Parent = this;
            }
        }

        public override void InvalidateLayout(bool boundsChanged)
        {
            Content?.UpdateLayout(new Rect(Width, Height));
        }

        public override IEnumerable<Control> GetDescendants()
        {
            yield return Content;
        }

        private static IEnumerable<Control> GetVisualTreeAsList(Control content, bool addTreeWhichIsGone = true)
        {
            if (content.IsGone && !addTreeWhichIsGone)
                yield break;
            var descendants = content.GetDescendants();
            foreach (var child in descendants.SelectMany(control => GetVisualTreeAsList(control, addTreeWhichIsGone)))
            {
                yield return child;
            }
            yield return content;
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            if (BackgroundBrush != null)
            {
                spriteBatch.Begin();
                BackgroundBrush.Draw(spriteBatch, new Rect(Width, Height));
                spriteBatch.End();
            }
            spriteBatch.GraphicsDevice.ScissorRectangle = Content.BoundingRect;

            spriteBatch.Begin(SpriteSortMode.Immediate, rasterizerState: new RasterizerState { ScissorTestEnable = true });
            DrawControl(spriteBatch, Content);
            spriteBatch.End();

            if (FlyOut != null)
            {
                spriteBatch.Begin();
                DrawControl(spriteBatch, FlyOut);
                spriteBatch.End();
            }
        }

        internal void CreateFlyOut(Rect position, Control content)
        {
            FlyOut = new FlyOut(position, content);
            FlyOut.UpdateLayout(new Rect(Width, Height));
        }

        private static void DrawControl(SpriteBatch spriteBatch, Control control)
        {
            if (!control.IsVisible || control.IsGone)
                return;

            //if (!Visible) return;

            // Controlgröße ermitteln
            //Rectangle controlArea = new Rectangle(AbsolutePosition, ActualSize);
            //Rectangle localRenderMask = controlArea.Intersection(renderMask);

            // Scissor-Filter aktivieren
            //batch.Begin(rasterizerState: new RasterizerState() { ScissorTestEnable = true }, samplerState: SamplerState.LinearWrap, transformMatrix: AbsoluteTransformation);
            //OnDraw(batch, controlArea, gameTime);
            //batch.End();


            //invalidDrawing = false;
            control.OnDraw(spriteBatch, control.BoundingRect);

            var oldRect = new Rect(spriteBatch.GraphicsDevice.ScissorRectangle);
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect ^ control.BoundingRect;
            foreach (var c in control.GetDescendants())
            {
                DrawControl(spriteBatch, c);
            }
            spriteBatch.GraphicsDevice.ScissorRectangle = oldRect;
        }

        internal void Update(TimeSpan elapsed)
        {
            if (!Initialized)
            {
                InvalidateLayout(true);
                Initialized = true;
            }

            var mouseState = Mouse.GetState();
            TouchLocation touchState;
            var touchCollection = TouchPanel.GetState();
            if (touchCollection.Count > 0)
            {
                touchState = touchCollection[0];
            }
            var touchPosition = (PointF)touchState.Position.ToPoint();
            var mousePosition = (PointF)mouseState.Position;

            var content = FlyOut ?? Content;
            if (mousePosition != LastMousePosition)
            {
                var args = new MouseMoveEventHandlerArgs(LastMousePosition, mousePosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.AbsolutePoint) && !c.BoundingRect.Contains(a.OldPoint), (c, a) => { c.OnMouseEnter(a); }, (c, a) => c.BoundingRect.Contains(a.AbsolutePoint));
                IterateVisualTree(content, args, (c, a) => c.BoundingRect.Contains(a.AbsolutePoint) && c.BoundingRect.Contains(a.OldPoint), (c, a) => { c.OnMouseMove(a); }, null);
                IterateVisualTree(content, args, (c, a) => !c.BoundingRect.Contains(a.AbsolutePoint) && c.BoundingRect.Contains(a.OldPoint), (c, a) => { c.OnMouseLeave(a); }, (c, a) => c.BoundingRect.Contains(a.OldPoint));
                LastMousePosition = mousePosition;
            }

            if (mouseState.LeftButton == ButtonState.Pressed && _mouseStates[0] != ButtonState.Pressed)
            {
                _mouseStates[0] = ButtonState.Pressed;
                var args = new MouseButtonEventHandlerArgs(mousePosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.AbsolutePoint),
                    (c, a) =>
                    {
                        if (c.LastMouseLeftButtonState) return;
                        c.OnMouseLeftDown(a);
                        c.LastMouseLeftButtonState = true;
                    },
                    null
                    );
            }
            if (mouseState.LeftButton == ButtonState.Released && _mouseStates[0] != ButtonState.Released)
            {
                _mouseStates[0] = ButtonState.Released;
                var args = new MouseButtonEventHandlerArgs(mousePosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.AbsolutePoint),
                    (c, a) =>
                    {
                        if (!c.LastMouseLeftButtonState) return;
                        c.OnMouseLeftUp(a);
                        c.LastMouseLeftButtonState = false;
                    },
                    null
                    );
            }
            if (touchState.State == TouchLocationState.Pressed)
            {
                var args = new TouchEventHandlerArgs(touchPosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchDown(a); },
                    null
                    );
            }
            if (touchState.State == TouchLocationState.Released)
            {
                var args = new TouchEventHandlerArgs(touchPosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position),
                    (c, a) => { c.OnTouchUp(a); },
                    null
                    );
            }
            if (touchState.State == TouchLocationState.Moved && touchPosition != LastTouchPosition)
            {
                var args = new TouchEventHandlerArgs(touchPosition);
                IterateVisualTree(content, args,
                    (c, a) => c.BoundingRect.Contains(a.Position) || c.BoundingRect.Contains(LastTouchPosition),
                    (c, a) =>
                    {
                        if (c.BoundingRect.Contains(a.Position))
                            c.OnTouchMove(a);
                        else
                            c.OnTouchCancel(a);
                    },
                    null
                    );
                LastTouchPosition = touchPosition;
            }
        }

        private void IterateVisualTree<T>(Control control, T args, Func<Control, T, bool> actionFunc, Action<Control, T> action, Func<Control, T, bool> treeFunc) where T : BaseEventHandlerArgs
        {
            if (control.IsGone)
                return;
            var goIntoTree = treeFunc?.Invoke(control, args) ?? actionFunc(control, args);
            if (!goIntoTree)
                return;
            foreach (var descendant in control.GetDescendants())
            {
                IterateVisualTree(descendant, args, actionFunc, action, treeFunc);
                if (args.Handled)
                    return;
            }
            if (actionFunc(control, args))
                action(control, args);
        }
    }
}
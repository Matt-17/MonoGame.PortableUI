using System;
using System.Diagnostics;
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
        private Timer _longClickTimer;
        private const int LongClickDuration = 1; //in seconds

        public Button()
        {
            Padding = new Thickness(8);
            BackgroundBrush = Color.White;
            HoverColor = new Color(0, 0, 0, 0.2f);
            PressedColor = new Color(0, 0, 0, 0.4f);
            _longClickTimer = new Timer(LongClickDuration);
            _longClickTimer.Elapsed += OnLongClick;
            //var grid = new Grid();
            //grid.AddChild(new Rect {BackgroundBrush = Color.DarkMagenta});
            //grid.AddChild(new ContentPresenter(this));
            //Template = grid;
        }

        public HoverStates HoverState { get; set; }
        public ButtonStates LeftButtonState { get; set; }
        public ButtonStates RightButtonState { get; set; }
        public TouchStates TouchState { get; set; }

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
                    Content = textBlock;
                }
                textBlock.Text = value;
            }
        }

        protected internal override void OnMouseEnter()
        {
            base.OnMouseEnter();
            HoverState = HoverStates.Hovering;
        }

        protected internal override void OnMouseLeave()
        {
            base.OnMouseLeave();
            HoverState = HoverStates.NotHovering;
            LeftButtonState = ButtonStates.Released;
            _longClickTimer.Stop();
        }

        protected internal override async void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            LeftButtonState = ButtonStates.Pressed;
            await _longClickTimer.Start();
        }

        protected internal override void OnMouseLeftUp()
        {
            base.OnMouseLeftUp();
            if (LeftButtonState == ButtonStates.Pressed)
            {
                LeftButtonState = ButtonStates.Released;
                _longClickTimer.Stop();
                OnClick();
            }
        }

        protected internal override void OnMouseRightDown()
        {
            base.OnMouseRightDown();
            RightButtonState = ButtonStates.Pressed;
        }

        protected internal override void OnMouseRightUp()
        {
            base.OnMouseRightUp();
            if (RightButtonState == ButtonStates.Pressed)
            {
                RightButtonState = ButtonStates.Released;
                OnRightClick();
            }
        }

        protected internal override async void OnTouchDown()
        {
            base.OnTouchDown();
            TouchState = TouchStates.Touched;
            await _longClickTimer.Start();
        }

        protected internal override void OnTouchUp()
        {
            base.OnTouchUp();
            TouchState = TouchStates.Released;
            _longClickTimer.Stop();
            OnClick();
        }

        protected internal override void OnTouchCancel()
        {
            base.OnTouchCancel();
            TouchState = TouchStates.Released;
            _longClickTimer.Stop();
        }

        public event EventHandler Click;
        public event EventHandler LongClick;
        public event EventHandler RightClick;

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var clientRect = rect - Margin;
            if (LeftButtonState == ButtonStates.Pressed)
                PressedColor.Draw(spriteBatch, clientRect);
            else if (HoverState == HoverStates.Hovering)
                HoverColor.Draw(spriteBatch, clientRect);

            Content?.OnDraw(spriteBatch, clientRect - Padding);
        }

        protected virtual void OnClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRightClick()
        {
            RightClick?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLongClick(object sender, EventArgs eventArgs)
        {
            _longClickTimer.Stop();
            LeftButtonState = ButtonStates.Released;
            TouchState = TouchStates.Released;
            LongClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
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
            TextColor = Color.Black;
            _longClickTimer = new Timer(LongClickDuration);
            _longClickTimer.Elapsed += OnLongClick;
            //var grid = new Grid();
            //grid.AddChild(new Rect { BackgroundBrush = Color.DarkMagenta });
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

        protected virtual void OnStateChanged()
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

        protected internal override void OnMouseEnter()
        {
            base.OnMouseEnter();
            HoverState = HoverStates.Hovering;
            OnStateChanged();
        }

        protected internal override void OnMouseLeave()
        {
            base.OnMouseLeave();
            HoverState = HoverStates.NotHovering;
            LeftButtonState = ButtonStates.Released;
            OnStateChanged();
            _longClickTimer.Stop();
        }

        protected internal override async void OnMouseLeftDown(Point position)
        {
            base.OnMouseLeftDown(position);
            LeftButtonState = ButtonStates.Pressed;
            OnStateChanged();
            await _longClickTimer.Start();
        }

        protected internal override void OnMouseLeftUp(Point position)
        {
            base.OnMouseLeftUp(position);
            if (LeftButtonState == ButtonStates.Pressed)
            {
                LeftButtonState = ButtonStates.Released;
                OnStateChanged();
                _longClickTimer.Stop();
                OnClick();
            }
        }

        protected internal override void OnMouseRightDown(Point position)
        {
            base.OnMouseRightDown(position);
            RightButtonState = ButtonStates.Pressed;
            OnStateChanged();
        }

        protected internal override void OnMouseRightUp(Point position)
        {
            base.OnMouseRightUp(position);
            if (RightButtonState == ButtonStates.Pressed)
            {
                RightButtonState = ButtonStates.Released;
                OnStateChanged();
                OnRightClick();
            }
        }

        protected internal override async void OnTouchDown()
        {
            base.OnTouchDown();
            TouchState = TouchStates.Touched;
            OnStateChanged();
            await _longClickTimer.Start();
        }

        protected internal override void OnTouchUp()
        {
            base.OnTouchUp();
            TouchState = TouchStates.Released;
            OnStateChanged();
            _longClickTimer.Stop();
            OnClick();
        }

        protected internal override void OnTouchCancel()
        {
            base.OnTouchCancel();
            TouchState = TouchStates.Released;
            OnStateChanged();
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
            OnStateChanged();
            LongClick?.Invoke(this, EventArgs.Empty);
        }

        public override Size MeasureLayout()
        {

            return base.MeasureLayout();
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class TextBox : TextBlock
    {
        private IKeyboard _attachedKeyboard;

        public int CursorPosition { get; set; }

        public Brush CursorColor { get; set; }

        public event EventHandler EnterPressed;

        public string InputScope { get; set; }

        public new string Text
        {
            get { return base.Text; }
            set
            {
                if (base.Text == value)
                    return;
                var oldText = base.Text;
                base.Text = value;
                CursorPosition = Math.Min(CursorPosition, Text.Length);
                OnTextChanged(new TextChangedEventArgs(value, oldText));
            }
        }

        public string HintText { get; set; } = "Hint text";
        public Color HintTextColor { get; set; } = Color.Silver;

        public Thickness Padding { get; set; }
        public event TextChangedEventHandler TextChanged;

        public TextBox()
        {
            BackgroundBrush = Color.White;
            CursorPosition = 0;
            CursorColor = Color.Black;
            Click += OnClick;
            MouseUp += OnMouseUp;
            TouchUp += OnTouchUp;
            Height = 28;
            Padding = 4;
        }

        private void OnClick(object sender, EventArgs eventArgs)
        {
            Focus();
        }

        private void OnMouseUp(object sender, MouseEventArgs args)
        {
            SetCursorFromPosition(args.Position);
        }

        private void OnTouchUp(object sender, TouchEventArgs args)
        {
            SetCursorFromPosition(args.Position);
        }

        protected internal override void OnGotFocus(GotFocusEventArgs args)
        {
            base.OnGotFocus(args);
            ScreenEngine.Instance.RequestKeyboard(InputScope);
            AttachKeyboard(ScreenEngine.Instance.CurrentKeyboard);
        }

        protected internal override void OnLostFocus(LostFocusEventArgs args)
        {
            base.OnLostFocus(args);
            DetachKeyboard();
            ScreenEngine.Instance.HideKeyboard();
        }

        protected internal virtual void HandleKeyPressed(object sender, KeyEventArgs args)
        {
            switch (args.InputType)
            {
                case InputType.Char:
                    HandleCharPressed(args.Char);
                    
                    break;
                case InputType.Command:
                    HandleCommandPressed(args.Command);
                    break;
                case InputType.Function:
                    HandleFunctionPressed(args.Function);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal virtual void HandleCharPressed(char c)
        {
            Text = Text.Insert(CursorPosition, c.ToString());
            CursorPosition++;
        }

        protected internal virtual void HandleFunctionPressed(string function)
        {
            
        }

        protected internal virtual void HandleCommandPressed(KeyboardCommand command)
        {
            switch (command)
            {
                case KeyboardCommand.Backspace:
                    if (Text.Length > 0 && CursorPosition > 0)
                    {
                        CursorPosition--;
                        Text = Text.Remove(CursorPosition, 1);
                    }
                    break;
                case KeyboardCommand.Enter:
                    EnterPressed?.Invoke(this, EventArgs.Empty);
                    break;
                case KeyboardCommand.CursorLeft:
                    if (CursorPosition > 0)
                        CursorPosition--;
                    break;
                case KeyboardCommand.CursorRight:
                    if (CursorPosition < Text.Length)
                        CursorPosition++;
                    break;
                case KeyboardCommand.CursorUp:
                    //TODO cursor up handling
                    break;
                case KeyboardCommand.CursorDown:
                    //TODO cursor down handling
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var textRect = rect - Padding;

            if (Text.Length == 0 && !string.IsNullOrEmpty(HintText) && Font != null)
            {
                var measuredHint = MeasureText(HintText);
                var offset = textRect.Offset;
                offset.Y += (textRect.Height - measuredHint.Y) / 2;
                if (SnapToPixel)
                    offset = offset.ToInts();
                spriteBatch.DrawString(Font, HintText, offset, HintTextColor);
            }

            if (!IsFocused)
                return;

            if (ScreenSystem.TotalTime.Milliseconds > 500)
                return;
            var measuredText = MeasureText(Text.Substring(0, CursorPosition));
            var x = textRect.Left + measuredText.X;
            var height = MeasureText("|").Y;
            var top = (textRect.Height - height)/2 + textRect.Top;
            CursorColor.Draw(spriteBatch, new Rect(x, top, 1, height));
        }

        protected virtual void OnTextChanged(TextChangedEventArgs args)
        {
            TextChanged?.Invoke(this, args);
        }

        private void AttachKeyboard(IKeyboard keyboard)
        {
            if (_attachedKeyboard == keyboard)
                return;
            DetachKeyboard();
            _attachedKeyboard = keyboard;
            if (_attachedKeyboard != null)
                _attachedKeyboard.KeyPressed += HandleKeyPressed;
        }

        private void DetachKeyboard()
        {
            if (_attachedKeyboard != null)
                _attachedKeyboard.KeyPressed -= HandleKeyPressed;
            _attachedKeyboard = null;
        }

        private void SetCursorFromPosition(PointF position)
        {
            var textRect = BoundingRect - Margin - Padding;
            var x = Math.Max(0, position.X - textRect.Left);
            var closestIndex = 0;
            var closestDistance = float.MaxValue;

            for (var i = 0; i <= Text.Length; i++)
            {
                var measured = MeasureText(Text.Substring(0, i)).X;
                var distance = Math.Abs(measured - x);
                if (distance >= closestDistance)
                    continue;
                closestDistance = distance;
                closestIndex = i;
            }

            CursorPosition = closestIndex;
        }
    }
}

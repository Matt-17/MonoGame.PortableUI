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


        public event TextChangedEventHandler TextChanged;

        public TextBox()
        {
            BackgroundBrush = Color.White;
            CursorPosition = 0;
            CursorColor = Color.Black;
            Click += OnClick;
            Height = 28;
        }

        private void OnClick(object sender, EventArgs eventArgs)
        {
            ScreenEngine.FocusedControl = this;
        }

        protected internal override void OnGotFocus(GotFocusEventArgs args)
        {
            base.OnGotFocus(args);
            ScreenEngine.Instance.RequestKeyboard(InputScope);
            ScreenEngine.Instance.CurrentKeyboard.Control.KeyPressed += HandleKeyPressed;
        }

        protected internal override void OnLostFocus(LostFocusEventArgs args)
        {
            base.OnLostFocus(args);
            ScreenEngine.Instance.CurrentKeyboard.Control.KeyPressed -= HandleKeyPressed;
            ScreenEngine.Instance.HideKeyboard();
        }

        private void HandleKeyPressed(object sender, KeyEventArgs args)
        {
            switch (args.Command)
            {
                case KeyboardCommand.Input:
                    Text = Text.Insert(CursorPosition, args.Key);
                    CursorPosition += args.Key.Length;
                    break;
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

            if (!IsFocused)
                return;

            if (ScreenSystem.TotalTime.Milliseconds > 500)
                return;
            var measuredText = Font.MeasureString(Text.Substring(0, CursorPosition));
            var x = rect.Left + measuredText.X;
            var height = Font.MeasureString("|").Y;
            var top = (rect.Height - height) / 2 + rect.Top;
            CursorColor.Draw(spriteBatch, new Rect(x, top, 1, height));
        }

        protected virtual void OnTextChanged(TextChangedEventArgs args)
        {
            TextChanged?.Invoke(this, args);
        }
    }
}
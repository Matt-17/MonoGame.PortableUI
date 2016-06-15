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

        public new string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                CursorPosition = Math.Min(CursorPosition, Text.Length);
            }
        }

        public TextBox()
        {
            CursorPosition = 0;
            CursorColor = Color.Black;
            KeyPressed += HandleKeyPressed;
            Click += OnClick;
        }

        private void OnClick(object sender, EventArgs eventArgs)
        {
            ScreenEngine.FocusedControl = this;
        }

        private void HandleKeyPressed(object sender, KeyPressedEventArgs args)
        {
            switch (args.Command)
            {
                case KeyboardCommand.Input:
                    Text = Text.Insert(CursorPosition, args.Key);
                    CursorPosition += args.Key.Length;
                    break;
                case KeyboardCommand.Backspace:
                    if (Text.Length > 0)
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

            if (ScreenManager.Time.Milliseconds > 500)
                return;         
            var measuredText = Font.MeasureString(Text.Substring(0, CursorPosition));
            var measuredText2= Font.MeasureString("abcdefghiojklmyfLMH");
            var x = rect.Left+measuredText.X;
            var top = (rect.Height - measuredText2.Y)/2+rect.Top;
            CursorColor.Draw(spriteBatch, new Rect(x, top, 1, measuredText2.Y));      
        }
    }
}
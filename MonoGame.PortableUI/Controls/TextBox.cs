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

        public event EventHandler EnterPressed;

        private Border _cursor;

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
            _cursor = new Border();
            _cursor.Width = 100;
            _cursor.Height = 200;
            _cursor.BorderColor = Color.Red;

            KeyPressed += HandleKeyPressed;
        }

        private void HandleKeyPressed(object sender, KeyPressedEventHandlerArgs args)
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

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            _cursor.UpdateLayout(rect);
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            var measuredText = Font.MeasureString(Text.Substring(0, CursorPosition));
            var x = rect.Left+Margin.Left+measuredText.X;
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(x, rect.Top+Margin.Top, 1, rect.Height-Margin.Top-Margin.Bottom), Color.Red);
        }

        public override void OnClick()
        {
            base.OnClick();
            ScreenEngine.FocusedControl = this;
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class TextBox : TextBlock
    {
        public int CursorPosition { get; set; }

        public event EventHandler EnterPressed;


        public TextBox()
        {
            CursorPosition = 0;
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
        
        public override void OnClick()
        {
            base.OnClick();
            ScreenEngine.FocusedControl = this;
        }
    }

    public enum CursorDirection
    {
        Left,
        Right,
        Up,
        Down
    }
}
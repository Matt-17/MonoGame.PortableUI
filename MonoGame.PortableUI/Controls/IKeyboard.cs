using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public interface IKeyboard
    {
        int Height { get; }
        Control Control { get; }
        void OnKeyboardAppear();
        void OnKeyboardDisappear();
        event KeyboardHiddenEventHandler KeyboardHidden;
        event KeyEventHandler KeyPressed;
        event KeyEventHandler KeyDown;
        event KeyEventHandler KeyUp;
    }
}
namespace MonoGame.PortableUI.Controls
{
    public enum KeyboardCommand
    {
        Backspace,
        Enter,
        CursorLeft,
        CursorRight,
        CursorUp,
        CursorDown,
        Delete,
        Home,
        End,
        SelectAll,
        Copy,
        Cut,
        Paste
    }

    [System.Flags]
    public enum KeyboardModifiers
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }

    public enum InputType
    {
        Char,
        Command,
        Function
    }
}

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
        Paste,
        Escape,
        PageUp,
        PageDown,
        /// <summary>Context-menu key / Shift+F10 / gamepad Y: open the focused control's ContextMenu.</summary>
        ContextMenu,
        Undo,
        Redo,
        /// <summary>Insert key: toggles overwrite mode in text boxes.</summary>
        Insert
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

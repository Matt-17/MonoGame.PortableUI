namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyEventArgs
    {
        public KeyEventArgs(string function)
            : this(function, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(string function, KeyboardModifiers modifiers)
        {
            InputType = InputType.Function;
            Function = function;
            Modifiers = modifiers;
        }

        public KeyEventArgs(KeyboardCommand command)
            : this(command, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(KeyboardCommand command, KeyboardModifiers modifiers)
        {
            InputType = InputType.Command;
            Command = command;
            Modifiers = modifiers;
        }


        public KeyEventArgs(char key)
            : this(key, KeyboardModifiers.None)
        {
        }

        public KeyEventArgs(char key, KeyboardModifiers modifiers)
        {
            InputType = InputType.Char;
            Char = key;
            Modifiers = modifiers;
        }

        public InputType InputType { get; }
        public string? Function { get; }
        public char Char { get; }
        public KeyboardCommand Command { get; }
        public KeyboardModifiers Modifiers { get; }
    }
}

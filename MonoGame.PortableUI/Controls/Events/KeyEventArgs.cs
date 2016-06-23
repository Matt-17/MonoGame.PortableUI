namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyEventArgs
    {
        public KeyEventArgs(string function)
        {
            InputType = InputType.Function;
            Function = function;
        }

        public KeyEventArgs(KeyboardCommand command)
        {
            InputType = InputType.Command;
            Command = command;
        }


        public KeyEventArgs(char key)
        {
            InputType = InputType.Char;
            Char = key;
        }

        public InputType InputType { get; }
        public string Function { get; }
        public char Char { get; }
        public KeyboardCommand Command { get; }
    }
}
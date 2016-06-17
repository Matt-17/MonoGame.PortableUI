namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyEventArgs
    {
        public KeyEventArgs(string key)
        {
            Command = KeyboardCommand.Input;
            Key = key;
        }

        public KeyEventArgs(KeyboardCommand command)
        {
            Command = command;
        }

        public KeyboardCommand Command { get; }
        public string Key { get; }
    }
}
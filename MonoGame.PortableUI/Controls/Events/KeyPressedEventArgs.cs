namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyPressedEventArgs
    {
        public KeyPressedEventArgs(string key)
        {
            Command = KeyboardCommand.Input;
            Key = key;
        }

        public KeyPressedEventArgs(KeyboardCommand command)
        {
            Command = command;
        }

        public KeyboardCommand Command { get; }
        public string Key { get; }
    }
}
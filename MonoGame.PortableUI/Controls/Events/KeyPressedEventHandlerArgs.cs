namespace MonoGame.PortableUI.Controls.Events
{
    public class KeyPressedEventHandlerArgs
    {
        public KeyPressedEventHandlerArgs(string key)
        {
            Command = KeyboardCommand.Input;
            Key = key;
        }

        public KeyPressedEventHandlerArgs(KeyboardCommand command)
        {
            Command = command;
        }

        public KeyboardCommand Command { get; }
        public string Key { get; }
    }
}
namespace MonoGame.PortableUI.Controls
{
    public class TextChangedEventArgs
    {
        public string NewText { get; set; }
        public string OldText { get; set; }

        public TextChangedEventArgs(string newText, string oldText)
        {
            NewText = newText;
            OldText = oldText;
        }
    }
}
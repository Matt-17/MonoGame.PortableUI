namespace MonoGame.PortableUI.Controls.Events
{
    public class GotFocusEventArgs
    {
        public Control OldElement { get; set; }

        public GotFocusEventArgs(Control oldElement)
        {
            OldElement = oldElement;
        }
    }
}
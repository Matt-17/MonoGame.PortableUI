namespace MonoGame.PortableUI.Controls.Events
{
    public class LostFocusEventArgs
    {
        public Control NewElement { get; set; }

        public LostFocusEventArgs(Control newElement)
        {
            NewElement = newElement;
        }
    }
}
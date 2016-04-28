namespace MonoGame.PortableUI.Controls
{
    public class ContentChangedEventArgs
    {
        public Control NewControl { get; set; }

        public ContentChangedEventArgs(Control newControl)
        {
            NewControl = newControl;
        }
    }
}
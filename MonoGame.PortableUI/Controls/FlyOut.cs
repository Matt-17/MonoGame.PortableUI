namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl
    {
        private Control content;

        public FlyOut(Control content)
        {
            this.content = content;
        }
    }
}
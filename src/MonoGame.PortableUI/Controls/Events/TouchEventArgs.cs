using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls.Events
{
    public class TouchEventArgs : BaseEventArgs
    {
        public TouchEventArgs(PointF position)
        {
            Position = position;
        }

        public PointF Position { get; set; }
    }
}
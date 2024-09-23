using System;

namespace MonoGame.PortableUI.Controls.Events
{
    public class CollectionChangedEventArgs : EventArgs
    {
        public Control[] NewElements { get; }
        public Control[] RemovedElements { get; }

        public CollectionChangedEventArgs(Control[] newElements, Control[] removedElements)
        {
            NewElements = newElements;
            RemovedElements = removedElements;
        }
    }
}
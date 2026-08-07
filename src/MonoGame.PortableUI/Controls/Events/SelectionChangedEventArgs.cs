using System;

namespace MonoGame.PortableUI.Controls.Events
{
    /// <summary>Carries the previous and new selection index of a selector control
    /// (ListBox, ComboBox, DataGrid, TabControl). Indexes are -1 when nothing is selected.</summary>
    public class SelectionChangedEventArgs : EventArgs
    {
        public SelectionChangedEventArgs(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public int OldIndex { get; }
        public int NewIndex { get; }
    }
}

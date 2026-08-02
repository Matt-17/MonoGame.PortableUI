namespace MonoGame.PortableUI.Controls.Events
{
    public class ValueChangedEventArgs : BaseEventArgs
    {
        public float OldValue { get; init; }

        public float NewValue { get; init; }
    }
}

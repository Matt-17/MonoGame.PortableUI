namespace MonoGame.PortableUI.Common
{
    public class RowDefinition
    {
        public RowDefinition()
        {
            Height = new GridLength {Unit = GridLengthUnit.Relative, Value = 1};
        }

        public GridLength Height { get; set; }
    }
}
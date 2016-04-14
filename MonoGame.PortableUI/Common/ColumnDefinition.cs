namespace MonoGame.PortableUI.Common
{
    public class ColumnDefinition
    {
        public ColumnDefinition()
        {
            Width = new GridLength(1, GridLengthUnit.Relative);
        }

        public GridLength Width { get; set; }
    }
}
namespace MonoGame.PortableUI.Common
{
    public class ColumnDefinition : GridDefinition
    {
        public ColumnDefinition()
        {
            Width = new GridLength(1, GridLengthUnit.Relative);
        }

        public GridLength Width { get; set; }
    }
}
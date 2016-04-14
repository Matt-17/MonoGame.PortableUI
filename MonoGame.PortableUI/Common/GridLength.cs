namespace MonoGame.PortableUI.Common
{
    public struct GridLength
    {
        public static GridLength Auto = new GridLength(1, GridLengthUnit.Auto);

        public GridLength(int value) : this()
        {
            Value = value;
            Unit = GridLengthUnit.Absolute;
        }

        public GridLength(int value, GridLengthUnit unit) : this()
        {
            Value = value;
            Unit = unit;
        }

        public GridLengthUnit Unit { get; set; }
        public float Value { get; set; }
    }
}
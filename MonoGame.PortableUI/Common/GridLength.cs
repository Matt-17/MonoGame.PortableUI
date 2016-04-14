namespace MonoGame.PortableUI.Common
{
    public class GridLength
    {
        public static GridLength Auto = new GridLength(1, GridLengthUnit.Auto);

        public GridLength()
        {
            Pixels = 1;
            Unit = GridLengthUnit.Relative;
        }

        public GridLength(int pixels) 
        {
            Pixels = pixels;
            Unit = GridLengthUnit.Absolute;
        }

        public GridLength(int pixels, GridLengthUnit unit)
        {
            Pixels = pixels;
            Unit = unit;
        }

        public GridLengthUnit Unit { get; set; }  

        public int Pixels { get; set; }
    }
}
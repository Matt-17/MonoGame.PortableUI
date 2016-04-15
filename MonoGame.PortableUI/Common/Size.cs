namespace MonoGame.PortableUI.Common
{
    public struct Size
    {
        public Size(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; set; }
        public float Height { get; set; }
    }
}
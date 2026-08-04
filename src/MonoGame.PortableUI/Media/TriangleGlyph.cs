using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    /// Shared antialiased triangle glyph used for the ComboBox dropdown arrow and the DataGrid sort
    /// indicator, so both read as the same visual language. Cached per graphics device.
    /// </summary>
    internal static class TriangleGlyph
    {
        private const int Width = 30;
        private const int Height = 18;

        /// <summary>Returns a filled triangle texture pointing up or down (device-cached, white/alpha).</summary>
        public static Texture2D Get(GraphicsDevice graphicsDevice, bool pointingUp)
        {
            var key = new BrushTextureCacheKey(pointingUp ? "triangle-glyph-up-v1" : "triangle-glyph-down-v1", Width, Height);
            return BrushTextureCache.GetOrCreate(graphicsDevice, key, device =>
            {
                var data = new Color[Width * Height];
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var covered = 0;
                        for (var sy = 0; sy < 2; sy++)
                        {
                            for (var sx = 0; sx < 2; sx++)
                            {
                                var py = y + 0.25f + sy * 0.5f;
                                var px = x + 0.25f + sx * 0.5f;
                                // Down triangle narrows towards the bottom; up triangle narrows towards the top.
                                var t = pointingUp ? (Height - py) : py;
                                var inset = t / Height * (Width / 2f);
                                if (px >= inset && px <= Width - inset)
                                    covered++;
                            }
                        }

                        var coverage = (byte)(covered * 255 / 4);
                        data[y * Width + x] = new Color(coverage, coverage, coverage, coverage);
                    }
                }

                var texture = new Texture2D(device, Width, Height);
                texture.SetData(data);
                return texture;
            });
        }
    }
}

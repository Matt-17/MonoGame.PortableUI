using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI
{
    public enum ThemeEra
    {
        Retro,
        Desktop,
        Modern,
        Terminal,
        Glass
    }

    public enum ThemeBrightness
    {
        Light,
        Dark
    }

    public sealed class ThemeMetadata
    {
        public ThemeEra Era { get; init; }
        public ThemeBrightness Brightness { get; init; }
        public bool ReducedMotion { get; init; }
        public string Description { get; init; } = "";
        public IReadOnlyList<Color> PreviewSwatches { get; init; } = new List<Color>();
    }
}

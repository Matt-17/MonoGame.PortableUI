using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI
{
    public sealed class ThemeDefinition
    {
        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public string FontName { get; init; } = "";
        public ThemePalette Palette { get; init; } = ThemePalette.Empty;
        public ThemeMetadata Metadata { get; init; } = new ThemeMetadata();
        public Color ClearColor { get; init; }
        public Color BackgroundColor { get; init; }
        public Func<PortableTheme> CreateTheme { get; init; } = PortableTheme.CreateDefault;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

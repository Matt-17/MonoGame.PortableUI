using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoThemePreset
    {
        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public Func<PortableTheme> CreateTheme { get; init; } = PortableTheme.CreateDefault;
        public DemoThemePalette Palette { get; init; } = DemoThemePalette.Empty;
        public string FontName { get; init; } = "";
        public Color ClearColor { get; init; }
        public Color BackgroundColor { get; init; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

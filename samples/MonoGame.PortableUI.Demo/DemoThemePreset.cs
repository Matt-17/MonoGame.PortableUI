using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Demo
{
    public sealed class DemoThemePreset
    {
        private PortableTheme? _sharedTheme;

        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public Func<PortableTheme> CreateTheme { get; init; } = PortableTheme.CreateDefault;
        public ThemePalette Palette { get; init; } = ThemePalette.Empty;
        public string FontName { get; init; } = "";
        public Color ClearColor { get; init; }
        public Color BackgroundColor { get; init; }

        /// <summary>
        ///     Cached theme instance for read-only consumers (gallery cards, previews) — creating
        ///     37 themes on every switch caused a visible hitch. Apply/switch paths that mutate the
        ///     theme should keep calling <see cref="CreateTheme"/>.
        /// </summary>
        public PortableTheme SharedTheme => _sharedTheme ??= CreateTheme();

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

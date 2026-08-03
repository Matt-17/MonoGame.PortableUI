using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Themes;

/// <summary>The library's untouched styling when no theme is applied (PortableTheme.CreateDefault).</summary>
public static class DefaultTheme
{
    public static ThemeDefinition Create()
    {
        var teal = new Color(20, 126, 133);
        var palette = new ThemePalette
        {
            Background = Color.White,
            Surface = new Color(245, 245, 245),
            SurfaceAlt = new Color(225, 229, 233),
            Text = Color.Black,
            HeadingText = Color.Black,
            MutedText = new Color(105, 115, 125),
            Primary = teal,
            Secondary = new Color(82, 101, 111),
            Warning = new Color(214, 143, 0),
            Danger = new Color(178, 34, 34),
            Info = new Color(51, 153, 255),
            Selection = teal,
            SelectionText = Color.White,
            TabText = Color.Black,
            SelectedTabText = Color.Black,
            FieldFrame = Color.White,
            FieldBorder = new Color(82, 101, 111),
            DisabledSurface = new Color(210, 216, 222),
            DisabledText = Color.Gray
        };

        return new ThemeDefinition
        {
            Id = "default",
            DisplayName = "Default (no theme)",
            FontName = "default",
            Palette = palette,
            Metadata = new ThemeMetadata
            {
                Era = ThemeEra.Modern,
                Brightness = ThemeBrightness.Light,
                ReducedMotion = false,
                Description = "The library's built-in styling when no theme is applied (PortableTheme.CreateDefault).",
                PreviewSwatches = new[] { palette.Background, palette.Surface, palette.Primary, palette.Secondary, palette.Selection }
            },
            // Deliberately the untouched library default so the demo shows what consumers get without a theme.
            CreateTheme = PortableTheme.CreateDefault,
            ClearColor = Color.White,
            BackgroundColor = Color.White
        };
    }
}

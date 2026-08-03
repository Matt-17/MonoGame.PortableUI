using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Themes;

/// <summary>NeXTSTEP: dark gray workspace with strongly beveled light-gray chrome.</summary>
public static class NextStepTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("nextstep", "NeXTSTEP", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#555555", surface: "#AAAAAA", surfaceAlt: "#777777", text: "#000000",
            primary: "#111111", secondary: "#D0D0D0", selection: "#111111", selectionText: "#FFFFFF",
            styleTheme: theme => ThemeBuilder.Bevel(theme, theme.Palette.Surface, ThemeBuilder.Hex("#D0D0D0"), theme.Palette.Surface, ThemeBuilder.Hex("#585858"), ThemeBuilder.Hex("#303030")));
    }
}
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Themes;

/// <summary>Windows 95: teal desktop, gray raised-bevel buttons that sink when pressed.</summary>
public static class Win95Theme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("win95", "Windows 95", "selawik", ThemeEra.Desktop, ThemeBrightness.Light,
            background: "#008080", surface: "#C0C0C0", surfaceAlt: "#E0E0E0", text: "#000000",
            primary: "#000080", secondary: "#1084D0", selection: "#000080", selectionText: "#FFFFFF",
            styleTheme: theme => ThemeBuilder.Bevel(theme, theme.Palette.Surface, Color.White, ThemeBuilder.Hex("#DFDFDF"), ThemeBuilder.Hex("#808080"), Color.Black));
    }
}

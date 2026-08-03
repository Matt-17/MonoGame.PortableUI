using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Windows Aero / 7: sky-blue acrylic glass with white frames and soft panel shadows.</summary>
public static class AeroTheme
{
    public static ThemeDefinition Create()
    {
        return ThemeBuilder.Catalog("aero", "Windows Aero / 7", "selawik", ThemeEra.Glass, ThemeBrightness.Light,
            background: "#DCEFFF", surface: "#B8D6FB", surfaceAlt: "#FFFFFF", text: "#1E395B",
            primary: "#2FA6DE", secondary: "#5A8BB0", selection: "#2FA6DE", selectionText: "#FFFFFF",
            glass: true,
            styleTheme: theme =>
            {
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(new Color(255, 255, 255, 96)), 1, 10);
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
            });
    }
}
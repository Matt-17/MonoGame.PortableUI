using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Aurora Modern: graphite-to-teal gradients with mint/lime highlights and soft glow shadows.</summary>
public static class AuroraTheme
{
    public static ThemeDefinition Create()
    {
        var graphite = new Color(15, 18, 24);
        var deepTeal = new Color(17, 45, 40);
        var panel = new Color(25, 31, 34);
        var panelAlt = new Color(38, 48, 51);
        var field = new Color(11, 15, 18);
        var text = new Color(235, 247, 242);
        var mint = new Color(104, 228, 192);
        var lime = new Color(231, 247, 109);
        var ink = new Color(12, 15, 18);
        var palette = new ThemePalette
        {
            Background = graphite,
            Surface = panel,
            SurfaceAlt = panelAlt,
            Text = text,
            HeadingText = Color.White,
            MutedText = new Color(150, 174, 169),
            Primary = mint,
            Secondary = new Color(255, 178, 93),
            Warning = new Color(255, 178, 93),
            Danger = new Color(255, 107, 122),
            Info = new Color(124, 199, 255),
            Selection = lime,
            SelectionText = ink,
            TabText = text,
            SelectedTabText = ink,
            FieldFrame = field,
            FieldBorder = mint,
            DisabledSurface = new Color(45, 51, 53),
            DisabledText = new Color(103, 121, 119),
            BackgroundBrush = new GradientBrush(graphite, deepTeal, GradientDirection.DiagonalDown),
            SurfaceBrush = new GradientBrush(panel, new Color(31, 43, 40), GradientDirection.Vertical),
            SurfaceAltBrush = new GradientBrush(panelAlt, new Color(31, 39, 45), GradientDirection.Horizontal),
            SelectionBrush = new GradientBrush(mint, lime, GradientDirection.Horizontal),
            FieldFrameBrush = new GradientBrush(field, new Color(18, 25, 28), GradientDirection.Vertical)
        };

        return ThemeBuilder.CreateDefinition("aurora", "Aurora Modern", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Dark, palette, graphite,
            styleTheme: theme =>
            {
                theme.ButtonBackgroundBrush = new GradientBrush(panelAlt, new Color(42, 60, 55), GradientDirection.DiagonalDown);
                theme.ButtonHoverBrush = new GradientBrush(new Color(104, 228, 192, 85), new Color(231, 247, 109, 75), GradientDirection.Horizontal);
                theme.ButtonPressedBrush = new GradientBrush(new Color(255, 178, 93, 135), new Color(104, 228, 192, 120), GradientDirection.Horizontal);
                theme.ToolTipBackgroundBrush = new GradientBrush(new Color(8, 12, 14, 245), new Color(24, 38, 34, 245), GradientDirection.Vertical);
                ThemeBuilder.Chrome(theme.Button, null, null, 0, 10);
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 130), Offset = new Vector2(0, 8), Blur = 16 };
                theme.ButtonShadow = new ShadowStyle { Color = new Color(104, 228, 192, 70), Offset = new Vector2(0, 4), Blur = 12 };
            });
    }
}
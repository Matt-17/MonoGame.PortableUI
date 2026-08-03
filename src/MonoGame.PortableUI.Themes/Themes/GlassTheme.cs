using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>Frosted glass: translucent frosted panes over a night backdrop with soft light bands.</summary>
public static class GlassTheme
{
    public static ThemeDefinition Create()
    {
        var night = new Color(9, 14, 28);
        var cyan = new Color(97, 242, 226, 214);
        var pane = new FrostedGlassBrush(new Color(255, 255, 255, 42), new Color(255, 255, 255, 216), 18, 0.42f);
        var paneAlt = new FrostedGlassBrush(new Color(205, 248, 255, 48), new Color(255, 255, 255, 202), 20, 0.44f);
        var field = new FrostedGlassBrush(new Color(255, 255, 255, 54), new Color(255, 255, 255, 208), 16, 0.34f);
        var palette = new ThemePalette
        {
            Background = night,
            Surface = new Color(255, 255, 255, 42),
            SurfaceAlt = new Color(205, 248, 255, 48),
            Text = new Color(238, 250, 255),
            HeadingText = Color.White,
            MutedText = new Color(179, 211, 222),
            Primary = cyan,
            Secondary = new Color(255, 144, 116, 214),
            Warning = new Color(255, 211, 124, 214),
            Danger = new Color(255, 86, 128, 218),
            Info = new Color(136, 148, 255, 214),
            Selection = cyan,
            SelectionText = new Color(7, 20, 29),
            TabText = new Color(238, 250, 255),
            SelectedTabText = new Color(7, 20, 29),
            FieldFrame = new Color(255, 255, 255, 54),
            FieldBorder = new Color(255, 255, 255, 146),
            DisabledSurface = new Color(20, 29, 41, 164),
            DisabledText = new Color(142, 163, 174),
            BackgroundBrush = new GlassBackdropBrush(),
            SurfaceBrush = pane,
            SurfaceAltBrush = paneAlt,
            SelectionBrush = new GradientBrush(cyan, new Color(255, 211, 124, 192), GradientDirection.Horizontal),
            FieldFrameBrush = field
        };

        return ThemeBuilder.CreateDefinition("glass", "Frosted Glass", "atkinsonhyperlegible", ThemeEra.Glass, ThemeBrightness.Dark, palette, night,
            styleTheme: theme =>
            {
                theme.TextBoxBackgroundBrush = field;
                theme.ButtonBackgroundBrush = paneAlt;
                theme.ButtonHoverBrush = new FrostedGlassBrush(new Color(97, 242, 226, 82), new Color(255, 255, 255, 216), 18, 0.38f);
                theme.ButtonPressedBrush = new FrostedGlassBrush(new Color(255, 144, 116, 118), new Color(255, 255, 255, 190), 12, 0.26f);
                theme.ToolTipBackgroundBrush = new FrostedGlassBrush(new Color(9, 14, 28, 228), new Color(255, 255, 255, 120), 10, 0.14f);
                theme.ToolTipTextColor = Color.White;
                ThemeBuilder.Chrome(theme.Button, null, ThemeBuilder.Solid(new Color(255, 255, 255, 96)), 1, 10);
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
            });
    }
}

/// <summary>
///     The night backdrop the frosted panes blur over: base gradient, soft diagonal light
///     bands, faint underlay "cards" and a subtle grid — content for the glass to refract.
/// </summary>
public sealed class GlassBackdropBrush : Brush
{
    private readonly GradientBrush _baseGradient = new GradientBrush(
        new Color(9, 14, 28),
        new Color(42, 61, 78),
        GradientDirection.DiagonalDown);

    public override void Draw(SpriteBatch spriteBatch, Rect rect)
    {
        Draw(spriteBatch, rect, 1);
    }

    public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        _baseGradient.Draw(spriteBatch, rect, opacity);
        DrawSoftBand(spriteBatch, rect, 0.18f, 0.16f, rect.Width * 0.75f, 72, -0.32f, new Color(84, 237, 218, 128), opacity);
        DrawSoftBand(spriteBatch, rect, 0.72f, 0.18f, rect.Width * 0.7f, 64, 0.28f, new Color(255, 149, 118, 116), opacity);
        DrawSoftBand(spriteBatch, rect, 0.54f, 0.64f, rect.Width * 0.9f, 92, -0.24f, new Color(118, 131, 255, 98), opacity);
        DrawSoftBand(spriteBatch, rect, 0.22f, 0.86f, rect.Width * 0.65f, 58, 0.2f, new Color(255, 217, 128, 82), opacity);
        DrawUnderlayCards(spriteBatch, rect, opacity);
        DrawGrid(spriteBatch, rect, opacity);
    }

    private static void DrawSoftBand(SpriteBatch spriteBatch, Rect rect, float x, float y, float width, float height, float rotation, Color color, float opacity)
    {
        for (var i = -3; i <= 3; i++)
        {
            var alphaScale = 1f - System.Math.Abs(i) * 0.18f;
            var bandColor = new Color(color.R, color.G, color.B, (byte)(color.A * alphaScale));
            DrawRotatedRect(spriteBatch, rect, x, y + i * 0.01f, width, height + System.Math.Abs(i) * 16, rotation, bandColor, opacity);
        }
    }

    private static void DrawUnderlayCards(SpriteBatch spriteBatch, Rect rect, float opacity)
    {
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.05f, rect.Top + rect.Height * 0.2f, 250, 46, new Color(255, 255, 255, 35), opacity);
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.08f, rect.Top + rect.Height * 0.29f, 165, 18, new Color(84, 237, 218, 84), opacity);
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.08f, rect.Top + rect.Height * 0.34f, 220, 18, new Color(255, 255, 255, 28), opacity);

        DrawRect(spriteBatch, rect.Left + rect.Width * 0.72f, rect.Top + rect.Height * 0.2f, 210, 42, new Color(255, 149, 118, 70), opacity);
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.76f, rect.Top + rect.Height * 0.28f, 150, 18, new Color(255, 255, 255, 30), opacity);
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.78f, rect.Top + rect.Height * 0.34f, 92, 18, new Color(118, 131, 255, 60), opacity);

        DrawRect(spriteBatch, rect.Left + rect.Width * 0.38f, rect.Top + rect.Height * 0.77f, 300, 38, new Color(255, 255, 255, 28), opacity);
        DrawRect(spriteBatch, rect.Left + rect.Width * 0.42f, rect.Top + rect.Height * 0.84f, 190, 16, new Color(84, 237, 218, 58), opacity);
    }

    private static void DrawGrid(SpriteBatch spriteBatch, Rect rect, float opacity)
    {
        const int spacing = 44;
        var verticalColor = new Color(255, 255, 255, 18);
        var horizontalColor = new Color(255, 255, 255, 12);

        for (var x = rect.Left; x < rect.Right; x += spacing)
            DrawRect(spriteBatch, x, rect.Top, 1, rect.Height, verticalColor, opacity);

        for (var y = rect.Top; y < rect.Bottom; y += spacing)
            DrawRect(spriteBatch, rect.Left, y, rect.Width, 1, horizontalColor, opacity);
    }

    private static void DrawRotatedRect(SpriteBatch spriteBatch, Rect rect, float x, float y, float width, float height, float rotation, Color color, float opacity)
    {
        var position = new Vector2(rect.Left + rect.Width * x, rect.Top + rect.Height * y);
        spriteBatch.Draw(
            SolidColorBrush.Pixel,
            position,
            null,
            ApplyOpacity(color, opacity),
            rotation,
            new Vector2(0.5f, 0.5f),
            new Vector2(width, height),
            SpriteEffects.None,
            0);
    }

    private static void DrawRect(SpriteBatch spriteBatch, float left, float top, float width, float height, Color color, float opacity)
    {
        spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(left, top, width, height), ApplyOpacity(color, opacity));
    }
}

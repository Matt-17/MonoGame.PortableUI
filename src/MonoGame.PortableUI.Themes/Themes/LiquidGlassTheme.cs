using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Liquid Glass: almost clear, lens-like panes that refract a vivid wallpaper at their rounded
///     rims, catch light on the top-left edge and float on soft shadows. Pill buttons, iOS-style
///     blue accent, tinted glass for call-to-action buttons. Without shader support the panes fall
///     back to frosted acrylic.
/// </summary>
public static class LiquidGlassTheme
{
    public static ThemeDefinition Create()
    {
        var night = ThemeBuilder.Hex("#0A0E1F");
        var blue = ThemeBuilder.Hex("#0A84FF");
        var palette = new ThemePalette
        {
            Background = night,
            Surface = new Color(24, 30, 52, 120),
            SurfaceAlt = new Color(255, 255, 255, 36),
            Text = Color.White,
            HeadingText = Color.White,
            MutedText = new Color(214, 220, 238),
            Primary = blue,
            Secondary = ThemeBuilder.Hex("#BF5AF2"),
            Warning = ThemeBuilder.Hex("#FF9F0A"),
            Danger = ThemeBuilder.Hex("#FF453A"),
            Info = ThemeBuilder.Hex("#64D2FF"),
            Selection = blue,
            SelectionText = Color.White,
            TabText = new Color(232, 236, 248),
            SelectedTabText = Color.White,
            FieldFrame = new Color(0, 0, 0, 70),
            FieldBorder = new Color(255, 255, 255, 70),
            DisabledSurface = new Color(255, 255, 255, 18),
            DisabledText = new Color(160, 168, 190),
            BackgroundBrush = new LiquidWallpaperBrush(),
            SurfaceBrush = new LiquidGlassBrush(new Color(18, 22, 42, 70)) { Frost = 0.35f },
            SurfaceAltBrush = new LiquidGlassBrush(new Color(255, 255, 255, 26)),
            SelectionBrush = ThemeBuilder.Solid(new Color(10, 132, 255, 210)),
            FieldFrameBrush = new LiquidGlassBrush(new Color(0, 0, 0, 70)) { Frost = 0.6f, Refraction = 6, Bezel = 8 }
        };

        return ThemeBuilder.CreateDefinition("liquid", "Liquid Glass", "roboto", ThemeEra.Glass, ThemeBrightness.Dark, palette, night,
            styleTheme: theme =>
            {
                const float pill = 19;
                var clear = new LiquidGlassBrush(new Color(255, 255, 255, 30)) { Refraction = 10, Bezel = 12 };
                ThemeBuilder.Chrome(theme.Button, clear, null, 0, pill);
                theme.ButtonBackgroundBrush = clear;
                theme.ButtonTextColor = Color.White;
                theme.ButtonHoverTextColor = Color.White;
                theme.ButtonPressedTextColor = Color.White;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 34));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 50));
                theme.Button.InvalidateResolvedCache();

                Tinted(theme.PrimaryButton, new Color(10, 132, 255, 175), pill);
                Tinted(theme.SecondaryButton, new Color(255, 255, 255, 52), pill);
                Tinted(theme.DangerButton, new Color(255, 69, 58, 175), pill);
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 36));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 60));

                // Cards: big rounded corners; the glass draws its own rim.
                ThemeBuilder.Chrome(theme.Panel, null, null, 0, 26);

                var field = palette.FieldFrameBrush!;
                foreach (var style in new[] { theme.TextBox, theme.ListBox, theme.ComboBox })
                    ThemeBuilder.Chrome(style, field, null, 0, 12);
                ThemeBuilder.Chrome(theme.ComboBox, clear, null, 0, pill);
                theme.TextBoxBackgroundBrush = field;
                theme.TextBoxTextColor = Color.White;
                theme.TextBoxHintTextColor = new Color(190, 198, 220);
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(blue);
                theme.TextBoxSelectionBrush = ThemeBuilder.Solid(new Color(10, 132, 255, 120));
                theme.ListBoxBackgroundBrush = field;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxItemTextColor = Color.White;
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Solid(new Color(10, 132, 255, 200));
                theme.ComboBoxDropDownBackgroundBrush = new LiquidGlassBrush(new Color(14, 18, 36, 190)) { Frost = 0.8f };
                theme.ComboBoxGlyphColor = Color.White;

                theme.TabHeaderBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 46));
                theme.TabHeaderTextColor = new Color(214, 220, 238);
                theme.TabSelectedHeaderTextColor = Color.White;

                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 30));
                theme.ProgressBarFillBrush = new LinearGradientBrush(new GradientStop(0, blue), new GradientStop(1, ThemeBuilder.Hex("#64D2FF"))) { AngleDegrees = 0 };
                theme.SliderTrackBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 40));
                theme.SliderFillBrush = ThemeBuilder.Solid(blue);
                theme.SliderThumbBrush = ThemeBuilder.Solid(Color.White);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 120));
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ScrollBarBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 110));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 170));
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 30));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 150));
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(blue);
                theme.RadioButtonDotBrush = ThemeBuilder.Solid(blue);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 46));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#30D158"));
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(Color.White);
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 26));
                theme.DataGridHeaderTextColor = Color.White;
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 10));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 22));
                theme.ToolTipBackgroundBrush = new LiquidGlassBrush(new Color(14, 18, 36, 200)) { Frost = 0.8f };
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = Color.White;
                theme.ProgressIndicatorForeground = blue;
                theme.FocusBorderBrush = ThemeBuilder.Solid(new Color(100, 210, 255, 200));
                theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 4), Blur = 10 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 14), Blur = 30 };
                // Dialogs: denser, darker glass with a wide refracting rim, so the UI behind reads as
                // light and colour, not as competing text.
                theme.ModalBackgroundBrush = new LiquidGlassBrush(new Color(16, 20, 42, 165)) { Frost = 0.95f, Refraction = 18, Bezel = 22, Highlight = 0.7f };
                theme.ModalShadow = new ShadowStyle { Color = new Color(0, 0, 0, 140), Offset = new Vector2(0, 20), Blur = 40 };
            });
    }

    private static void Tinted(ControlStyle style, Color tint, float radius)
    {
        ThemeBuilder.Chrome(style, new LiquidGlassBrush(tint) { Refraction = 10, Bezel = 12, Frost = 0.75f }, null, 0, radius);
        style.Normal.TextColor = Color.White;
        style.Disabled.Background = new LiquidGlassBrush(new Color(255, 255, 255, 14)) { Refraction = 6, Bezel = 10 };
        style.Disabled.TextColor = new Color(160, 168, 190);
        style.InvalidateResolvedCache();
    }
}

/// <summary>
///     A vivid wallpaper for the liquid panes to refract: deep night gradient, big soft colour
///     orbs and a few crisp light lines (edges are what makes refraction visible).
/// </summary>
public sealed class LiquidWallpaperBrush : Brush
{
    private readonly GradientBrush _base = new(ThemeBuilder.Hex("#0B1030"), ThemeBuilder.Hex("#1A0B2E"), GradientDirection.DiagonalDown);
    private readonly RadialGradientBrush _blue = Orb(ThemeBuilder.Hex("#2F6BFF"));
    private readonly RadialGradientBrush _pink = Orb(ThemeBuilder.Hex("#FF3D9A"));
    private readonly RadialGradientBrush _amber = Orb(ThemeBuilder.Hex("#FFB547"));
    private readonly RadialGradientBrush _teal = Orb(ThemeBuilder.Hex("#21D4C4"));

    /// <summary>Lets the orbs drift slowly (the glass then visibly blurs/refracts a living scene). Default on.</summary>
    public bool Animated { get; set; } = true;

    /// <summary>Drift speed multiplier.</summary>
    public float Speed { get; set; } = 1f;

    private float _time;

    private static RadialGradientBrush Orb(Color color) =>
        new(new GradientStop(0, color), new GradientStop(0.45f, new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)150)), new GradientStop(1, Color.Transparent));

    public override void Draw(SpriteBatch spriteBatch, Rect rect) => Draw(spriteBatch, rect, 1);

    public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        _base.Draw(spriteBatch, rect, opacity);
        _time = Animated ? (float)MonoGame.PortableUI.Common.ScreenSystem.TotalTime.TotalSeconds * Speed : 0;
        if (Animated)
            ScreenEngine.RequestAnimationFrame();
        var size = Math.Max(rect.Width, rect.Height);
        DrawOrb(spriteBatch, _blue, rect, 0.12f, 0.2f, size * 0.62f, opacity, 0);
        DrawOrb(spriteBatch, _pink, rect, 0.78f, 0.25f, size * 0.55f, opacity, 1);
        DrawOrb(spriteBatch, _amber, rect, 0.62f, 0.95f, size * 0.5f, opacity, 2);
        DrawOrb(spriteBatch, _teal, rect, 0.2f, 0.92f, size * 0.42f, opacity, 3);

        // Crisp diagonal light lines: refraction bends them visibly at every glass rim.
        var pixel = Primitives.Pixel(spriteBatch);
        for (var i = 0; i < 9; i++)
        {
            var x = rect.Left + rect.Width * (i / 8f) - rect.Height * 0.3f;
            spriteBatch.Draw(pixel, new Vector2(x, rect.Top), null, ApplyOpacity(new Color(255, 255, 255, i % 3 == 0 ? 70 : 34), opacity),
                0.35f, Vector2.Zero, new Vector2(i % 3 == 0 ? 3 : 1.5f, rect.Height * 1.3f), SpriteEffects.None, 0);
        }
    }

    private void DrawOrb(SpriteBatch spriteBatch, Brush orb, Rect rect, float x, float y, float diameter, float opacity, int phase)
    {
        // Slow Lissajous drift, each orb on its own phase and pace.
        var t = _time * (0.05f + phase * 0.013f) + phase * 1.9f;
        var cx = rect.Left + rect.Width * (x + 0.09f * MathF.Sin(t));
        var cy = rect.Top + rect.Height * (y + 0.07f * MathF.Cos(t * 1.3f));
        orb.Draw(spriteBatch, new Rect(cx - diameter / 2, cy - diameter / 2, diameter, diameter), opacity);
    }
}

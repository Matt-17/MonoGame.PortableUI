using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Themes;

/// <summary>
///     Frosted glass, light: milky white panes with a heavy blur over a bright pastel wallpaper,
///     dark crisp text, a clear blue accent, rounded cards on soft shadows — the airy look of
///     current mobile and desktop systems.
/// </summary>
public static class GlassTheme
{
    public static ThemeDefinition Create()
    {
        var ink = ThemeBuilder.Hex("#1C1C1E");
        var blue = ThemeBuilder.Hex("#007AFF");
        // Milky frost: strong blur, white tint, a bright sheen on the top edge, fine grain.
        var pane = new FrostedGlassBrush(new Color(255, 255, 255, 150), new Color(255, 255, 255, 230), 30, 0.18f);
        var paneAlt = new FrostedGlassBrush(new Color(255, 255, 255, 185), new Color(255, 255, 255, 240), 26, 0.14f);
        var field = new FrostedGlassBrush(new Color(255, 255, 255, 205), new Color(255, 255, 255, 200), 20, 0.1f);
        var palette = new ThemePalette
        {
            Background = ThemeBuilder.Hex("#EEF0F8"),
            Surface = new Color(255, 255, 255, 150),
            SurfaceAlt = new Color(255, 255, 255, 185),
            Text = ink,
            HeadingText = ink,
            MutedText = ThemeBuilder.Hex("#55556A"),
            Primary = blue,
            Secondary = ThemeBuilder.Hex("#5856D6"),
            Warning = ThemeBuilder.Hex("#FF9500"),
            Danger = ThemeBuilder.Hex("#FF3B30"),
            Info = ThemeBuilder.Hex("#32ADE6"),
            Selection = blue,
            SelectionText = Color.White,
            TabText = ThemeBuilder.Hex("#3A3A48"),
            SelectedTabText = ink,
            FieldFrame = new Color(255, 255, 255, 205),
            FieldBorder = new Color(0, 0, 0, 28),
            DisabledSurface = new Color(255, 255, 255, 90),
            DisabledText = ThemeBuilder.Hex("#9A9AAA"),
            BackgroundBrush = new GlassBackdropBrush(),
            SurfaceBrush = pane,
            SurfaceAltBrush = paneAlt,
            SelectionBrush = ThemeBuilder.Solid(blue),
            FieldFrameBrush = field
        };

        return ThemeBuilder.CreateDefinition("glass", "Frosted Glass", "roboto", ThemeEra.Glass, ThemeBrightness.Light, palette, palette.Background,
            styleTheme: theme =>
            {
                const float radius = 12;
                var hairline = ThemeBuilder.Solid(new Color(255, 255, 255, 170));
                ThemeBuilder.Chrome(theme.Button, paneAlt, hairline, 1, radius);
                theme.ButtonBackgroundBrush = paneAlt;
                theme.ButtonTextColor = ink;
                theme.ButtonHoverTextColor = ink;
                theme.ButtonPressedTextColor = ink;
                theme.ButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 90));
                theme.ButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 22));
                theme.Button.InvalidateResolvedCache();

                // Call-to-action buttons are solid colour with a soft top light, like filled system buttons.
                Filled(theme.PrimaryButton, "#2B8CFF", "#0066E0", radius);
                Filled(theme.SecondaryButton, "#7472E8", "#4B49C8", radius);
                Filled(theme.DangerButton, "#FF5A50", "#E5251B", radius);
                theme.VariantButtonHoverBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 40));
                theme.VariantButtonPressedBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 40));

                // Cards: rounded, with a hairline light edge.
                ThemeBuilder.Chrome(theme.Panel, null, null, 0, 22);

                var fieldBorder = ThemeBuilder.Solid(new Color(0, 0, 0, 26));
                foreach (var style in new[] { theme.TextBox, theme.ListBox })
                    ThemeBuilder.Chrome(style, field, fieldBorder, 1, 10);
                ThemeBuilder.Chrome(theme.ComboBox, paneAlt, hairline, 1, radius);
                theme.TextBoxBackgroundBrush = field;
                theme.TextBoxTextColor = ink;
                theme.TextBoxHintTextColor = ThemeBuilder.Hex("#8E8E9A");
                theme.TextBoxCursorBrush = ThemeBuilder.Solid(blue);
                theme.TextBoxSelectionBrush = ThemeBuilder.Solid(new Color(0, 122, 255, 70));
                theme.ListBoxBackgroundBrush = field;
                theme.ListBoxItemBackgroundBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ListBoxItemTextColor = ink;
                theme.ListBoxSelectedItemBackgroundBrush = ThemeBuilder.Solid(blue);
                theme.ListBoxSelectedItemTextColor = Color.White;
                theme.ComboBoxDropDownBackgroundBrush = new FrostedGlassBrush(new Color(255, 255, 255, 220), new Color(255, 255, 255, 240), 24, 0.1f);
                theme.ComboBoxGlyphColor = ThemeBuilder.Hex("#3A3A48");
                theme.ContextMenuBackgroundBrush = theme.ComboBoxDropDownBackgroundBrush;

                // Tabs as a segmented control: frosted strip, the selected segment bright white.
                theme.TabHeaderBackgroundBrush = new FrostedGlassBrush(new Color(255, 255, 255, 110), new Color(255, 255, 255, 200), 24, 0.12f);
                theme.TabSelectedHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 240));
                theme.TabHeaderTextColor = ThemeBuilder.Hex("#3A3A48");
                theme.TabSelectedHeaderTextColor = ink;

                theme.ProgressBarBackgroundBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 22));
                theme.ProgressBarFillBrush = new LinearGradientBrush(new GradientStop(0, blue), new GradientStop(1, ThemeBuilder.Hex("#5AC8FA"))) { AngleDegrees = 0 };
                theme.SliderTrackBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 30));
                theme.SliderFillBrush = ThemeBuilder.Solid(blue);
                theme.SliderThumbBrush = ThemeBuilder.Solid(Color.White);
                theme.SliderThumbBorderBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 40));
                theme.ScrollBarGutterBrush = ThemeBuilder.Solid(Color.Transparent);
                theme.ScrollBarBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 70));
                theme.ScrollBarHoverBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 110));
                theme.CheckBoxBoxBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 220));
                theme.CheckBoxBoxBorderBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 70));
                theme.CheckBoxBoxBorderWidth = 1;
                theme.CheckBoxCheckMarkBrush = ThemeBuilder.Solid(blue);
                theme.CheckBoxTextColor = ink;
                theme.RadioButtonDotBrush = ThemeBuilder.Solid(blue);
                theme.ToggleSwitchOffTrackBrush = ThemeBuilder.Solid(new Color(120, 120, 128, 70));
                theme.ToggleSwitchOnTrackBrush = ThemeBuilder.Solid(ThemeBuilder.Hex("#34C759"));
                theme.ToggleSwitchKnobBrush = ThemeBuilder.Solid(Color.White);
                theme.DataGridHeaderBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 150));
                theme.DataGridHeaderTextColor = ThemeBuilder.Hex("#3A3A48");
                theme.DataGridAlternateRowBackgroundBrush = ThemeBuilder.Solid(new Color(255, 255, 255, 70));
                theme.DataGridGridLinesBrush = ThemeBuilder.Solid(new Color(0, 0, 0, 18));
                theme.ToolTipBackgroundBrush = new FrostedGlassBrush(new Color(40, 40, 46, 225), new Color(255, 255, 255, 60), 12, 0.08f);
                theme.ToolTipBorderBrush = null;
                theme.ToolTipTextColor = Color.White;
                theme.ProgressIndicatorForeground = blue;
                theme.FocusBorderBrush = ThemeBuilder.Solid(new Color(0, 122, 255, 170));
                theme.ButtonShadow = new ShadowStyle { Color = new Color(40, 50, 90, 26), Offset = new Vector2(0, 2), Blur = 6 };
                theme.PanelShadow = new ShadowStyle { Color = new Color(40, 50, 90, 46), Offset = new Vector2(0, 12), Blur = 30 };
            });
    }

    private static void Filled(ControlStyle style, string top, string bottom, float radius)
    {
        ThemeBuilder.Chrome(style, ThemeBuilder.Gloss((0, top), (1, bottom)), null, 0, radius);
        style.Normal.TextColor = Color.White;
        style.Disabled.Background = ThemeBuilder.Solid(new Color(255, 255, 255, 110));
        style.Disabled.TextColor = ThemeBuilder.Hex("#9A9AAA");
        style.InvalidateResolvedCache();
    }
}

/// <summary>
///     The bright wallpaper the frosted panes blur: a soft pastel gradient with large colour orbs,
///     so the frost picks up gentle colour shifts as panes move across it.
/// </summary>
public sealed class GlassBackdropBrush : Brush
{
    private readonly GradientBrush _base = new(ThemeBuilder.Hex("#F4EEFF"), ThemeBuilder.Hex("#E6F2FF"), GradientDirection.DiagonalDown);
    private readonly RadialGradientBrush _peach = Orb(ThemeBuilder.Hex("#FFB199"));
    private readonly RadialGradientBrush _lilac = Orb(ThemeBuilder.Hex("#B69CFF"));
    private readonly RadialGradientBrush _sky = Orb(ThemeBuilder.Hex("#7CC6FF"));
    private readonly RadialGradientBrush _mint = Orb(ThemeBuilder.Hex("#8BE3C9"));

    private static RadialGradientBrush Orb(Color color) =>
        new(new GradientStop(0, color), new GradientStop(0.5f, new Color((byte)color.R, (byte)color.G, (byte)color.B, (byte)140)), new GradientStop(1, Color.Transparent));

    public override void Draw(SpriteBatch spriteBatch, Rect rect) => Draw(spriteBatch, rect, 1);

    public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        _base.Draw(spriteBatch, rect, opacity);
        var size = Math.Max(rect.Width, rect.Height);
        DrawOrb(spriteBatch, _peach, rect, 0.85f, 0.15f, size * 0.6f, opacity);
        DrawOrb(spriteBatch, _lilac, rect, 0.1f, 0.35f, size * 0.55f, opacity);
        DrawOrb(spriteBatch, _sky, rect, 0.55f, 0.9f, size * 0.6f, opacity);
        DrawOrb(spriteBatch, _mint, rect, 0.95f, 0.85f, size * 0.38f, opacity);
    }

    private static void DrawOrb(SpriteBatch spriteBatch, Brush orb, Rect rect, float x, float y, float diameter, float opacity)
    {
        var cx = rect.Left + rect.Width * x;
        var cy = rect.Top + rect.Height * y;
        orb.Draw(spriteBatch, new Rect(cx - diameter / 2, cy - diameter / 2, diameter, diameter), opacity);
    }
}

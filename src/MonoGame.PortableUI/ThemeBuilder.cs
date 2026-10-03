using System;

using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI;

/// <summary>
///     Helpers for writing themes. Lives in the core library so every file of the
///     MonoGame.PortableUI.Themes catalog is self-contained: copy one theme file (plus its font,
///     if it uses one) into your project and adjust it — no other file from the catalog is needed.
///     Each theme calls <see cref="CreateDefinition"/> (explicit palette) or <see cref="Catalog"/>
///     (compact hex palette) and applies its chrome in a <c>styleTheme</c> callback.
/// </summary>
public static class ThemeBuilder
{
    /// <summary>Builds a definition from an explicit palette; <paramref name="styleTheme"/> applies per-theme chrome, shadows and post effects.</summary>
    public static ThemeDefinition CreateDefinition(
        string id,
        string displayName,
        string fontName,
        ThemeEra era,
        ThemeBrightness brightness,
        ThemePalette palette,
        Color clearColor,
        Action<PortableTheme>? styleTheme = null,
        bool reducedMotion = false)
    {
        return new ThemeDefinition
        {
            Id = id,
            DisplayName = displayName,
            FontName = fontName,
            Palette = palette,
            Metadata = new ThemeMetadata
            {
                Era = era,
                Brightness = brightness,
                ReducedMotion = reducedMotion,
                Description = displayName,
                PreviewSwatches = new[] { palette.Background, palette.Surface, palette.Primary, palette.Secondary, palette.Selection }
            },
            CreateTheme = () =>
            {
                var theme = PortableTheme.FromPalette(palette);
                theme.Typography.FontName = fontName;
                theme.Cursor = DefaultCursor(era, palette);
                ApplyEraCaret(theme, era);
                styleTheme?.Invoke(theme);
                return theme;
            },
            ClearColor = clearColor,
            BackgroundColor = palette.Background
        };
    }

    /// <summary>
    ///     Compact builder for hex-based catalog themes: derives the full 19-slot palette
    ///     (muted/disabled/danger colors, glass brushes) from eight anchor colors.
    /// </summary>
    public static ThemeDefinition Catalog(
        string id,
        string displayName,
        string fontName,
        ThemeEra era,
        ThemeBrightness brightness,
        string background,
        string surface,
        string surfaceAlt,
        string text,
        string primary,
        string secondary,
        string selection,
        string selectionText,
        Action<PortableTheme>? styleTheme = null,
        bool reducedMotion = false,
        bool glass = false,
        bool liquid = false,
        string? danger = null,
        Brush? backgroundBrush = null,
        Brush? surfaceBrush = null)
    {
        var backgroundColor = Hex(background);
        var surfaceColor = Hex(surface);
        var surfaceAltColor = Hex(surfaceAlt);
        var textColor = Hex(text);
        var primaryColor = Hex(primary);
        var secondaryColor = Hex(secondary);
        var selectionColor = Hex(selection);
        var selectionTextColor = Hex(selectionText);
        var palette = new ThemePalette
        {
            Background = backgroundColor,
            Surface = surfaceColor,
            SurfaceAlt = surfaceAltColor,
            Text = textColor,
            HeadingText = textColor,
            MutedText = ReadableMuted(textColor, backgroundColor, surfaceColor),
            Primary = primaryColor,
            Secondary = secondaryColor,
            Warning = secondaryColor,
            Danger = danger != null ? Hex(danger) : Mix(secondaryColor, Color.Red, 0.45f),
            Info = primaryColor,
            Selection = selectionColor,
            SelectionText = selectionTextColor,
            TabText = textColor,
            SelectedTabText = selectionTextColor,
            FieldFrame = surfaceAltColor,
            FieldBorder = primaryColor,
            DisabledSurface = Mix(surfaceColor, backgroundColor, 0.5f),
            DisabledText = Mix(textColor, backgroundColor, 0.45f),
            BackgroundBrush = backgroundBrush ?? (glass ? new GradientBrush(backgroundColor, Mix(primaryColor, backgroundColor, 0.78f), GradientDirection.DiagonalDown) : null),
            SurfaceBrush = surfaceBrush ?? (liquid ? new LiquidGlassBrush() : glass ? new AcrylicBrush(new Color((byte)surfaceColor.R, (byte)surfaceColor.G, (byte)surfaceColor.B, (byte)150)) : null),
            SurfaceAltBrush = glass ? new AcrylicBrush(new Color((byte)surfaceAltColor.R, (byte)surfaceAltColor.G, (byte)surfaceAltColor.B, (byte)168)) : null,
            SelectionBrush = new LinearGradientBrush(new GradientStop(0, selectionColor), new GradientStop(1, primaryColor)) { AngleDegrees = 0 },
            FieldFrameBrush = glass ? new AcrylicBrush(new Color((byte)surfaceAltColor.R, (byte)surfaceAltColor.G, (byte)surfaceAltColor.B, (byte)160)) : null
        };

        return new ThemeDefinition
        {
            Id = id,
            DisplayName = displayName,
            FontName = fontName,
            Palette = palette,
            Metadata = new ThemeMetadata
            {
                Era = era,
                Brightness = brightness,
                ReducedMotion = reducedMotion,
                Description = displayName,
                PreviewSwatches = new[] { palette.Background, palette.Surface, palette.Primary, palette.Secondary, palette.Selection }
            },
            CreateTheme = () =>
            {
                var theme = PortableTheme.FromPalette(palette);
                theme.Typography.FontName = fontName;
                theme.Cursor = DefaultCursor(era, palette);
                ApplyEraCaret(theme, era);
                styleTheme?.Invoke(theme);
                if (reducedMotion)
                {
                    theme.Button.TransitionDuration = TimeSpan.Zero;
                    theme.TextBox.TransitionDuration = TimeSpan.Zero;
                }
                if (liquid)
                {
                    theme.Panel.Normal.CornerRadius = 20;
                }
                return theme;
            },
            ClearColor = backgroundColor,
            BackgroundColor = backgroundColor
        };
    }

    /// <summary>Sets background/border/corner radius on a control style's normal state.</summary>
    public static void Chrome(ControlStyle style, Brush? background, Brush? border, float borderWidth, float radius)
    {
        if (background != null)
            style.Normal.Background = background;
        style.Normal.BorderBrush = border ?? style.Normal.BorderBrush;
        style.Normal.BorderThickness = new Thickness(border == null ? 0 : borderWidth);
        style.Normal.CornerRadius = radius;
        style.InvalidateResolvedCache();
    }

    /// <summary>Classic raised-bevel button chrome (Win95/NeXT/Amiga): raised face, sunken when pressed.</summary>
    public static void Bevel(PortableTheme theme, Color face, Color outerLight, Color innerLight, Color innerDark, Color outerDark)
    {
        var raised = new BevelBrush(face, outerLight, innerLight, innerDark, outerDark);
        Chrome(theme.Button, raised, null, 0, 0);
        theme.Button.Pressed.Background = raised.AsSunken();
        theme.ButtonBackgroundBrush = raised;
        theme.Button.InvalidateResolvedCache();
    }

    /// <summary>
    ///     Styles the Primary/Secondary/Danger button slots: <paramref name="style"/> gets each slot and
    ///     its palette color (Primary, Secondary, Danger).
    /// </summary>
    public static void Variants(PortableTheme theme, Action<ControlStyle, Color> style)
    {
        style(theme.PrimaryButton, theme.Palette.Primary);
        style(theme.SecondaryButton, theme.Palette.Secondary);
        style(theme.DangerButton, theme.Palette.Danger);
        theme.PrimaryButton.InvalidateResolvedCache();
        theme.SecondaryButton.InvalidateResolvedCache();
        theme.DangerButton.InvalidateResolvedCache();
    }

    /// <summary>A vertical gradient from explicit (position, hex) stops; equal positions make a hard gloss edge.</summary>
    public static LinearGradientBrush Gloss(params (float Position, string Color)[] stops)
    {
        var gradientStops = new GradientStop[stops.Length];
        for (var i = 0; i < stops.Length; i++)
            gradientStops[i] = new GradientStop(stops[i].Position, Hex(stops[i].Color));
        return new LinearGradientBrush(gradientStops);
    }

    /// <summary>
    ///     The pointer that fits an era: chunky pixel arrows for 8-bit machines, the 1-bit arrow for
    ///     classic desktops and terminals, the fine anti-aliased arrow for everything modern.
    ///     Applied before <c>styleTheme</c>, so a theme can still set its own.
    /// </summary>
    public static CursorStyle DefaultCursor(ThemeEra era, ThemePalette palette)
    {
        return era switch
        {
            ThemeEra.Retro => CursorStyle.PixelArrow(Color.White, Color.Black),
            ThemeEra.Terminal => CursorStyle.TextCell(),
            ThemeEra.Desktop => CursorStyle.Arrow(Color.White, Color.Black),
            _ => CursorStyle.ModernArrow(Color.White, Color.Black)
        };
    }

    /// <summary>Text-mode themes get the DOS caret: underline / overwrite block, blinking at ~3.75 Hz.</summary>
    public static void ApplyEraCaret(PortableTheme theme, ThemeEra era)
    {
        if (era != ThemeEra.Terminal)
            return;
        theme.TextBoxCaretStyle = Controls.CaretStyle.TextMode;
        theme.TextBoxCaretBlinkInterval = TimeSpan.FromMilliseconds(133);
    }

    public static Color Hex(string value)
    {
        var hex = value.TrimStart('#');
        return new Color(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

    public static Color Mix(Color first, Color second, float amount)
    {
        amount = MathHelper.Clamp(amount, 0, 1);
        return Color.Lerp(first, second, amount);
    }

    public static SolidColorBrush Solid(Color color)
    {
        return new SolidColorBrush(color);
    }

    /// <summary>Muted text = text mixed toward the background, backing off until it stays readable on the surface.</summary>
    public static Color ReadableMuted(Color text, Color background, Color surface)
    {
        var mix = 0.35f;
        var muted = Mix(text, background, mix);
        while (mix > 0 && ContrastRatio(muted, surface) < 2.45)
        {
            mix -= 0.08f;
            muted = Mix(text, background, Math.Max(0, mix));
        }

        return muted;
    }

    private static double ContrastRatio(Color a, Color b)
    {
        static double Luminance(Color color)
        {
            static double Channel(byte value)
            {
                var c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }

        var la = Luminance(a);
        var lb = Luminance(b);
        var lighter = Math.Max(la, lb);
        var darker = Math.Min(la, lb);
        return (lighter + 0.05) / (darker + 0.05);
    }
}

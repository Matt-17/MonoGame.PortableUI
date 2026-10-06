using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public sealed class Typography
    {
        public string FontName { get; set; } = "default";
        public float TextSize { get; set; } = 14;
        /// <summary>Text size of <see cref="Controls.TextBlock"/>s with <c>IsHeading</c> set.</summary>
        public float HeadingSize { get; set; } = 16;

        /// <summary>Outline of every text block under this theme that sets no <see cref="Controls.TextBlock.Stroke"/> itself (null = none).</summary>
        public Media.TextStroke? TextStroke { get; set; }

        /// <summary>Outer glow of every text block under this theme that sets no <see cref="Controls.TextBlock.Glow"/> itself (null = none).</summary>
        public Media.TextGlow? TextGlow { get; set; }

        private Text.UIFont? _dynamicFont;

        /// <summary>
        ///     Runtime-rasterizing font of this theme: text controls under it (a screen, a
        ///     <see cref="UISurface"/>, a <see cref="Controls.ThemeIsland"/>) use it instead of
        ///     <see cref="FontManager.DefaultDynamicFont"/>, so surfaces with different themes draw
        ///     their own fonts side by side. A control's own <c>DynamicFont</c> still wins.
        /// </summary>
        public Text.UIFont? DynamicFont
        {
            get => _dynamicFont;
            set
            {
                if (ReferenceEquals(_dynamicFont, value))
                    return;
                _dynamicFont = value;
                // Screens re-measure their text on the next update, as for the default font.
                FontManager.NotifyDynamicFontChanged();
            }
        }
    }

    public sealed class ThemeMetrics
    {
        public Thickness ControlPadding { get; set; } = new Thickness(8, 6);
        public float ControlHeight { get; set; } = 32;
        public float CornerRadius { get; set; }
        public float BorderWidth { get; set; } = 1;
        public float Spacing { get; set; } = 8;
    }

    public sealed class StateStyle
    {
        private Brush? _background;
        private Brush? _borderBrush;
        private Thickness? _borderThickness;
        private CornerRadius? _cornerRadius;
        private ShadowStyle[]? _shadows;
        private Color? _textColor;
        private FocusVisualKind? _focusVisualKind;

        /// <summary>Bumped by every setter so <see cref="ControlStyle"/> can detect runtime
        /// mutation and drop its resolved cache automatically.</summary>
        internal int Version { get; private set; }

        public Brush? Background { get => _background; set { _background = value; Version++; } }
        public Brush? BorderBrush { get => _borderBrush; set { _borderBrush = value; Version++; } }
        public Thickness? BorderThickness { get => _borderThickness; set { _borderThickness = value; Version++; } }
        public CornerRadius? CornerRadius { get => _cornerRadius; set { _cornerRadius = value; Version++; } }
        public ShadowStyle[]? Shadows { get => _shadows; set { _shadows = value; Version++; } }
        public Color? TextColor { get => _textColor; set { _textColor = value; Version++; } }
        public FocusVisualKind? FocusVisualKind { get => _focusVisualKind; set { _focusVisualKind = value; Version++; } }

        public StateStyle Resolve(StateStyle normal)
        {
            if (ReferenceEquals(this, normal))
                return this;

            return new StateStyle
            {
                Background = Background ?? normal.Background,
                BorderBrush = BorderBrush ?? normal.BorderBrush,
                BorderThickness = BorderThickness ?? normal.BorderThickness,
                CornerRadius = CornerRadius ?? normal.CornerRadius,
                Shadows = Shadows ?? normal.Shadows,
                TextColor = TextColor ?? normal.TextColor,
                FocusVisualKind = FocusVisualKind ?? normal.FocusVisualKind
            };
        }
    }

    public sealed class ControlStyle
    {
        private StateStyle?[]? _resolvedCache;
        private int _cachedVersion = -1;
        private int _slotVersion;

        private StateStyle _normal = new StateStyle();
        private StateStyle _hover = new StateStyle();
        private StateStyle _pressed = new StateStyle();
        private StateStyle _focused = new StateStyle();
        private StateStyle _disabled = new StateStyle();
        private StateStyle _checked = new StateStyle();

        public StateStyle Normal { get => _normal; set { _normal = value; _slotVersion++; } }
        public StateStyle Hover { get => _hover; set { _hover = value; _slotVersion++; } }
        public StateStyle Pressed { get => _pressed; set { _pressed = value; _slotVersion++; } }
        public StateStyle Focused { get => _focused; set { _focused = value; _slotVersion++; } }
        public StateStyle Disabled { get => _disabled; set { _disabled = value; _slotVersion++; } }
        public StateStyle Checked { get => _checked; set { _checked = value; _slotVersion++; } }
        /// <summary>Duration of state animations (the button press/release animation); zero turns them off
        /// (reduced motion).</summary>
        public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromMilliseconds(120);

        // Versions only ever increment, so the sum strictly increases on any mutation.
        private int CurrentVersion =>
            _slotVersion + _normal.Version + _hover.Version + _pressed.Version +
            _focused.Version + _disabled.Version + _checked.Version;

        /// <summary>
        ///     Cached per-state resolution used by controls every frame. The cache drops itself
        ///     when any state style is replaced or mutated — no manual invalidation needed.
        /// </summary>
        public StateStyle GetResolved(ControlVisualState state)
        {
            var version = CurrentVersion;
            if (_resolvedCache is null || _cachedVersion != version)
            {
                _resolvedCache = new StateStyle?[6];
                _cachedVersion = version;
            }

            var index = (int)state;
            if (index < 0 || index >= _resolvedCache.Length)
                return Resolve(state);
            return _resolvedCache[index] ??= Resolve(state);
        }

        /// <summary>Kept for compatibility; the cache now invalidates itself on mutation.</summary>
        public void InvalidateResolvedCache()
        {
            _resolvedCache = null;
        }

        public StateStyle Resolve(ControlVisualState state)
        {
            switch (state)
            {
                case ControlVisualState.Hover:
                    return Hover.Resolve(Normal);
                case ControlVisualState.Pressed:
                    return Pressed.Resolve(Normal);
                case ControlVisualState.Focused:
                    return Focused.Resolve(Normal);
                case ControlVisualState.Disabled:
                    return Disabled.Resolve(Normal);
                case ControlVisualState.Checked:
                    return Checked.Resolve(Normal);
                default:
                    return Normal;
            }
        }
    }

    public enum ControlVisualState
    {
        Normal,
        Hover,
        Pressed,
        Focused,
        Disabled,
        Checked
    }

    public abstract class PostEffect
    {
        protected PostEffect(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     True for an effect of the display rather than the look (CRT curvature): it only applies in
        ///     <see cref="ScreenEngineOptions.PostEffects"/> and is ignored in a theme's list, so the curve
        ///     that input follows has a single owner.
        /// </summary>
        public virtual bool IsDisplayOnly => false;

        /// <summary>The UI point under <paramref name="point"/> of the shown picture - the inverse of the
        /// effect's geometry. Identity for effects that do not move pixels.</summary>
        public virtual PointF DisplayToUi(PointF point, Rect screen) => point;

        /// <summary>Where UI point <paramref name="point"/> appears in the shown picture.</summary>
        public virtual PointF UiToDisplay(PointF point, Rect screen) => point;
    }

    /// <summary>Pointer mapping through a list of effects (the ones that move pixels: curvature).</summary>
    internal static class PostEffectGeometry
    {
        public static bool MovesPixels(IReadOnlyList<PostEffect>? effects)
        {
            if (effects == null)
                return false;
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i] is { Enabled: true, IsDisplayOnly: true })
                    return true;
            }
            return false;
        }

        /// <summary>Undoes the effects in reverse order (the picture applied them in list order).</summary>
        public static PointF DisplayToUi(IReadOnlyList<PostEffect>? effects, PointF point, Rect screen)
        {
            if (effects == null)
                return point;
            for (var i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i].Enabled)
                    point = effects[i].DisplayToUi(point, screen);
            }
            return point;
        }

        public static PointF UiToDisplay(IReadOnlyList<PostEffect>? effects, PointF point, Rect screen)
        {
            if (effects == null)
                return point;
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i].Enabled)
                    point = effects[i].UiToDisplay(point, screen);
            }
            return point;
        }
    }

    /// <summary>
    ///     Which post-effect stages an engine draws (<see cref="ScreenEngineOptions.PostEffectMode"/>).
    ///     Set by the host that shows the UI, not by the UI itself: a host whose display renders
    ///     curvature and scanlines on its own (e.g. a tube shader in a 3D scene) draws the UI flat.
    /// </summary>
    public enum PostEffectMode
    {
        /// <summary>The theme's look effects and the display effects (default).</summary>
        All,

        /// <summary>Only the theme's look effects (<see cref="PortableTheme.PostEffects"/>, also in
        /// <see cref="Controls.ThemeIsland"/>s); display effects and their pointer mapping are off.</summary>
        ThemeOnly,

        /// <summary>No post effects at all: the UI is drawn straight into the target, without the
        /// extra render target and full-screen pass, and pointer input is not mapped through a barrel.</summary>
        None
    }

    public static class ControlStyleBuilder
    {
        public static IReadOnlyDictionary<string, ControlStyle> FromPalette(ThemePalette palette)
        {
            var button = CreateControlStyle(
                palette.SurfaceBrush ?? Solid(palette.Surface),
                Solid(palette.Primary),
                palette.Text,
                palette.SelectionBrush ?? Solid(palette.Selection),
                palette.SelectionText);

            var field = CreateControlStyle(
                palette.FieldFrameBrush ?? Solid(palette.FieldFrame),
                Solid(palette.FieldBorder),
                palette.Text,
                palette.SelectionBrush ?? Solid(palette.Selection),
                palette.SelectionText);

            var flat = CreateControlStyle(
                palette.SurfaceBrush ?? Solid(palette.Surface),
                Solid(palette.FieldBorder),
                palette.Text,
                Solid(palette.Primary),
                palette.SelectionText);

            return new Dictionary<string, ControlStyle>(StringComparer.OrdinalIgnoreCase)
            {
                ["Button"] = button,
                ["PrimaryButton"] = Variant(palette.Primary, palette.SelectionText, palette.DisabledSurface),
                ["SecondaryButton"] = Variant(palette.Secondary, palette.SelectionText, palette.DisabledSurface),
                ["DangerButton"] = Variant(palette.Danger, palette.SelectionText, palette.DisabledSurface),
                ["TextBox"] = field,
                ["CheckBox"] = field,
                ["RadioButton"] = field,
                ["ToggleButton"] = button,
                ["ComboBox"] = field,
                ["ListBox"] = CreateControlStyle(
                    palette.SurfaceBrush ?? Solid(palette.Surface),
                    Solid(palette.FieldBorder),
                    palette.Text,
                    palette.SelectionBrush ?? Solid(palette.Selection),
                    palette.SelectionText),
                ["ListBoxItem"] = flat,
                ["Tab"] = button,
                ["ToolTip"] = CreateControlStyle(Solid(palette.Background), Solid(palette.Primary), palette.Text, Solid(palette.SurfaceAlt), palette.Text),
                ["ContextMenu"] = flat,
                ["ScrollBar"] = CreateControlStyle(Solid(palette.Primary), Solid(palette.Primary), palette.Text, Solid(palette.Secondary), palette.Text),
                ["Slider"] = flat,
                ["ProgressBar"] = flat,
                ["Panel"] = flat
            };
        }

        public static ControlStyle CreateControlStyle(Brush background, Brush border, Color text, Brush activeBackground, Color activeText)
        {
            // Hover/Pressed intentionally leave Background/TextColor unset: interactive controls
            // (Button) composite their translucent hover/pressed overlays on top of the normal
            // background, so a replacement here would double-style them. Themes can still opt
            // into per-state replacements (e.g. Luna's orange hover ring via Hover.BorderBrush).
            return new ControlStyle
            {
                Normal = new StateStyle
                {
                    Background = background,
                    BorderBrush = border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = 0,
                    TextColor = text
                },
                Hover = new StateStyle(),
                Pressed = new StateStyle(),
                Focused = new StateStyle
                {
                    BorderBrush = border,
                    FocusVisualKind = FocusVisualKind.Rectangle
                },
                Disabled = new StateStyle
                {
                    TextColor = paletteOrDefaultDisabledText(text)
                }
            };
        }

        /// <summary>A filled call-to-action style: <paramref name="fill"/> face and border, readable text.</summary>
        public static ControlStyle Variant(Color fill, Color preferredText, Color disabledFill)
        {
            var style = CreateControlStyle(Solid(fill), Solid(fill), ReadableOn(fill, preferredText), Solid(fill), ReadableOn(fill, preferredText));
            style.Normal.BorderThickness = new Thickness(0);
            style.Disabled.Background = Solid(disabledFill);
            style.Disabled.BorderBrush = Solid(disabledFill);
            return style;
        }

        /// <summary><paramref name="preferred"/> when it reads on <paramref name="background"/>, else black or white.</summary>
        public static Color ReadableOn(Color background, Color preferred)
        {
            if (Contrast(preferred, background) >= 3)
                return preferred;
            return Contrast(Color.Black, background) >= Contrast(Color.White, background) ? Color.Black : Color.White;
        }

        private static double Contrast(Color a, Color b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        private static double Luminance(Color c)
        {
            static double Channel(byte v)
            {
                var x = v / 255.0;
                return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        private static Color paletteOrDefaultDisabledText(Color text)
        {
            return new Color(
                (byte)((text.R + 128) / 2),
                (byte)((text.G + 128) / 2),
                (byte)((text.B + 128) / 2),
                text.A);
        }

        private static SolidColorBrush Solid(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}

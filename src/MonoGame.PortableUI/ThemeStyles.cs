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
        public int TextSize { get; set; } = 14;
        public int HeadingSize { get; set; } = 16;
        public float HeadingScale { get; set; } = 1.15f;
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
        private CornerStyle? _cornerStyle;
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
        public CornerStyle? CornerStyle { get => _cornerStyle; set { _cornerStyle = value; Version++; } }
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
                CornerStyle = CornerStyle ?? normal.CornerStyle,
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
    }

    public sealed class ScanlinePostEffect : PostEffect
    {
        public ScanlinePostEffect() : base("scanlines")
        {
        }

        public float Spacing { get; set; } = 3;
        public float Strength { get; set; } = 0.18f;
    }

    public sealed class CrtBarrelPostEffect : PostEffect
    {
        public CrtBarrelPostEffect() : base("crt-barrel")
        {
        }

        public float Distortion { get; set; } = 0.08f;
        public float Vignette { get; set; } = 0.24f;
    }

    public sealed class VignettePostEffect : PostEffect
    {
        public VignettePostEffect() : base("vignette")
        {
        }

        public float Strength { get; set; } = 0.2f;
    }

    public sealed class FilmGrainPostEffect : PostEffect
    {
        public FilmGrainPostEffect() : base("film-grain")
        {
        }

        public float Strength { get; set; } = 0.04f;
    }

    public sealed class BloomPostEffect : PostEffect
    {
        public BloomPostEffect() : base("bloom")
        {
        }

        public float Strength { get; set; } = 0.25f;
        public float Threshold { get; set; } = 0.72f;
    }

    public sealed class DotMatrixPostEffect : PostEffect
    {
        public DotMatrixPostEffect() : base("dot-matrix")
        {
        }

        public float CellSize { get; set; } = 3;
        public float Strength { get; set; } = 0.18f;
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

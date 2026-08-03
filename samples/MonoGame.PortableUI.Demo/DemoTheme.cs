using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>
    ///     "Demo Theme" — a template that shows how to create your own theme with nothing but
    ///     the MonoGame.PortableUI core package. Copy this file into your project, rename it,
    ///     change the colors and overrides, and you have your own theme.
    ///
    ///     A theme is built in three steps:
    ///       1. Define a <see cref="ThemePalette"/> — 19 semantic color slots.
    ///       2. Call <see cref="PortableTheme.FromPalette"/> — it derives every control style
    ///          (buttons, text boxes, tabs, scroll bars, …) from those slots.
    ///       3. Override the few things that give the theme its character: corner radii,
    ///          borders, shadows, special brushes.
    ///
    ///     For ~40 ready-made themes see the MonoGame.PortableUI.Themes package; each of its
    ///     themes lives in its own file and can be copied and customized the same way.
    /// </summary>
    public static class DemoTheme
    {
        public const string Id = "demo";

        // The palette colors, named for readability.
        private static readonly Color Cream = new Color(250, 243, 231);
        private static readonly Color Paper = new Color(255, 251, 244);
        private static readonly Color Sand = new Color(240, 228, 210);
        private static readonly Color Ink = new Color(43, 37, 52);
        private static readonly Color Violet = new Color(124, 58, 237);
        private static readonly Color Coral = new Color(255, 122, 89);

        /// <summary>Step 1: the semantic palette — every control style is derived from these slots.</summary>
        public static ThemePalette CreatePalette()
        {
            return new ThemePalette
            {
                // Large areas: window background, panel surfaces.
                Background = Cream,
                Surface = Paper,
                SurfaceAlt = Sand,

                // Text.
                Text = Ink,
                HeadingText = Ink,
                MutedText = new Color(122, 111, 130),

                // Accents and state colors.
                Primary = Violet,
                Secondary = Coral,
                Warning = new Color(216, 141, 27),
                Danger = new Color(205, 56, 75),
                Info = new Color(36, 130, 199),

                // Selected/highlighted content.
                Selection = Violet,
                SelectionText = Paper,
                TabText = Ink,
                SelectedTabText = Paper,

                // Input fields.
                FieldFrame = Paper,
                FieldBorder = new Color(166, 138, 215),

                // Disabled state.
                DisabledSurface = new Color(226, 216, 202),
                DisabledText = new Color(150, 142, 132),

                // Optional: brushes override the flat colors above with gradients, frosted
                // glass, bevels, patterns or your own custom Brush subclass.
                SelectionBrush = new GradientBrush(Violet, Coral, GradientDirection.Horizontal)
            };
        }

        /// <summary>Steps 2 + 3: build the theme from the palette, then add the character.</summary>
        public static PortableTheme Create()
        {
            var palette = CreatePalette();
            var theme = PortableTheme.FromPalette(palette);

            // Rounded, borderless buttons …
            theme.Button.Normal.BorderThickness = new Thickness(0);
            theme.Button.Normal.CornerRadius = 12;
            theme.Button.InvalidateResolvedCache();

            // … floating on soft violet shadows.
            theme.ButtonShadow = new ShadowStyle { Color = new Color(124, 58, 237, 60), Offset = new Vector2(0, 4), Blur = 10 };
            theme.PanelShadow = new ShadowStyle { Color = new Color(60, 40, 90, 40), Offset = new Vector2(0, 6), Blur = 14 };

            // Dark tooltips for contrast on the light background.
            theme.ToolTipBackgroundBrush = new SolidColorBrush(new Color(43, 37, 52, 240));
            theme.ToolTipTextColor = Cream;

            return theme;
        }

        /// <summary>Wraps the theme in a demo preset so it appears in the demo's theme picker.</summary>
        public static DemoThemePreset CreatePreset()
        {
            return new DemoThemePreset
            {
                Id = Id,
                DisplayName = "Demo Theme",
                CreateTheme = Create,
                Palette = CreatePalette(),
                FontName = "roboto",
                ClearColor = Cream,
                BackgroundColor = Cream
            };
        }
    }
}

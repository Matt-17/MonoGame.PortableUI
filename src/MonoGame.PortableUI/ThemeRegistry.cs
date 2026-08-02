using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public static class ThemeRegistry
    {
        public const string DefaultThemeId = "default";

        private static readonly IReadOnlyList<ThemeDefinition> BuiltInThemes = new[]
        {
            CreateDefaultDefinition(),
            CreateGlass(),
            CreateC64(),
            CreateGameBoy(),
            CreateDos(),
            CreateAmiga(),
            CreateTerminal(),
            CreateStudio(),
            CreateAurora(),
            Catalog("nes", "NES 8-Bit", "pressstart2p", ThemeEra.Retro, ThemeBrightness.Dark, "#000000", "#202020", "#7C7C7C", "#FCFCFC", "#3CBCFC", "#F83800", "#FCFCFC", "#000000"),
            Catalog("mac1bit", "Mac System 1-bit", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light, "#FFFFFF", "#FFFFFF", "#CCCCCC", "#000000", "#000000", "#808080", "#000000", "#FFFFFF", reducedMotion: true),
            Catalog("norton", "Norton Blue TUI", "px437ibmvga8x16", ThemeEra.Terminal, ThemeBrightness.Dark, "#0000A8", "#0000A8", "#000000", "#FFFFFF", "#00A8A8", "#FCFC54", "#00A8A8", "#000000"),
            Catalog("phosphor", "Green Phosphor CRT", "vt323", ThemeEra.Terminal, ThemeBrightness.Dark, "#001100", "#001B00", "#003300", "#33FF33", "#33FF33", "#1A801A", "#33FF33", "#001100", postEffects: TerminalPostEffects()),
            Catalog("amber", "Amber Terminal", "vt323", ThemeEra.Terminal, ThemeBrightness.Dark, "#1A0F00", "#221400", "#332000", "#FFB000", "#FFB000", "#805800", "#FFB000", "#1A0F00", postEffects: TerminalPostEffects()),
            Catalog("win95", "Windows 95", "selawik", ThemeEra.Desktop, ThemeBrightness.Light, "#008080", "#C0C0C0", "#E0E0E0", "#000000", "#000080", "#1084D0", "#000080", "#FFFFFF"),
            Catalog("macos9", "Mac OS 9 Platinum", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light, "#CCCCCC", "#DDDDDD", "#BBBBBB", "#000000", "#6666CC", "#9999CC", "#6666CC", "#FFFFFF"),
            Catalog("nextstep", "NeXTSTEP", "selawik", ThemeEra.Desktop, ThemeBrightness.Light, "#555555", "#AAAAAA", "#777777", "#000000", "#111111", "#D0D0D0", "#111111", "#FFFFFF"),
            Catalog("beos", "BeOS R5", "selawik", ThemeEra.Desktop, ThemeBrightness.Light, "#D8D8D8", "#D8D8D8", "#FFCB00", "#000000", "#336698", "#FFCB00", "#FFCB00", "#000000"),
            Catalog("luna", "Windows XP Luna", "selawik", ThemeEra.Desktop, ThemeBrightness.Light, "#ECE9D8", "#F4F3EE", "#DDE8FF", "#000000", "#3C81F3", "#F9B233", "#3C81F3", "#FFFFFF"),
            Catalog("aqua", "macOS Aqua", "atkinsonhyperlegible", ThemeEra.Desktop, ThemeBrightness.Light, "#F0F0F0", "#FFFFFF", "#E8E8E8", "#1F2933", "#3B88FD", "#88BFFC", "#3B88FD", "#FFFFFF"),
            Catalog("aero", "Windows Aero / 7", "selawik", ThemeEra.Glass, ThemeBrightness.Light, "#DCEFFF", "#B8D6FB", "#FFFFFF", "#1E395B", "#2FA6DE", "#5A8BB0", "#2FA6DE", "#FFFFFF", glass: true),
            Catalog("metro", "Metro / Windows 8", "selawik", ThemeEra.Modern, ThemeBrightness.Dark, "#1D1D1D", "#252525", "#333333", "#FFFFFF", "#1BA1E2", "#E51400", "#1BA1E2", "#FFFFFF"),
            Catalog("fluent", "Fluent Acrylic", "selawik", ThemeEra.Glass, ThemeBrightness.Dark, "#202020", "#2C2C2C", "#3A3A3A", "#FFFFFF", "#0078D4", "#60CDFF", "#0078D4", "#FFFFFF", glass: true),
            Catalog("material", "Material Design 3", "roboto", ThemeEra.Modern, ThemeBrightness.Light, "#FFFBFE", "#FFFBFE", "#E7E0EC", "#1C1B1F", "#6750A4", "#B3261E", "#6750A4", "#FFFFFF"),
            Catalog("liquid", "Liquid Glass", "atkinsonhyperlegible", ThemeEra.Glass, ThemeBrightness.Dark, "#111827", "#FFFFFF", "#C7D2FE", "#FFFFFF", "#7DD3FC", "#F0ABFC", "#FFFFFF", "#111827", glass: true, liquid: true),
            Catalog("cyberpunk", "Cyberpunk Neon", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark, "#0A0A12", "#161622", "#23142F", "#FCEE0A", "#00F0FF", "#FF003C", "#FCEE0A", "#000000", postEffects: NeonPostEffects()),
            Catalog("vaporwave", "Vaporwave Sunset", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark, "#1A1030", "#2C164D", "#3E1E6C", "#FFFFFF", "#FF71CE", "#01CDFE", "#05FFA1", "#1A1030"),
            Catalog("nord", "Nord", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Dark, "#2E3440", "#3B4252", "#434C5E", "#D8DEE9", "#88C0D0", "#BF616A", "#88C0D0", "#2E3440"),
            Catalog("dracula", "Dracula", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark, "#282A36", "#343746", "#44475A", "#F8F8F2", "#BD93F9", "#FF79C6", "#44475A", "#F8F8F2"),
            Catalog("solarized-light", "Solarized Light", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Light, "#FDF6E3", "#EEE8D5", "#E7DFC5", "#657B83", "#268BD2", "#2AA198", "#268BD2", "#FDF6E3"),
            Catalog("solarized-dark", "Solarized Dark", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark, "#002B36", "#073642", "#094352", "#839496", "#268BD2", "#2AA198", "#268BD2", "#FDF6E3"),
            Catalog("gruvbox", "Gruvbox", "ibmplexmono", ThemeEra.Modern, ThemeBrightness.Dark, "#282828", "#3C3836", "#504945", "#EBDBB2", "#D65D0E", "#689D6A", "#D79921", "#282828"),
            Catalog("parchment", "RPG Parchment", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, "#E8D8B0", "#F2E3BD", "#D6BC7F", "#3B2A18", "#C9A227", "#8B0000", "#8B0000", "#FFFFFF"),
            Catalog("lcars", "Sci-Fi HUD / LCARS", "orbitron", ThemeEra.Modern, ThemeBrightness.Dark, "#000000", "#111111", "#222222", "#FFCC99", "#FF9C00", "#CC99CC", "#9999FF", "#000000"),
            Catalog("eink", "E-Ink Paper", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, "#F5F2EA", "#FFFFFF", "#DDD9D0", "#333333", "#333333", "#8A8680", "#333333", "#FFFFFF", reducedMotion: true),
            Catalog("neumorphic", "Neumorphism", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, "#E0E5EC", "#E0E5EC", "#D3DAE4", "#4A5568", "#5B7FFF", "#A3B1C6", "#5B7FFF", "#FFFFFF"),
            Catalog("brutalist", "Brutalist", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, "#FFFFFF", "#FFFFFF", "#F0F0F0", "#000000", "#FF3300", "#000000", "#000000", "#FFFFFF", reducedMotion: true)
        };

        public static IReadOnlyList<ThemeDefinition> Themes => BuiltInThemes;

        public static ThemeDefinition Default => Resolve(DefaultThemeId);

        public static ThemeDefinition Resolve(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                foreach (var theme in Themes)
                {
                    if (string.Equals(theme.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
                        return theme;
                }
            }

            foreach (var theme in Themes)
            {
                if (string.Equals(theme.Id, DefaultThemeId, StringComparison.Ordinal))
                    return theme;
            }

            throw new InvalidOperationException("The theme registry does not contain the default theme.");
        }

        private static ThemeDefinition CreateDefaultDefinition()
        {
            var teal = new Color(20, 126, 133);
            var palette = new ThemePalette
            {
                Background = Color.White,
                Surface = new Color(245, 245, 245),
                SurfaceAlt = new Color(225, 229, 233),
                Text = Color.Black,
                HeadingText = Color.Black,
                MutedText = new Color(105, 115, 125),
                Primary = teal,
                Secondary = new Color(82, 101, 111),
                Warning = new Color(214, 143, 0),
                Danger = new Color(178, 34, 34),
                Info = new Color(51, 153, 255),
                Selection = teal,
                SelectionText = Color.White,
                TabText = Color.Black,
                SelectedTabText = Color.Black,
                FieldFrame = Color.White,
                FieldBorder = new Color(82, 101, 111),
                DisabledSurface = new Color(210, 216, 222),
                DisabledText = Color.Gray
            };

            return new ThemeDefinition
            {
                Id = "default",
                DisplayName = "Default (no theme)",
                FontName = "default",
                Palette = palette,
                Metadata = new ThemeMetadata
                {
                    Era = ThemeEra.Modern,
                    Brightness = ThemeBrightness.Light,
                    ReducedMotion = false,
                    Description = "The library's built-in styling when no theme is applied (PortableTheme.CreateDefault).",
                    PreviewSwatches = new[] { palette.Background, palette.Surface, palette.Primary, palette.Secondary, palette.Selection }
                },
                // Deliberately the untouched library default so the demo shows what consumers get without a theme.
                CreateTheme = PortableTheme.CreateDefault,
                ClearColor = Color.White,
                BackgroundColor = Color.White
            };
        }

        private static ThemeDefinition CreateC64()
        {
            var blue = new Color(64, 49, 141);
            var darkBlue = new Color(31, 32, 86);
            var white = new Color(158, 173, 255);
            var lightBlue = new Color(120, 106, 189);
            var green = new Color(104, 235, 104);
            var yellow = new Color(255, 255, 119);
            var red = new Color(136, 57, 50);
            var cyan = new Color(112, 164, 178);
            var palette = new ThemePalette
            {
                Background = blue,
                Surface = blue,
                SurfaceAlt = darkBlue,
                Text = white,
                HeadingText = white,
                // Brighter than the classic C64 light blue so secondary text stays readable.
                MutedText = new Color(172, 164, 222),
                Primary = green,
                Secondary = yellow,
                Warning = yellow,
                Danger = red,
                Info = cyan,
                Selection = white,
                SelectionText = blue,
                TabText = white,
                SelectedTabText = blue,
                FieldFrame = darkBlue,
                FieldBorder = lightBlue,
                DisabledSurface = darkBlue,
                DisabledText = lightBlue
            };

            return CreateDefinition("c64", "C64", "pressstart2p", ThemeEra.Retro, ThemeBrightness.Dark, palette, blue);
        }

        private static ThemeDefinition CreateGlass()
        {
            var night = new Color(9, 14, 28);
            var cyan = new Color(97, 242, 226, 214);
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
                BackgroundBrush = new GradientBrush(night, new Color(17, 35, 54), GradientDirection.DiagonalDown),
                SurfaceBrush = new FrostedGlassBrush(new Color(255, 255, 255, 42), new Color(255, 255, 255, 216), 18, 0.42f),
                SurfaceAltBrush = new FrostedGlassBrush(new Color(205, 248, 255, 48), new Color(255, 255, 255, 202), 20, 0.44f),
                SelectionBrush = new GradientBrush(cyan, new Color(255, 211, 124, 192), GradientDirection.Horizontal),
                FieldFrameBrush = new FrostedGlassBrush(new Color(255, 255, 255, 54), new Color(255, 255, 255, 208), 16, 0.34f)
            };

            return CreateDefinition("glass", "Frosted Glass", "atkinsonhyperlegible", ThemeEra.Glass, ThemeBrightness.Dark, palette, night);
        }

        private static ThemeDefinition CreateGameBoy()
        {
            var light = new Color(155, 188, 15);
            var glass = new Color(202, 220, 159);
            var mid = new Color(139, 172, 15);
            var dark = new Color(48, 98, 48);
            var ink = new Color(15, 56, 15);
            var palette = new ThemePalette
            {
                Background = light,
                Surface = mid,
                SurfaceAlt = dark,
                Text = ink,
                HeadingText = ink,
                MutedText = dark,
                Primary = ink,
                Secondary = new Color(88, 111, 26),
                Warning = new Color(102, 84, 15),
                Danger = new Color(112, 45, 40),
                Info = new Color(32, 78, 54),
                Selection = ink,
                SelectionText = glass,
                TabText = ink,
                SelectedTabText = glass,
                FieldFrame = dark,
                FieldBorder = ink,
                DisabledSurface = new Color(113, 143, 34),
                DisabledText = new Color(64, 91, 38)
            };

            return CreateDefinition("gameboy", "Game Boy DMG", "silkscreen", ThemeEra.Retro, ThemeBrightness.Light, palette, light, reducedMotion: true);
        }

        private static ThemeDefinition CreateDos()
        {
            var navy = new Color(0, 0, 84);
            var blue = new Color(0, 0, 170);
            var dialog = new Color(170, 170, 170);
            var cyan = new Color(85, 255, 255);
            var yellow = new Color(255, 255, 85);
            var black = new Color(0, 0, 0);
            var palette = new ThemePalette
            {
                Background = navy,
                Surface = blue,
                SurfaceAlt = dialog,
                Text = new Color(192, 192, 192),
                HeadingText = yellow,
                MutedText = cyan,
                Primary = new Color(0, 170, 170),
                Secondary = yellow,
                Warning = yellow,
                Danger = new Color(170, 0, 0),
                Info = Color.White,
                Selection = dialog,
                SelectionText = black,
                TabText = Color.White,
                SelectedTabText = black,
                FieldFrame = dialog,
                FieldBorder = cyan,
                DisabledSurface = new Color(85, 85, 85),
                DisabledText = new Color(120, 120, 120)
            };

            return CreateDefinition("dos", "DOS / Turbo Vision", "px437ibmvga8x16", ThemeEra.Terminal, ThemeBrightness.Dark, palette, navy);
        }

        private static ThemeDefinition CreateAmiga()
        {
            var gray = new Color(170, 170, 170);
            var panel = new Color(195, 195, 195);
            var blue = new Color(0, 86, 170);
            var ink = new Color(23, 23, 28);
            var palette = new ThemePalette
            {
                Background = gray,
                Surface = panel,
                SurfaceAlt = new Color(146, 146, 154),
                Text = ink,
                HeadingText = ink,
                MutedText = new Color(0, 45, 112),
                Primary = blue,
                Secondary = new Color(255, 136, 0),
                Warning = new Color(255, 136, 0),
                Danger = new Color(196, 48, 48),
                Info = new Color(39, 107, 172),
                Selection = blue,
                SelectionText = Color.White,
                TabText = ink,
                SelectedTabText = Color.White,
                FieldFrame = new Color(146, 146, 154),
                FieldBorder = blue,
                DisabledSurface = new Color(132, 132, 140),
                DisabledText = new Color(86, 86, 92)
            };

            return CreateDefinition("amiga", "Amiga Workbench", "jersey10", ThemeEra.Desktop, ThemeBrightness.Light, palette, gray);
        }

        private static ThemeDefinition CreateTerminal()
        {
            var graphite = new Color(11, 16, 20);
            var green = new Color(42, 246, 183);
            var cyan = new Color(83, 232, 255);
            var text = new Color(217, 255, 244);
            var palette = new ThemePalette
            {
                Background = graphite,
                Surface = new Color(21, 30, 34),
                SurfaceAlt = new Color(39, 52, 58),
                Text = text,
                HeadingText = green,
                MutedText = new Color(138, 184, 174),
                Primary = green,
                Secondary = cyan,
                Warning = new Color(246, 206, 87),
                Danger = new Color(255, 96, 116),
                Info = cyan,
                Selection = green,
                SelectionText = graphite,
                TabText = text,
                SelectedTabText = graphite,
                FieldFrame = new Color(39, 52, 58),
                FieldBorder = green,
                DisabledSurface = new Color(46, 57, 62),
                DisabledText = new Color(105, 126, 125)
            };

            return CreateDefinition("terminal", "Terminal Glass", "ibmplexmono", ThemeEra.Terminal, ThemeBrightness.Dark, palette, graphite);
        }

        private static ThemeDefinition CreateStudio()
        {
            var offWhite = new Color(247, 244, 236);
            var paper = new Color(255, 252, 246);
            var charcoal = new Color(37, 37, 37);
            var blue = new Color(59, 130, 246);
            var coral = new Color(255, 107, 94);
            var palette = new ThemePalette
            {
                Background = offWhite,
                Surface = paper,
                SurfaceAlt = new Color(233, 228, 218),
                Text = charcoal,
                HeadingText = charcoal,
                MutedText = new Color(109, 106, 101),
                Primary = blue,
                Secondary = coral,
                Warning = new Color(210, 144, 39),
                Danger = new Color(194, 64, 76),
                Info = new Color(46, 118, 178),
                Selection = charcoal,
                SelectionText = offWhite,
                TabText = charcoal,
                SelectedTabText = offWhite,
                FieldFrame = new Color(233, 228, 218),
                FieldBorder = blue,
                DisabledSurface = new Color(218, 213, 203),
                DisabledText = new Color(139, 134, 126)
            };

            return CreateDefinition("studio", "Soft Studio", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Light, palette, offWhite);
        }

        private static ThemeDefinition CreateAurora()
        {
            var graphite = new Color(15, 18, 24);
            var deepTeal = new Color(17, 45, 40);
            var mint = new Color(104, 228, 192);
            var lime = new Color(231, 247, 109);
            var ink = new Color(12, 15, 18);
            var palette = new ThemePalette
            {
                Background = graphite,
                Surface = new Color(25, 31, 34),
                SurfaceAlt = new Color(38, 48, 51),
                Text = new Color(235, 247, 242),
                HeadingText = Color.White,
                MutedText = new Color(150, 174, 169),
                Primary = mint,
                Secondary = new Color(255, 178, 93),
                Warning = new Color(255, 178, 93),
                Danger = new Color(255, 107, 122),
                Info = new Color(124, 199, 255),
                Selection = lime,
                SelectionText = ink,
                TabText = new Color(235, 247, 242),
                SelectedTabText = ink,
                FieldFrame = new Color(11, 15, 18),
                FieldBorder = mint,
                DisabledSurface = new Color(45, 51, 53),
                DisabledText = new Color(103, 121, 119),
                BackgroundBrush = new GradientBrush(graphite, deepTeal, GradientDirection.DiagonalDown),
                SurfaceBrush = new GradientBrush(new Color(25, 31, 34), new Color(31, 43, 40), GradientDirection.Vertical),
                SurfaceAltBrush = new GradientBrush(new Color(38, 48, 51), new Color(31, 39, 45), GradientDirection.Horizontal),
                SelectionBrush = new GradientBrush(mint, lime, GradientDirection.Horizontal),
                FieldFrameBrush = new GradientBrush(new Color(11, 15, 18), new Color(18, 25, 28), GradientDirection.Vertical)
            };

            return CreateDefinition("aurora", "Aurora Modern", "atkinsonhyperlegible", ThemeEra.Modern, ThemeBrightness.Dark, palette, graphite);
        }

        private static ThemeDefinition CreateDefinition(
            string id,
            string displayName,
            string fontName,
            ThemeEra era,
            ThemeBrightness brightness,
            ThemePalette palette,
            Color clearColor,
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
                    var theme = CreateTheme(palette);
                    ApplyThemeShadows(theme, id);
                    ApplyThemeChrome(theme, id);
                    return theme;
                },
                ClearColor = clearColor,
                BackgroundColor = palette.Background
            };
        }

        /// <summary>
        ///     Applies palette styles, shadows and era chrome to a flat theme â€” usable by apps that
        ///     hand-build themes (like the demo registry) so they get the same look as the catalog.
        /// </summary>
        public static void ApplyStyling(PortableTheme theme, string id)
        {
            if (theme.Palette != null && !ReferenceEquals(theme.Palette, ThemePalette.Empty))
                ApplyPaletteStyles(theme, theme.Palette);
            ApplyThemeShadows(theme, id);
            ApplyThemeChrome(theme, id);
        }

        /// <summary>Per-theme era chrome: button borders, corner radii, gradients, bevels, pills.</summary>
        private static void ApplyThemeChrome(PortableTheme theme, string id)
        {
            var palette = theme.Palette;

            void Chrome(ControlStyle style, Brush? background, Brush? border, float borderWidth, float radius)
            {
                if (background != null)
                    style.Normal.Background = background;
                style.Normal.BorderBrush = border ?? style.Normal.BorderBrush;
                style.Normal.BorderThickness = new Thickness(border == null ? 0 : borderWidth);
                style.Normal.CornerRadius = radius;
                style.InvalidateResolvedCache();
            }

            void Bevel(Color face, Color outerLight, Color innerLight, Color innerDark, Color outerDark)
            {
                var raised = new BevelBrush(face, outerLight, innerLight, innerDark, outerDark);
                Chrome(theme.Button, raised, null, 0, 0);
                theme.Button.Pressed.Background = raised.AsSunken();
                theme.ButtonBackgroundBrush = raised;
                theme.Button.InvalidateResolvedCache();
            }

            switch (id)
            {
                case "luna":
                {
                    // Windows XP: glossy face, dark blue frame, radius 3, orange hover ring.
                    var face = new LinearGradientBrush(new GradientStop(0, Hex("#FFFFFF")), new GradientStop(1, Hex("#ECE9D8")));
                    var pressed = new LinearGradientBrush(new GradientStop(0, Hex("#D8D4C8")), new GradientStop(1, Hex("#E8E4D8")));
                    Chrome(theme.Button, face, Solid(Hex("#003C74")), 1, 3);
                    theme.Button.Normal.TextColor = Color.Black;
                    theme.Button.Hover.BorderBrush = Solid(Hex("#F9B233"));
                    theme.Button.Hover.BorderThickness = new Thickness(2);
                    theme.Button.Pressed.Background = pressed;
                    theme.ButtonTextColor = Color.Black;
                    theme.ButtonHoverBrush = Solid(new Color(249, 178, 51, 34));
                    theme.ButtonPressedBrush = Solid(new Color(0, 60, 116, 40));
                    Chrome(theme.TextBox, Solid(Color.White), Solid(Hex("#7F9DB9")), 1, 0);
                    Chrome(theme.ListBox, Solid(Color.White), Solid(Hex("#7F9DB9")), 1, 0);
                    Chrome(theme.ComboBox, Solid(Color.White), Solid(Hex("#7F9DB9")), 1, 0);
                    theme.TextBoxBackgroundBrush = Solid(Color.White);
                    theme.TextBoxTextColor = Color.Black;
                    theme.ListBoxBackgroundBrush = Solid(Color.White);
                    theme.ListBoxItemTextColor = Color.Black;
                    theme.ListBoxItemBackgroundBrush = Solid(Color.White);
                    theme.ComboBoxGlyphColor = Hex("#003C74");
                    theme.Button.InvalidateResolvedCache();
                    break;
                }
                case "win95":
                    Bevel(palette.Surface, Color.White, Hex("#DFDFDF"), Hex("#808080"), Color.Black);
                    break;
                case "nextstep":
                    Bevel(palette.Surface, Hex("#D0D0D0"), palette.Surface, Hex("#585858"), Hex("#303030"));
                    break;
                case "amiga":
                    Bevel(palette.Surface, Color.White, palette.Surface, palette.Surface, Color.Black);
                    break;
                case "beos":
                {
                    var raised = new BevelBrush(palette.Surface, Color.White, Hex("#9A9A9A"));
                    Chrome(theme.Button, raised, null, 0, 0);
                    theme.Button.Pressed.Background = raised.AsSunken();
                    theme.ButtonBackgroundBrush = raised;
                    break;
                }
                case "mac1bit":
                    Chrome(theme.Button, Solid(Color.White), Solid(Color.Black), 1, 5);
                    theme.Button.Pressed.Background = Solid(Color.Black);
                    theme.ButtonPressedTextColor = Color.White;
                    theme.DisabledOverlayBrush = PatternBrush.Dither(new Color(255, 255, 255, 180), new Color(255, 255, 255, 0));
                    break;
                case "macos9":
                {
                    var face = new LinearGradientBrush(new GradientStop(0, Hex("#F8F8F8")), new GradientStop(1, Hex("#D0D0D0")));
                    Chrome(theme.Button, face, Solid(Hex("#888888")), 1, 6);
                    break;
                }
                case "aqua":
                {
                    var gel = new LinearGradientBrush(
                        new GradientStop(0, Hex("#FDFFFF")),
                        new GradientStop(0.45f, Hex("#CBE3F5")),
                        new GradientStop(0.5f, Hex("#9CC5EB")),
                        new GradientStop(1, Hex("#C7E0F5")));
                    Chrome(theme.Button, gel, Solid(Hex("#7A96B8")), 1, 12);
                    theme.Button.Normal.TextColor = Color.Black;
                    theme.ButtonTextColor = Color.Black;
                    // Signature Aqua pinstripes on chrome surfaces.
                    var pinstripes = PatternBrush.Pinstripes(Hex("#F4F4F4"), Hex("#E8E8E8"), 4);
                    theme.TabHeaderBackgroundBrush = pinstripes;
                    theme.Panel.Normal.Background = pinstripes;
                    Chrome(theme.TextBox, Solid(Color.White), Solid(Hex("#9AB0C8")), 1, 4);
                    Chrome(theme.ListBox, Solid(Color.White), Solid(Hex("#9AB0C8")), 1, 4);
                    break;
                }
                case "lcars":
                    Chrome(theme.Button, null, null, 0, 19);
                    break;
                case "liquid":
                    Chrome(theme.Button, null, Solid(new Color(255, 255, 255, 96)), 1, 18);
                    break;
                case "glass":
                case "aero":
                    Chrome(theme.Button, null, Solid(new Color(255, 255, 255, 96)), 1, 10);
                    break;
                case "fluent":
                    Chrome(theme.Button, null, Solid(Hex("#454545")), 1, 4);
                    break;
                case "material":
                    Chrome(theme.Button, null, null, 0, 4);
                    break;
                case "studio":
                    Chrome(theme.Button, null, null, 0, 8);
                    break;
                case "aurora":
                    Chrome(theme.Button, null, null, 0, 10);
                    break;
                case "neumorphic":
                    Chrome(theme.Button, null, null, 0, 14);
                    break;
                case "nord":
                case "dracula":
                case "gruvbox":
                    Chrome(theme.Button, null, Solid(palette.FieldBorder), 1, 6);
                    break;
                case "solarized-light":
                case "solarized-dark":
                case "parchment":
                    Chrome(theme.Button, null, Solid(palette.FieldBorder), 1, 4);
                    break;
                case "vaporwave":
                    Chrome(theme.Button, null, Solid(palette.Primary), 1, 8);
                    break;
                case "eink":
                    Chrome(theme.Button, null, Solid(palette.Text), 1, 3);
                    break;
                case "brutalist":
                    Chrome(theme.Button, null, Solid(Color.Black), 3, 0);
                    break;
                case "c64":
                case "nes":
                    Chrome(theme.Button, null, Solid(palette.Primary), 2, 0);
                    break;
                case "gameboy":
                    Chrome(theme.Button, null, Solid(palette.Text), 1, 0);
                    break;
                case "dos":
                case "norton":
                {
                    // Turbo Vision / Norton Commander: light dialog buttons with black text and hard shadows.
                    var buttonFace = id == "norton" ? Hex("#00A8A8") : palette.SurfaceAlt;
                    Chrome(theme.Button, Solid(buttonFace), Solid(Color.Black), 1, 0);
                    theme.Button.Normal.TextColor = Color.Black;
                    theme.ButtonBackgroundBrush = Solid(buttonFace);
                    theme.ButtonTextColor = Color.Black;
                    theme.ButtonHoverBrush = Solid(new Color(0, 170, 170, 90));
                    theme.ButtonPressedBrush = Solid(new Color(0, 0, 0, 60));
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 190), Offset = new Vector2(2, 2), Blur = 0 };
                    Chrome(theme.ListBox, null, Solid(palette.FieldBorder), 1, 0);
                    Chrome(theme.TextBox, null, Solid(palette.FieldBorder), 1, 0);
                    break;
                }
                case "metro":
                    Chrome(theme.Button, null, null, 0, 0);
                    break;
                case "terminal":
                case "phosphor":
                case "amber":
                case "cyberpunk":
                    // Bright 1px primary frames from the palette builder, square corners.
                    Chrome(theme.Button, null, Solid(palette.Primary), 1, 0);
                    break;
            }
        }

        private static void ApplyThemeShadows(PortableTheme theme, string id)
        {
            switch (id)
            {
                case "material":
                    theme.ButtonShadow = ShadowStyle.Level1();
                    theme.PanelShadow = ShadowStyle.Level2();
                    break;
                case "neumorphic":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(163, 177, 198, 150), Offset = new Vector2(6, 6), Blur = 12 };
                    theme.PanelShadow = new ShadowStyle { Color = new Color(163, 177, 198, 130), Offset = new Vector2(8, 8), Blur = 16 };
                    break;
                case "brutalist":
                    theme.ButtonShadow = new ShadowStyle { Color = Color.Black, Offset = new Vector2(6, 6), Blur = 0 };
                    theme.PanelShadow = new ShadowStyle { Color = Color.Black, Offset = new Vector2(8, 8), Blur = 0 };
                    break;
                case "studio":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(217, 212, 203, 170), Offset = new Vector2(4, 5), Blur = 10 };
                    theme.PanelShadow = new ShadowStyle { Color = new Color(217, 212, 203, 190), Offset = new Vector2(6, 8), Blur = 14 };
                    break;
                case "glass":
                case "aero":
                case "fluent":
                case "liquid":
                    theme.PanelShadow = new ShadowStyle { Color = new Color(0, 0, 0, 110), Offset = new Vector2(0, 10), Blur = 18 };
                    break;
                case "aqua":
                case "luna":
                case "macos9":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 0, 0, 70), Offset = new Vector2(0, 2), Blur = 4 };
                    break;
                case "cyberpunk":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(0, 240, 255, 90), Offset = Vector2.Zero, Blur = 10, Spread = 1 };
                    theme.PanelShadow = new ShadowStyle { Color = new Color(255, 0, 60, 70), Offset = Vector2.Zero, Blur = 12 };
                    break;
                case "vaporwave":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(255, 113, 206, 110), Offset = new Vector2(0, 4), Blur = 12 };
                    theme.PanelShadow = new ShadowStyle { Color = new Color(1, 205, 254, 90), Offset = new Vector2(0, 6), Blur = 16 };
                    break;
                case "gameboy":
                    theme.ButtonShadow = new ShadowStyle { Color = new Color(15, 56, 15, 200), Offset = new Vector2(2, 2), Blur = 0 };
                    break;
                case "nord":
                case "dracula":
                    theme.ButtonShadow = ShadowStyle.Level1();
                    break;
            }
        }

        private static PortableTheme CreateTheme(ThemePalette palette)
        {
            var theme = new PortableTheme
            {
                Palette = palette,
                TextColor = palette.Text,
                TextSize = 14,
                PixelSnapping = true,
                FocusBorderBrush = Solid(palette.Primary),
                FocusBorderWidth = 2,
                FocusVisualKind = FocusVisualKind.Rectangle,
                DisabledOverlayBrush = Solid(new Color(0, 0, 0, 70)),
                DisabledTextColor = palette.DisabledText,
                ButtonPadding = new Thickness(8, 6),
                ButtonBackgroundBrush = SurfaceBrush(palette),
                ButtonHoverBrush = Solid(new Color((int)palette.Primary.R, (int)palette.Primary.G, (int)palette.Primary.B, 72)),
                ButtonPressedBrush = SelectionBrush(palette),
                ButtonTextColor = palette.Text,
                ButtonHoverTextColor = palette.Text,
                ButtonPressedTextColor = palette.SelectionText,
                ToggleBrush = SelectionBrush(palette),
                ToggleTextColor = palette.SelectionText,
                TextBoxBackgroundBrush = FieldFrameBrush(palette),
                TextBoxTextColor = palette.Text,
                TextBoxCursorBrush = Solid(palette.Primary),
                TextBoxSelectionBrush = Solid(new Color((int)palette.Primary.R, (int)palette.Primary.G, (int)palette.Primary.B, 120)),
                TextBoxHintTextColor = palette.MutedText,
                TextBoxPadding = new Thickness(6, 4),
                TextBoxHeight = 32,
                ScrollBarThickness = 8,
                ScrollBarGutterBrush = SurfaceBrush(palette),
                ScrollBarBrush = Solid(palette.Primary),
                ScrollBarHoverBrush = Solid(palette.Secondary),
                ScrollBarPressedBrush = SelectionBrush(palette),
                TabHeaderHeight = 36,
                TabHeaderBackgroundBrush = SurfaceBrush(palette),
                TabSelectedHeaderBackgroundBrush = SelectionBrush(palette),
                TabHeaderTextColor = palette.TabText,
                TabSelectedHeaderTextColor = palette.SelectedTabText,
                ContextMenuBackgroundBrush = SurfaceBrush(palette),
                ComboBoxHeight = 32,
                ComboBoxDropDownMaxHeight = 190,
                ComboBoxDropDownBackgroundBrush = SurfaceBrush(palette),
                ListBoxBackgroundBrush = SurfaceBrush(palette),
                ListBoxItemHeight = 28,
                ListBoxItemPadding = new Thickness(6, 0),
                ListBoxItemBackgroundBrush = SurfaceBrush(palette),
                ListBoxSelectedItemBackgroundBrush = SelectionBrush(palette),
                ListBoxItemTextColor = palette.Text,
                ListBoxSelectedItemTextColor = palette.SelectionText,
                CheckBoxBoxSize = 18,
                CheckBoxBoxSpacing = 8,
                CheckBoxBoxBorderWidth = 2,
                CheckBoxBoxBackgroundBrush = SurfaceBrush(palette),
                CheckBoxBoxBorderBrush = Solid(palette.Primary),
                CheckBoxCheckMarkBrush = SelectionBrush(palette),
                CheckBoxGlyphKind = CheckBoxGlyphKind.Check,
                CheckBoxTextColor = palette.Text,
                RadioButtonDotBrush = SelectionBrush(palette),
                RadioButtonDotSize = 8,
                ToolTipBackgroundBrush = Solid(new Color((int)palette.Background.R, (int)palette.Background.G, (int)palette.Background.B, 238)),
                ToolTipBorderBrush = Solid(palette.Primary),
                ToolTipBorderWidth = new Thickness(1),
                ToolTipPadding = new Thickness(8, 5, 8, 6),
                ToolTipTextColor = palette.Text,
                ProgressIndicatorForeground = palette.Primary,
                ProgressIndicatorHeight = 48,
                SliderTrackBrush = FieldFrameBrush(palette),
                SliderFillBrush = SelectionBrush(palette),
                SliderThumbBrush = SurfaceBrush(palette),
                SliderThumbBorderBrush = Solid(palette.Primary),
                ProgressBarBackgroundBrush = FieldFrameBrush(palette),
                ProgressBarFillBrush = SelectionBrush(palette)
            };

            ApplyPaletteStyles(theme, palette);
            return theme;
        }

        private static void ApplyPaletteStyles(PortableTheme theme, ThemePalette palette)
        {
            var styles = ControlStyleBuilder.FromPalette(palette);
            theme.Typography = new Typography { TextSize = theme.TextSize };
            theme.Metrics = new ThemeMetrics
            {
                ControlPadding = theme.ButtonPadding,
                ControlHeight = theme.ComboBoxHeight,
                BorderWidth = 1,
                Spacing = 8
            };
            theme.Button = styles["Button"];
            theme.TextBox = styles["TextBox"];
            theme.CheckBox = styles["CheckBox"];
            theme.RadioButton = styles["RadioButton"];
            theme.ToggleButton = styles["ToggleButton"];
            theme.ComboBox = styles["ComboBox"];
            theme.ListBox = styles["ListBox"];
            theme.ListBoxItem = styles["ListBoxItem"];
            theme.Tab = styles["Tab"];
            theme.ToolTip = styles["ToolTip"];
            theme.ContextMenu = styles["ContextMenu"];
            theme.ScrollBar = styles["ScrollBar"];
            theme.Slider = styles["Slider"];
            theme.ProgressBar = styles["ProgressBar"];
            theme.Panel = styles["Panel"];
        }

        private static ThemeDefinition Catalog(
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
            bool reducedMotion = false,
            bool glass = false,
            bool liquid = false,
            IReadOnlyList<PostEffect>? postEffects = null)
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
                Danger = Mix(secondaryColor, Color.Red, 0.45f),
                Info = primaryColor,
                Selection = selectionColor,
                SelectionText = selectionTextColor,
                TabText = textColor,
                SelectedTabText = selectionTextColor,
                FieldFrame = surfaceAltColor,
                FieldBorder = primaryColor,
                DisabledSurface = Mix(surfaceColor, backgroundColor, 0.5f),
                DisabledText = Mix(textColor, backgroundColor, 0.45f),
                BackgroundBrush = glass ? new GradientBrush(backgroundColor, Mix(primaryColor, backgroundColor, 0.78f), GradientDirection.DiagonalDown) : null,
                SurfaceBrush = liquid ? new LiquidGlassBrush() : glass ? new AcrylicBrush(new Color((byte)surfaceColor.R, (byte)surfaceColor.G, (byte)surfaceColor.B, (byte)150)) : null,
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
                    var theme = CreateTheme(palette);
                    ApplyThemeShadows(theme, id);
                    ApplyThemeChrome(theme, id);
                    theme.Typography.FontName = fontName;
                    theme.PostEffects = postEffects ?? Array.Empty<PostEffect>();
                    if (reducedMotion)
                    {
                        theme.Button.TransitionDuration = TimeSpan.Zero;
                        theme.TextBox.TransitionDuration = TimeSpan.Zero;
                    }
                    if (liquid)
                    {
                        theme.Panel.Normal.CornerStyle = CornerStyle.Squircle;
                        theme.Panel.Normal.CornerRadius = 20;
                    }
                    return theme;
                },
                ClearColor = backgroundColor,
                BackgroundColor = backgroundColor
            };
        }

        private static IReadOnlyList<PostEffect> TerminalPostEffects()
        {
            return new PostEffect[]
            {
                new ScanlinePostEffect { Strength = 0.12f },
                new CrtBarrelPostEffect { Distortion = 0.06f },
                new BloomPostEffect { Strength = 0.18f }
            };
        }

        private static IReadOnlyList<PostEffect> NeonPostEffects()
        {
            return new PostEffect[]
            {
                new ScanlinePostEffect { Strength = 0.06f },
                new BloomPostEffect { Strength = 0.34f },
                new FilmGrainPostEffect { Strength = 0.025f }
            };
        }

        private static Color Hex(string value)
        {
            var hex = value.TrimStart('#');
            return new Color(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }

        private static Color Mix(Color first, Color second, float amount)
        {
            amount = MathHelper.Clamp(amount, 0, 1);
            return Color.Lerp(first, second, amount);
        }

        /// <summary>Muted text = text mixed toward the background, backing off until it stays readable on the surface.</summary>
        private static Color ReadableMuted(Color text, Color background, Color surface)
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

        private static Brush SurfaceBrush(ThemePalette palette)
        {
            return palette.SurfaceBrush ?? Solid(palette.Surface);
        }

        private static Brush SelectionBrush(ThemePalette palette)
        {
            return palette.SelectionBrush ?? Solid(palette.Selection);
        }

        private static Brush FieldFrameBrush(ThemePalette palette)
        {
            return palette.FieldFrameBrush ?? Solid(palette.FieldFrame);
        }

        private static SolidColorBrush Solid(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}




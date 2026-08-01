using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    public static class DemoThemeRegistry
    {
        public const string DefaultThemeId = "c64";

        private static readonly IReadOnlyList<DemoThemePreset> AllPresets = new[]
        {
            CreateC64(),
            CreateGameBoy(),
            CreateDos(),
            CreateAmiga(),
            CreateTerminal(),
            CreateStudio(),
            CreateAurora()
        };

        public static IReadOnlyList<DemoThemePreset> Presets => AllPresets;

        public static DemoThemePreset Default => Resolve(DefaultThemeId);

        public static IReadOnlyList<string> FontNames { get; } = new[]
        {
            "pressstart2p",
            "silkscreen",
            "px437ibmvga8x16",
            "jersey10",
            "ibmplexmono",
            "atkinsonhyperlegible"
        };

        public static DemoThemePreset Resolve(string? id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                foreach (var preset in Presets)
                {
                    if (string.Equals(preset.Id, id.Trim(), StringComparison.OrdinalIgnoreCase))
                        return preset;
                }
            }

            foreach (var preset in Presets)
            {
                if (string.Equals(preset.Id, DefaultThemeId, StringComparison.Ordinal))
                    return preset;
            }

            throw new InvalidOperationException("The demo theme registry does not contain the default preset.");
        }

        public static DemoThemePreset ResolveStartupTheme(string[]? args)
        {
            return ResolveStartupTheme(args, Environment.GetEnvironmentVariable("PORTABLEUI_DEMO_THEME"));
        }

        public static DemoThemePreset ResolveStartupTheme(string[]? args, string? environmentThemeId)
        {
            if (TryParseThemeArgument(args, out var argumentThemeId))
                return Resolve(argumentThemeId);

            if (!string.IsNullOrWhiteSpace(environmentThemeId))
                return Resolve(environmentThemeId);

            return Default;
        }

        public static bool TryParseThemeArgument(string[]? args, out string? themeId)
        {
            themeId = null;
            if (args == null)
                return false;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--theme", StringComparison.OrdinalIgnoreCase))
                {
                    themeId = i + 1 < args.Length ? args[i + 1] : null;
                    return true;
                }

                const string prefix = "--theme=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    themeId = arg.Substring(prefix.Length);
                    return true;
                }
            }

            return false;
        }

        public static int IndexOf(string? id)
        {
            for (var i = 0; i < Presets.Count; i++)
            {
                if (string.Equals(Presets[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return 0;
        }

        private static DemoThemePreset CreateC64()
        {
            var palette = new DemoThemePalette
            {
                Background = C64Theme.Blue,
                Surface = C64Theme.Blue,
                SurfaceAlt = C64Theme.DarkBlue,
                Text = C64Theme.White,
                HeadingText = C64Theme.White,
                MutedText = C64Theme.LightBlue,
                Primary = C64Theme.Green,
                Secondary = C64Theme.Yellow,
                Warning = C64Theme.Yellow,
                Danger = C64Theme.Red,
                Info = C64Theme.Cyan,
                Selection = C64Theme.White,
                SelectionText = C64Theme.Blue,
                TabText = C64Theme.White,
                SelectedTabText = C64Theme.Blue,
                FieldFrame = C64Theme.DarkBlue,
                FieldBorder = C64Theme.LightBlue,
                DisabledSurface = C64Theme.DarkBlue,
                DisabledText = C64Theme.LightBlue
            };

            return new DemoThemePreset
            {
                Id = DefaultThemeId,
                DisplayName = "C64",
                CreateTheme = C64Theme.Create,
                Palette = palette,
                FontName = "pressstart2p",
                ClearColor = C64Theme.Blue,
                BackgroundColor = C64Theme.Blue
            };
        }

        private static DemoThemePreset CreateGameBoy()
        {
            var light = new Color(155, 188, 15);
            var glass = new Color(202, 220, 159);
            var mid = new Color(139, 172, 15);
            var dark = new Color(48, 98, 48);
            var ink = new Color(15, 56, 15);
            var red = new Color(112, 45, 40);

            var palette = new DemoThemePalette
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
                Danger = red,
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

            return new DemoThemePreset
            {
                Id = "gameboy",
                DisplayName = "Game Boy DMG",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: glass,
                    buttonBackground: mid,
                    buttonHover: new Color(15, 56, 15, 85),
                    buttonPressed: new Color(15, 56, 15, 150),
                    tooltipBackground: ink,
                    tooltipText: glass),
                Palette = palette,
                FontName = "silkscreen",
                ClearColor = light,
                BackgroundColor = light
            };
        }

        private static DemoThemePreset CreateDos()
        {
            var navy = new Color(0, 0, 84);
            var blue = new Color(0, 0, 170);
            var dialog = new Color(170, 170, 170);
            var dialogLight = new Color(255, 255, 255);
            var dialogDark = new Color(85, 85, 85);
            var cyan = new Color(85, 255, 255);
            var yellow = new Color(255, 255, 85);
            var lightGray = new Color(192, 192, 192);
            var red = new Color(170, 0, 0);
            var black = new Color(0, 0, 0);

            var palette = new DemoThemePalette
            {
                Background = navy,
                Surface = blue,
                SurfaceAlt = dialog,
                Text = lightGray,
                HeadingText = yellow,
                MutedText = cyan,
                Primary = new Color(0, 170, 170),
                Secondary = yellow,
                Warning = yellow,
                Danger = red,
                Info = dialogLight,
                Selection = dialog,
                SelectionText = black,
                TabText = dialogLight,
                SelectedTabText = black,
                FieldFrame = dialog,
                FieldBorder = cyan,
                DisabledSurface = dialogDark,
                DisabledText = new Color(120, 120, 120)
            };

            return new DemoThemePreset
            {
                Id = "dos",
                DisplayName = "DOS / Turbo Vision",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: blue,
                    buttonBackground: blue,
                    buttonHover: new Color(0, 170, 170, 110),
                    buttonPressed: new Color(170, 170, 170, 180),
                    tooltipBackground: navy,
                    tooltipText: dialogLight),
                Palette = palette,
                FontName = "px437ibmvga8x16",
                ClearColor = navy,
                BackgroundColor = navy
            };
        }

        private static DemoThemePreset CreateAmiga()
        {
            var gray = new Color(170, 170, 170);
            var panel = new Color(195, 195, 195);
            var blue = new Color(0, 86, 170);
            var darkBlue = new Color(0, 45, 112);
            var orange = new Color(255, 136, 0);
            var ink = new Color(23, 23, 28);

            var palette = new DemoThemePalette
            {
                Background = gray,
                Surface = panel,
                SurfaceAlt = new Color(146, 146, 154),
                Text = ink,
                HeadingText = ink,
                MutedText = darkBlue,
                Primary = blue,
                Secondary = orange,
                Warning = orange,
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

            return new DemoThemePreset
            {
                Id = "amiga",
                DisplayName = "Amiga Workbench",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: Color.White,
                    buttonBackground: panel,
                    buttonHover: new Color(0, 86, 170, 90),
                    buttonPressed: new Color(255, 136, 0, 135),
                    tooltipBackground: darkBlue,
                    tooltipText: Color.White),
                Palette = palette,
                FontName = "jersey10",
                ClearColor = gray,
                BackgroundColor = gray
            };
        }

        private static DemoThemePreset CreateTerminal()
        {
            var graphite = new Color(11, 16, 20);
            var panel = new Color(21, 30, 34);
            var glass = new Color(39, 52, 58);
            var green = new Color(42, 246, 183);
            var cyan = new Color(83, 232, 255);
            var text = new Color(217, 255, 244);

            var palette = new DemoThemePalette
            {
                Background = graphite,
                Surface = panel,
                SurfaceAlt = glass,
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
                FieldFrame = glass,
                FieldBorder = green,
                DisabledSurface = new Color(46, 57, 62),
                DisabledText = new Color(105, 126, 125)
            };

            return new DemoThemePreset
            {
                Id = "terminal",
                DisplayName = "Terminal Glass",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: new Color(5, 10, 13),
                    buttonBackground: panel,
                    buttonHover: new Color(42, 246, 183, 80),
                    buttonPressed: new Color(83, 232, 255, 120),
                    tooltipBackground: graphite,
                    tooltipText: text),
                Palette = palette,
                FontName = "ibmplexmono",
                ClearColor = graphite,
                BackgroundColor = graphite
            };
        }

        private static DemoThemePreset CreateStudio()
        {
            var offWhite = new Color(247, 244, 236);
            var paper = new Color(255, 252, 246);
            var charcoal = new Color(37, 37, 37);
            var blue = new Color(59, 130, 246);
            var coral = new Color(255, 107, 94);
            var muted = new Color(109, 106, 101);

            var palette = new DemoThemePalette
            {
                Background = offWhite,
                Surface = paper,
                SurfaceAlt = new Color(233, 228, 218),
                Text = charcoal,
                HeadingText = charcoal,
                MutedText = muted,
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

            return new DemoThemePreset
            {
                Id = "studio",
                DisplayName = "Soft Studio",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: paper,
                    buttonBackground: paper,
                    buttonHover: new Color(59, 130, 246, 60),
                    buttonPressed: new Color(255, 107, 94, 95),
                    tooltipBackground: charcoal,
                    tooltipText: offWhite),
                Palette = palette,
                FontName = "atkinsonhyperlegible",
                ClearColor = offWhite,
                BackgroundColor = offWhite
            };
        }

        private static DemoThemePreset CreateAurora()
        {
            var graphite = new Color(15, 18, 24);
            var deepTeal = new Color(17, 45, 40);
            var panel = new Color(25, 31, 34);
            var panelAlt = new Color(38, 48, 51);
            var field = new Color(11, 15, 18);
            var text = new Color(235, 247, 242);
            var muted = new Color(150, 174, 169);
            var mint = new Color(104, 228, 192);
            var lime = new Color(231, 247, 109);
            var amber = new Color(255, 178, 93);
            var coral = new Color(255, 107, 122);
            var blue = new Color(124, 199, 255);
            var ink = new Color(12, 15, 18);

            var palette = new DemoThemePalette
            {
                Background = graphite,
                Surface = panel,
                SurfaceAlt = panelAlt,
                Text = text,
                HeadingText = Color.White,
                MutedText = muted,
                Primary = mint,
                Secondary = amber,
                Warning = amber,
                Danger = coral,
                Info = blue,
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

            return new DemoThemePreset
            {
                Id = "aurora",
                DisplayName = "Aurora Modern",
                CreateTheme = () => CreateTheme(
                    palette,
                    textBoxBackground: palette.FieldFrameBrush!,
                    buttonBackground: new GradientBrush(panelAlt, new Color(42, 60, 55), GradientDirection.DiagonalDown),
                    buttonHover: new GradientBrush(new Color(104, 228, 192, 85), new Color(231, 247, 109, 75), GradientDirection.Horizontal),
                    buttonPressed: new GradientBrush(new Color(255, 178, 93, 135), new Color(104, 228, 192, 120), GradientDirection.Horizontal),
                    tooltipBackground: new GradientBrush(new Color(8, 12, 14, 245), new Color(24, 38, 34, 245), GradientDirection.Vertical),
                    tooltipText: text),
                Palette = palette,
                FontName = "atkinsonhyperlegible",
                ClearColor = graphite,
                BackgroundColor = graphite
            };
        }

        private static PortableTheme CreateTheme(
            DemoThemePalette palette,
            Brush textBoxBackground,
            Brush buttonBackground,
            Brush buttonHover,
            Brush buttonPressed,
            Brush tooltipBackground,
            Color tooltipText)
        {
            return new PortableTheme
            {
                TextColor = palette.Text,
                TextSize = 14,
                FocusBorderBrush = Brush(palette.Primary),
                FocusBorderWidth = 2,
                DisabledOverlayBrush = Brush(new Color(0, 0, 0, 70)),
                DisabledTextColor = palette.DisabledText,

                ButtonPadding = new Thickness(8, 6),
                ButtonBackgroundBrush = buttonBackground,
                ButtonHoverBrush = buttonHover,
                ButtonPressedBrush = buttonPressed,
                ButtonTextColor = palette.Text,
                ButtonHoverTextColor = palette.Text,
                ButtonPressedTextColor = palette.SelectionText,

                ToggleBrush = SelectionBrush(palette),
                ToggleTextColor = palette.SelectionText,

                TextBoxBackgroundBrush = textBoxBackground,
                TextBoxTextColor = palette.Text,
                TextBoxCursorBrush = Brush(palette.Primary),
                TextBoxSelectionBrush = Brush(new Color((int)palette.Primary.R, (int)palette.Primary.G, (int)palette.Primary.B, 120)),
                TextBoxHintTextColor = palette.MutedText,
                TextBoxPadding = new Thickness(6, 4),
                TextBoxHeight = 32,

                ScrollBarThickness = 8,
                ScrollBarGutterBrush = SurfaceBrush(palette),
                ScrollBarBrush = Brush(palette.Primary),
                ScrollBarHoverBrush = Brush(palette.Secondary),
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
                CheckBoxBoxBorderBrush = Brush(palette.Primary),
                CheckBoxCheckMarkBrush = SelectionBrush(palette),
                CheckBoxTextColor = palette.Text,

                ToolTipBackgroundBrush = tooltipBackground,
                ToolTipBorderBrush = Brush(palette.Primary),
                ToolTipBorderWidth = new Thickness(1),
                ToolTipPadding = new Thickness(8, 5, 8, 6),
                ToolTipTextColor = tooltipText,

                ProgressIndicatorForeground = palette.Primary,
                ProgressIndicatorHeight = 48
            };
        }

        private static Brush SurfaceBrush(DemoThemePalette palette)
        {
            return palette.SurfaceBrush ?? Brush(palette.Surface);
        }

        private static Brush SelectionBrush(DemoThemePalette palette)
        {
            return palette.SelectionBrush ?? Brush(palette.Selection);
        }

        private static SolidColorBrush Brush(Color color)
        {
            return new SolidColorBrush(color);
        }
    }
}

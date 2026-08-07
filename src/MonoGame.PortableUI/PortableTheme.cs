using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI
{
    public sealed class PortableTheme
    {
        private static readonly PortableTheme Fallback = CreateDefault();

        public static PortableTheme CreateDefault()
        {
            return new PortableTheme();
        }

        /// <summary>
        ///     Builds a complete theme from the 19 semantic palette slots: flat properties and
        ///     per-state control styles get sensible defaults, so a custom theme is
        ///     "palette + a few overrides" instead of ~60 properties. This is the core building
        ///     block used by the MonoGame.PortableUI.Themes catalog and by hand-written themes.
        /// </summary>
        public static PortableTheme FromPalette(ThemePalette palette)
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
                ProgressBarFillBrush = SelectionBrush(palette),
                ToggleSwitchOffTrackBrush = FieldFrameBrush(palette),
                ToggleSwitchOnTrackBrush = SelectionBrush(palette),
                ToggleSwitchKnobBrush = Solid(ContrastColor(palette.Selection)),
                BadgeBackgroundBrush = Solid(palette.Danger),
                BadgeTextColor = ContrastColor(palette.Danger),
                DataGridHeaderBackgroundBrush = SurfaceBrush(palette),
                DataGridHeaderTextColor = palette.TabText,
                DataGridAlternateRowBackgroundBrush = palette.SurfaceAltBrush ?? Solid(palette.SurfaceAlt),
                DataGridGridLinesBrush = Solid(new Color((int)palette.MutedText.R, (int)palette.MutedText.G, (int)palette.MutedText.B, 60))
            };

            ApplyPaletteStyles(theme, palette);
            return theme;
        }

        /// <summary>Regenerates the per-state control styles of <paramref name="theme"/> from a palette.</summary>
        public static void ApplyPaletteStyles(PortableTheme theme, ThemePalette palette)
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

        /// <summary>Black or white, whichever reads better on the given color.</summary>
        private static Color ContrastColor(Color background)
        {
            var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return luminance > 0.55 ? Color.Black : Color.White;
        }

        internal static PortableTheme ResolveCurrent()
        {
            return ScreenEngine.Instance?.Options.Theme ?? Fallback;
        }

        public Color TextColor { get; set; } = Color.Black;
        public int TextSize { get; set; } = 14;
        public bool PixelSnapping { get; set; } = true;
        public ThemePalette Palette { get; set; } = ThemePalette.Empty;
        public Typography Typography { get; set; } = new Typography();
        public ThemeMetrics Metrics { get; set; } = new ThemeMetrics();
        public ControlStyle Button { get; set; } = new ControlStyle();
        public ControlStyle TextBox { get; set; } = new ControlStyle();
        public ControlStyle CheckBox { get; set; } = new ControlStyle();
        public ControlStyle RadioButton { get; set; } = new ControlStyle();
        public ControlStyle ToggleButton { get; set; } = new ControlStyle();
        public ControlStyle ComboBox { get; set; } = new ControlStyle();
        public ControlStyle ListBox { get; set; } = new ControlStyle();
        public ControlStyle ListBoxItem { get; set; } = new ControlStyle();
        public ControlStyle Tab { get; set; } = new ControlStyle();
        public ControlStyle ToolTip { get; set; } = new ControlStyle();
        public ControlStyle ContextMenu { get; set; } = new ControlStyle();
        public ControlStyle ScrollBar { get; set; } = new ControlStyle();
        public ControlStyle Slider { get; set; } = new ControlStyle();
        public ControlStyle ProgressBar { get; set; } = new ControlStyle();
        public ControlStyle Panel { get; set; } = new ControlStyle();
        public IReadOnlyList<PostEffect> PostEffects { get; set; } = Array.Empty<PostEffect>();

        /// <summary>Drop shadow applied to buttons; null = no shadow.</summary>
        public ShadowStyle? ButtonShadow { get; set; }

        /// <summary>Drop shadow apps/screens should apply to elevated panels; null = no shadow.</summary>
        public ShadowStyle? PanelShadow { get; set; }

        public Brush? FocusBorderBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));
        public float FocusBorderWidth { get; set; } = 2;
        public FocusVisualKind FocusVisualKind { get; set; } = FocusVisualKind.Rectangle;
        public Brush? DisabledOverlayBrush { get; set; } = new SolidColorBrush(new Color(210, 216, 222, 145));
        public Color? DisabledTextColor { get; set; } = Color.Gray;

        public Thickness ButtonPadding { get; set; } = new Thickness(8);
        /// <summary>Optional default outer margin for buttons — gives drop shadows breathing room.</summary>
        public Thickness? ButtonMargin { get; set; }
        public Brush? ButtonBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Brush ButtonHoverBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 0.2f));
        public Brush ButtonPressedBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 0.4f));
        public Color ButtonTextColor { get; set; } = Color.Black;
        public Color? ButtonHoverTextColor { get; set; }
        public Color? ButtonPressedTextColor { get; set; }

        public Brush ToggleBrush { get; set; } = new SolidColorBrush(new Color(0.3f, 0.3f, 0.3f));
        public Color? ToggleTextColor { get; set; } = Color.White;

        public Brush? TextBoxBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Color TextBoxTextColor { get; set; } = Color.Black;
        public Brush TextBoxCursorBrush { get; set; } = new SolidColorBrush(Color.Black);
        public Brush TextBoxSelectionBrush { get; set; } = new SolidColorBrush(new Color(51, 153, 255, 95));
        public Color TextBoxHintTextColor { get; set; } = Color.Silver;
        public Thickness TextBoxPadding { get; set; } = new Thickness(4);
        public float TextBoxHeight { get; set; } = 28;

        public float ScrollBarThickness { get; set; } = 8;
        public Brush? ScrollBarGutterBrush { get; set; } = new SolidColorBrush(new Color(245, 245, 245));
        public Brush ScrollBarBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 120));
        public Brush? ScrollBarHoverBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 160));
        public Brush? ScrollBarPressedBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 190));

        public float TabHeaderHeight { get; set; } = 32;
        public Brush TabHeaderBackgroundBrush { get; set; } = new SolidColorBrush(Color.Silver);
        public Brush TabSelectedHeaderBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Color TabHeaderTextColor { get; set; } = Color.Black;
        public Color TabSelectedHeaderTextColor { get; set; } = Color.Black;

        public Brush ContextMenuBackgroundBrush { get; set; } = new SolidColorBrush(Color.Silver);

        public float ComboBoxHeight { get; set; } = 32;
        public float ComboBoxDropDownMaxHeight { get; set; } = 160;
        public Brush ComboBoxDropDownBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        /// <summary>Color of the dropdown triangle at the right edge of the ComboBox; null uses the button text color.</summary>
        public Color? ComboBoxGlyphColor { get; set; }

        public Brush ListBoxBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public float ListBoxItemHeight { get; set; } = 28;
        public Thickness ListBoxItemPadding { get; set; } = new Thickness(8, 0);
        public Brush ListBoxItemBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Brush ListBoxSelectedItemBackgroundBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));
        public Color ListBoxItemTextColor { get; set; } = Color.Black;
        public Color ListBoxSelectedItemTextColor { get; set; } = Color.White;

        public float CheckBoxBoxSize { get; set; } = 20;
        public float CheckBoxBoxSpacing { get; set; } = 8;
        public float CheckBoxBoxBorderWidth { get; set; } = 2;
        public Brush? CheckBoxBoxBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Brush? CheckBoxBoxBorderBrush { get; set; } = new SolidColorBrush(new Color(82, 101, 111));
        public Brush? CheckBoxCheckMarkBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));
        public CheckBoxGlyphKind CheckBoxGlyphKind { get; set; } = CheckBoxGlyphKind.Cross;
        public Color CheckBoxTextColor { get; set; } = Color.Black;

        public Brush? RadioButtonDotBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));
        public float RadioButtonDotSize { get; set; } = 8;

        public Brush? ToolTipBackgroundBrush { get; set; } = new SolidColorBrush(new Color(31, 35, 39, 238));
        public Brush? ToolTipBorderBrush { get; set; } = new SolidColorBrush(new Color(255, 255, 255, 90));
        public Thickness ToolTipBorderWidth { get; set; } = new Thickness(1);
        public Thickness ToolTipPadding { get; set; } = new Thickness(8, 5, 8, 6);
        public Color ToolTipTextColor { get; set; } = Color.White;

        public Color ProgressIndicatorForeground { get; set; } = Color.DarkBlue;
        public float ProgressIndicatorHeight { get; set; } = 48;

        public float SliderHeight { get; set; } = 32;
        public float SliderWidth { get; set; } = 160;
        public float SliderTrackHeight { get; set; } = 4;
        public float SliderThumbSize { get; set; } = 18;
        public Brush SliderTrackBrush { get; set; } = new SolidColorBrush(new Color(210, 216, 222));
        public Brush SliderFillBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));
        public Brush SliderThumbBrush { get; set; } = new SolidColorBrush(Color.White);
        public Brush SliderThumbBorderBrush { get; set; } = new SolidColorBrush(new Color(82, 101, 111));

        public float ProgressBarHeight { get; set; } = 18;
        public float ProgressBarWidth { get; set; } = 160;
        public Brush ProgressBarBackgroundBrush { get; set; } = new SolidColorBrush(new Color(225, 230, 235));
        public Brush ProgressBarFillBrush { get; set; } = new SolidColorBrush(new Color(20, 126, 133));

        public Brush ToggleSwitchOffTrackBrush { get; set; } = new SolidColorBrush(new Color(255, 255, 255, 60));
        public Brush ToggleSwitchOnTrackBrush { get; set; } = new SolidColorBrush(new Color(96, 226, 219));
        public Brush ToggleSwitchKnobBrush { get; set; } = new SolidColorBrush(Color.White);

        public Brush BadgeBackgroundBrush { get; set; } = new SolidColorBrush(new Color(230, 80, 80));
        public Color BadgeTextColor { get; set; } = Color.White;

        public Brush DataGridHeaderBackgroundBrush { get; set; } = new SolidColorBrush(Color.Silver);
        public Color DataGridHeaderTextColor { get; set; } = Color.Black;
        public Brush DataGridAlternateRowBackgroundBrush { get; set; } = new SolidColorBrush(Color.White);
        public Brush DataGridGridLinesBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 28));
    }
}

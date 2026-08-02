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
    }
}

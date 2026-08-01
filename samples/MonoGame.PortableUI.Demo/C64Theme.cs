using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Demo
{
    internal static class C64Theme
    {
        public static readonly Color Blue = new Color(64, 64, 224);
        public static readonly Color DarkBlue = new Color(31, 32, 152);
        public static readonly Color LightBlue = new Color(148, 150, 255);
        public static readonly Color White = Color.White;
        public static readonly Color Green = new Color(72, 255, 72);
        public static readonly Color Red = new Color(255, 72, 72);
        public static readonly Color Yellow = new Color(255, 236, 96);
        public static readonly Color Cyan = new Color(120, 255, 255);

        public static PortableTheme Create()
        {
            return new PortableTheme
            {
                TextColor = White,
                TextSize = 14,
                FocusBorderBrush = new SolidColorBrush(White),
                FocusBorderWidth = 2,
                DisabledOverlayBrush = new SolidColorBrush(new Color(0, 0, 0, 95)),
                DisabledTextColor = LightBlue,

                ButtonPadding = new Thickness(8, 6),
                ButtonBackgroundBrush = new SolidColorBrush(Blue),
                ButtonHoverBrush = new SolidColorBrush(new Color(148, 150, 255, 80)),
                ButtonPressedBrush = new SolidColorBrush(new Color(255, 255, 255, 120)),
                ButtonTextColor = White,
                ButtonHoverTextColor = White,
                ButtonPressedTextColor = Blue,

                ToggleBrush = new SolidColorBrush(LightBlue),
                ToggleTextColor = Blue,

                TextBoxBackgroundBrush = new SolidColorBrush(White),
                TextBoxTextColor = Blue,
                TextBoxCursorBrush = new SolidColorBrush(Blue),
                TextBoxSelectionBrush = new SolidColorBrush(new Color(148, 150, 255, 150)),
                TextBoxHintTextColor = new Color(86, 88, 188),
                TextBoxPadding = new Thickness(6, 4),
                TextBoxHeight = 32,

                ScrollBarThickness = 8,
                ScrollBarGutterBrush = new SolidColorBrush(Blue),
                ScrollBarBrush = new SolidColorBrush(LightBlue),
                ScrollBarHoverBrush = new SolidColorBrush(White),
                ScrollBarPressedBrush = new SolidColorBrush(Cyan),

                TabHeaderHeight = 36,
                TabHeaderBackgroundBrush = new SolidColorBrush(Blue),
                TabSelectedHeaderBackgroundBrush = new SolidColorBrush(LightBlue),
                TabHeaderTextColor = White,
                TabSelectedHeaderTextColor = Blue,
                ContextMenuBackgroundBrush = new SolidColorBrush(Blue),

                ComboBoxHeight = 32,
                ComboBoxDropDownMaxHeight = 168,
                ComboBoxDropDownBackgroundBrush = new SolidColorBrush(Blue),

                ListBoxBackgroundBrush = new SolidColorBrush(Blue),
                ListBoxItemHeight = 28,
                ListBoxItemPadding = new Thickness(6, 0),
                ListBoxItemBackgroundBrush = new SolidColorBrush(Blue),
                ListBoxSelectedItemBackgroundBrush = new SolidColorBrush(White),
                ListBoxItemTextColor = White,
                ListBoxSelectedItemTextColor = Blue,

                CheckBoxBoxSize = 18,
                CheckBoxBoxSpacing = 8,
                CheckBoxBoxBorderWidth = 2,
                CheckBoxBoxBackgroundBrush = new SolidColorBrush(Blue),
                CheckBoxBoxBorderBrush = new SolidColorBrush(LightBlue),
                CheckBoxCheckMarkBrush = new SolidColorBrush(White),
                CheckBoxTextColor = White,

                ToolTipBackgroundBrush = new SolidColorBrush(DarkBlue),
                ToolTipBorderBrush = new SolidColorBrush(LightBlue),
                ToolTipBorderWidth = new Thickness(1),
                ToolTipPadding = new Thickness(8, 5, 8, 6),
                ToolTipTextColor = White,

                ProgressIndicatorForeground = LightBlue,
                ProgressIndicatorHeight = 48
            };
        }
    }
}

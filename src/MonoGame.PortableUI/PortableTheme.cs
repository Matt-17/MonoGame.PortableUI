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
                // Subtle by default: a thin, slightly translucent ring a little outside the control.
                FocusBorderBrush = Solid(new Color((int)palette.Primary.R, (int)palette.Primary.G, (int)palette.Primary.B, 190)),
                FocusBorderWidth = 1.5f,
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
                TextBoxPadding = new Thickness(10, 4),
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
            theme.PrimaryButton = styles["PrimaryButton"];
            theme.SecondaryButton = styles["SecondaryButton"];
            theme.DangerButton = styles["DangerButton"];
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
        internal static Color ContrastColor(Color background)
        {
            var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return luminance > 0.55 ? Color.Black : Color.White;
        }

        internal static PortableTheme ResolveCurrent()
        {
            return ScreenEngine.Instance?.Options.Theme ?? Fallback;
        }

        public Color TextColor { get; set; } = Color.Black;
        public float TextSize { get; set; } = 14;

        /// <summary>
        ///     Minimum width and height of buttons (radio and toggle buttons, combo boxes included), check boxes
        ///     and text boxes: a touch target, e.g. 44 like the .NET MAUI template or 48 like Material.
        ///     0 (default) = none. A control's own <c>MinWidth</c>/<c>MinHeight</c> wins.
        /// </summary>
        public float MinimumControlSize { get; set; }
        public bool PixelSnapping { get; set; } = true;

        /// <summary>
        ///     Pixel-art look: animations move and scale in whole pixels, the way sprites step on a low-res
        ///     screen. False (default): while a control moves (animation, transition, squash and stretch,
        ///     breathing) its shapes and text are drawn at their exact fractional position with smooth
        ///     edges, so motion glides; at rest everything stays pixel-snapped either way.
        /// </summary>
        public bool PixelMotion { get; set; }

        /// <summary>
        ///     What the UI sounds like in this theme (with <c>engine.Sounds.UseThemeSounds</c>). Null: derived -
        ///     <see cref="Audio.UiSoundSet.Retro"/> bleeps for <see cref="PixelMotion"/> themes, otherwise
        ///     <see cref="Audio.UiSoundSet.Soft"/>. Catalog themes pick by era (glass, terminal, retro).
        /// </summary>
        public Audio.UiSoundSet? Sounds { get; set; }

        /// <summary>
        ///     How the UI feels in this theme: the haptic pulse per event, strength and rumble length. Null:
        ///     derived - <see cref="Haptics.UiHapticSet.Punchy"/> for <see cref="PixelMotion"/> themes, otherwise
        ///     <see cref="Haptics.UiHapticSet.Default"/>. Catalog themes pick by era.
        /// </summary>
        public Haptics.UiHapticSet? Haptics { get; set; }

        /// <summary>The haptic set in effect: <see cref="Haptics"/> or the derived default.</summary>
        public Haptics.UiHapticSet ResolveHaptics() => Haptics ?? (PixelMotion ? MonoGame.PortableUI.Haptics.UiHapticSet.Punchy : MonoGame.PortableUI.Haptics.UiHapticSet.Default);

        /// <summary>The sound set in effect: <see cref="Sounds"/> or the derived default.</summary>
        public Audio.UiSoundSet ResolveSounds() => Sounds ?? (PixelMotion ? Audio.UiSoundSet.Retro : Audio.UiSoundSet.Soft);
        public ThemePalette Palette { get; set; } = ThemePalette.Empty;
        public Typography Typography { get; set; } = new Typography();
        public ThemeMetrics Metrics { get; set; } = new ThemeMetrics();
        public ControlStyle Button { get; set; } = new ControlStyle();

        /// <summary>
        ///     App-defined style slots, by name: a control with <see cref="Control.StyleKey"/> set takes its
        ///     style from here, so a theme can describe the app's own controls (menu rows, HUD pills) and a
        ///     theme switch restyles them. Empty by default; a key missing from a theme falls back to the
        ///     control's built-in slot.
        /// </summary>
        public Dictionary<string, ControlStyle> Styles { get; } = new Dictionary<string, ControlStyle>(StringComparer.Ordinal);

        /// <summary>Chrome of <see cref="Controls.ButtonVariant.Primary"/> buttons (palette default: Primary fill).</summary>
        public ControlStyle PrimaryButton { get; set; } = ControlStyleBuilder.Variant(new Color(20, 126, 133), Color.White, new Color(210, 216, 222));

        /// <summary>Chrome of <see cref="Controls.ButtonVariant.Secondary"/> buttons (palette default: Secondary fill).</summary>
        public ControlStyle SecondaryButton { get; set; } = ControlStyleBuilder.Variant(new Color(82, 101, 111), Color.White, new Color(210, 216, 222));

        /// <summary>Chrome of <see cref="Controls.ButtonVariant.Danger"/> buttons (palette default: Danger fill).</summary>
        public ControlStyle DangerButton { get; set; } = ControlStyleBuilder.Variant(new Color(178, 34, 34), Color.White, new Color(210, 216, 222));

        /// <summary>Hover overlay of variant buttons (drawn over their colored face).</summary>
        public Brush VariantButtonHoverBrush { get; set; } = new SolidColorBrush(new Color(255, 255, 255, 40));

        /// <summary>Pressed overlay of variant buttons.</summary>
        public Brush VariantButtonPressedBrush { get; set; } = new SolidColorBrush(new Color(0, 0, 0, 70));
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
        /// <summary>
        ///     Look effects of the theme (scanlines, bloom, grain, …), dropped at Low render quality.
        ///     Screen curvature is not a look: a <c>CrtBarrelPostEffect</c> (effects package) here is ignored —
        ///     put it on the display (<see cref="ScreenEngineOptions.PostEffects"/>).
        /// </summary>
        public IReadOnlyList<PostEffect> PostEffects { get; set; } = Array.Empty<PostEffect>();

        /// <summary>
        ///     The software mouse pointer drawn when <see cref="ScreenEngineOptions.ShowSoftwareCursor"/>
        ///     is on (in-world screens, games that hide the system pointer). Default: the fine modern arrow.
        /// </summary>
        public CursorStyle Cursor { get; set; } = CursorStyle.ModernArrow(Color.White, Color.Black);

        /// <summary>
        ///     Background of modal dialogs and sheets; null = the panel/context-menu background.
        ///     Glass themes give dialogs a denser glass than their cards so text behind stays out of the way.
        /// </summary>
        public Brush? ModalBackgroundBrush { get; set; }

        /// <summary>Shadow under modal dialogs (they float above everything); null = none.</summary>
        public ShadowStyle? ModalShadow { get; set; } = new ShadowStyle { Color = new Color(0, 0, 0, 90), Offset = new Vector2(0, 16), Blur = 32 };

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

        /// <summary>Caret shape of text boxes; text-mode themes use <see cref="CaretStyle.TextMode"/>.</summary>
        public CaretStyle TextBoxCaretStyle { get; set; } = CaretStyle.Bar;

        /// <summary>Half period of the caret blink (on, then off for as long). DOS: 133 ms ≈ 3.75 Hz.</summary>
        public TimeSpan TextBoxCaretBlinkInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public Brush TextBoxSelectionBrush { get; set; } = new SolidColorBrush(new Color(51, 153, 255, 95));

        /// <summary>
        ///     Colour of selected text in text boxes. Null (default) keeps the text colour under a
        ///     translucent highlight and inverts it under an opaque one: the box's background colour
        ///     when it reads well on the highlight (Windows 3.1, Game Boy), otherwise black or white.
        /// </summary>
        public Color? TextBoxSelectionTextColor { get; set; }
        public Color TextBoxHintTextColor { get; set; } = Color.Silver;
        public Thickness TextBoxPadding { get; set; } = new Thickness(10, 4);
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

        /// <summary>Thickness of a progress bar's track and fill, centered in the control; null fills it.</summary>
        public float? ProgressBarTrackHeight { get; set; }

        /// <summary>Padding of a combo box's text; null uses <see cref="ButtonPadding"/> (Android Picker: 4, 10).</summary>
        public Thickness? ComboBoxPadding { get; set; }

        /// <summary>Width of a combo box's dropdown triangle; 0 hides it.</summary>
        public float ComboBoxGlyphSize { get; set; } = 10;

        /// <summary>Color of a combo box's placeholder text; null uses <see cref="TextBoxHintTextColor"/>.</summary>
        public Color? ComboBoxPlaceholderColor { get; set; }

        /// <summary>Space from a slider's edges to its track ends (Android: 16); null = half the thumb.</summary>
        public float? SliderTrackInset { get; set; }

        /// <summary>Corner radius of a slider's track and fill (a pill: half the track height).</summary>
        public CornerRadius SliderTrackCornerRadius { get; set; }

        /// <summary>Corner radius of a slider's thumb (a circle: half the thumb size).</summary>
        public CornerRadius SliderThumbCornerRadius { get; set; }

        /// <summary>Default size of a <c>ToggleSwitch</c> (iOS-style 96 x 52; Material 44 x 44 with a smaller track).</summary>
        public float ToggleSwitchWidth { get; set; } = 96;
        public float ToggleSwitchHeight { get; set; } = 52;

        /// <summary>Track size, centered in the switch; null fills it.</summary>
        public float? ToggleSwitchTrackWidth { get; set; }
        public float? ToggleSwitchTrackHeight { get; set; }

        /// <summary>Knob diameter; null fits it inside the track.</summary>
        public float? ToggleSwitchKnobSize { get; set; }

        /// <summary>Distance from a track end to the knob's center there; null = half the track height.</summary>
        public float? ToggleSwitchKnobTravelInset { get; set; }

        /// <summary>Knob while on; null keeps <see cref="ToggleSwitchKnobBrush"/>.</summary>
        public Brush? ToggleSwitchOnKnobBrush { get; set; }

        public Brush? ToggleSwitchDisabledTrackBrush { get; set; }
        public Brush? ToggleSwitchDisabledKnobBrush { get; set; }

        /// <summary>Shadow under the knob; null = none.</summary>
        public ShadowStyle? ToggleSwitchKnobShadow { get; set; }

        /// <summary>Diameter of a radio button's ring; null uses <see cref="CheckBoxBoxSize"/> (Material: 20).</summary>
        public float? RadioButtonRingSize { get; set; }

        /// <summary>Stroke width of a radio button's ring (Material: 2).</summary>
        public float RadioButtonRingThickness { get; set; } = 1.5f;

        /// <summary>Space left of a radio button's ring (Material: 6).</summary>
        public float RadioButtonRingInset { get; set; }

        /// <summary>Gap between a radio button's ring and its label; null uses <see cref="CheckBoxBoxSpacing"/>.</summary>
        public float? RadioButtonSpacing { get; set; }

        /// <summary>Ring outline; null uses <see cref="CheckBoxBoxBorderBrush"/> (Material: the secondary text color).</summary>
        public Brush? RadioButtonRingBrush { get; set; }

        /// <summary>Ring outline while checked; null keeps the unchecked ring (Material: the accent).</summary>
        public Brush? RadioButtonCheckedRingBrush { get; set; }

        /// <summary>Corner radius of the check box square (Material: 2).</summary>
        public CornerRadius CheckBoxBoxCornerRadius { get; set; }

        /// <summary>Box fill while checked (Material: the accent); null keeps <see cref="CheckBoxBoxBackgroundBrush"/>.</summary>
        public Brush? CheckBoxCheckedBoxBackgroundBrush { get; set; }

        /// <summary>Box border while checked; null keeps <see cref="CheckBoxBoxBorderBrush"/>.</summary>
        public Brush? CheckBoxCheckedBoxBorderBrush { get; set; }

        /// <summary>Box border, and fill while checked, of a disabled check box; null keeps the enabled brushes.</summary>
        public Brush? CheckBoxDisabledBoxBrush { get; set; }
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

        /// <summary>Squares (default) or Material's rotating arc.</summary>
        public ProgressIndicatorStyle ProgressIndicatorStyle { get; set; }

        /// <summary>Stroke width of the circular arc indicator (Material: 4).</summary>
        public float ProgressIndicatorStrokeWidth { get; set; } = 4;

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

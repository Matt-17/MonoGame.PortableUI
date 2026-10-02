using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ComboBox : Button
    {
        private int _selectedIndex = -1;
        // SelectedIndex set while Items is still empty would clamp to -1 and be lost; it is kept
        // here and applied once items arrive (on the next layout pass).
        private int _pendingSelectedIndex = -1;

        public ComboBox()
        {
            var theme = PortableTheme.ResolveCurrent();

            Items = new List<object>();
            Height = theme.ComboBoxHeight;
            TextAlignment = TextAlignment.Left;
            DropDownMaxHeight = theme.ComboBoxDropDownMaxHeight;
            ItemHeight = theme.ListBoxItemHeight;
            DropDownBackgroundBrush = theme.ComboBoxDropDownBackgroundBrush;
            ItemBackgroundBrush = theme.ListBoxItemBackgroundBrush;
            SelectedItemBackgroundBrush = theme.ListBoxSelectedItemBackgroundBrush;
            ItemTextColor = theme.ListBoxItemTextColor;
            SelectedItemTextColor = theme.ListBoxSelectedItemTextColor;
            GlyphColor = theme.ComboBoxGlyphColor;
            // ComboBoxes may need their own text color (e.g. Turbo Vision: yellow on blue while
            // dialog buttons are black on gray) — the ComboBox style slot wins over ButtonTextColor.
            if (theme.ComboBox.Normal.TextColor is { } styleTextColor)
                TextColor = styleTextColor;
            // Reserve room on the right so text never overlaps the dropdown glyph.
            Padding = WithGlyphReserve(Padding);
            Click += ComboBoxClick;
        }

        public List<object> Items { get; }
        public float DropDownMaxHeight { get; set; }
        public float ItemHeight { get; set; }
        public Brush DropDownBackgroundBrush { get; set; }
        public Brush ItemBackgroundBrush { get; set; }
        public Brush SelectedItemBackgroundBrush { get; set; }
        public Color ItemTextColor { get; set; }
        public Color SelectedItemTextColor { get; set; }
        /// <summary>Color of the dropdown triangle; null falls back to the current text color.</summary>
        public Color? GlyphColor { get; set; }
        public float GlyphSize { get; set; } = 10;

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return UseThemeStyle ? theme.ComboBox : null;
        }

        private Thickness WithGlyphReserve(Thickness padding)
        {
            return new Thickness(padding.Left, padding.Top, padding.Right + GlyphSize + 8, padding.Bottom);
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            // Decide what was theme-seeded before base runs: Button re-seeds TextColor itself, after
            // which the ComboBox-specific comparison would no longer match.
            var textWasThemeDefault = TextColor.Equals(oldTheme.ComboBox.Normal.TextColor ?? oldTheme.ButtonTextColor);
            var paddingWasThemeDefault = Padding.Equals(WithGlyphReserve(oldTheme.ButtonPadding));

            base.OnThemeChanged(oldTheme, newTheme);

            if (textWasThemeDefault)
                TextColor = newTheme.ComboBox.Normal.TextColor ?? newTheme.ButtonTextColor;
            if (paddingWasThemeDefault)
                Padding = WithGlyphReserve(newTheme.ButtonPadding);

            if (Height.Equals(oldTheme.ComboBoxHeight))
                Height = newTheme.ComboBoxHeight;
            if (DropDownMaxHeight.Equals(oldTheme.ComboBoxDropDownMaxHeight))
                DropDownMaxHeight = newTheme.ComboBoxDropDownMaxHeight;
            if (ItemHeight.Equals(oldTheme.ListBoxItemHeight))
                ItemHeight = newTheme.ListBoxItemHeight;
            if (ReferenceEquals(DropDownBackgroundBrush, oldTheme.ComboBoxDropDownBackgroundBrush))
                DropDownBackgroundBrush = newTheme.ComboBoxDropDownBackgroundBrush;
            if (ReferenceEquals(ItemBackgroundBrush, oldTheme.ListBoxItemBackgroundBrush))
                ItemBackgroundBrush = newTheme.ListBoxItemBackgroundBrush;
            if (ReferenceEquals(SelectedItemBackgroundBrush, oldTheme.ListBoxSelectedItemBackgroundBrush))
                SelectedItemBackgroundBrush = newTheme.ListBoxSelectedItemBackgroundBrush;
            if (ItemTextColor.Equals(oldTheme.ListBoxItemTextColor))
                ItemTextColor = newTheme.ListBoxItemTextColor;
            if (SelectedItemTextColor.Equals(oldTheme.ListBoxSelectedItemTextColor))
                SelectedItemTextColor = newTheme.ListBoxSelectedItemTextColor;
            if (Nullable.Equals(GlyphColor, oldTheme.ComboBoxGlyphColor))
                GlyphColor = newTheme.ComboBoxGlyphColor;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            DrawDropDownGlyph(spriteBatch, rect);
        }

        private void DrawDropDownGlyph(SpriteBatch spriteBatch, Rect rect)
        {
            // rect is in render space (LayoutScale, popup zoom): scale the layout-unit glyph metrics.
            var width = ToRender(GlyphSize);
            if (width <= 0 || rect.Width < width * 2)
                return;

            var height = width * 0.6f;
            var glyphRect = new Rect(
                rect.Right - width - ToRenderX(10),
                rect.Top + (rect.Height - height) / 2,
                width,
                height);
            var color = Brush.ApplyOpacity(GlyphColor ?? TextColor, RenderOpacity);
            spriteBatch.Draw(TriangleGlyph.Get(spriteBatch.GraphicsDevice, pointingUp: false), glyphRect, color);
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _pendingSelectedIndex = Items.Count == 0 && value >= 0 ? value : -1;
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;
                var oldIndex = _selectedIndex;
                _selectedIndex = clamped;
                UpdateDisplay();
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldIndex, clamped));
            }
        }

        /// <summary>Items is a plain list: removals surface in the layout pass, where the selection
        /// is clamped (with SelectionChanged) and the shown text follows the selected item.</summary>
        public override Size MeasureLayout()
        {
            if (_pendingSelectedIndex >= 0 && Items.Count > 0)
                SelectedIndex = _pendingSelectedIndex;

            var clamped = ClampIndex(_selectedIndex);
            if (_selectedIndex != clamped)
                SelectedIndex = clamped;
            else
                UpdateDisplay();
            return base.MeasureLayout();
        }

        private Func<object, Control>? _itemTemplate;
        private object? _templatedItem;

        /// <summary>Visual for items in the dropdown and for the selected item in the closed box
        /// (instead of <c>ToString()</c>).</summary>
        public Func<object, Control>? ItemTemplate
        {
            get => _itemTemplate;
            set
            {
                _itemTemplate = value;
                _templatedItem = null;
                UpdateDisplay();
                InvalidateLayout(true);
            }
        }

        private void UpdateDisplay()
        {
            var item = SelectedItem;
            if (ItemTemplate != null && item != null)
            {
                if (!ReferenceEquals(_templatedItem, item) || Content is TextBlock)
                {
                    Content = ItemTemplate(item);
                    _templatedItem = item;
                }
                return;
            }

            _templatedItem = null;
            if (Content != null && Content is not TextBlock)
                Content = null;
            var text = item?.ToString() ?? "";
            if (Text != text)
                Text = text;
        }

        public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

        private void ComboBoxClick(object? sender, EventArgs e)
        {
            if (Screen == null || Items.Count == 0)
                return;

            var listBox = CreateDropDownListBox();
            listBox.ItemInvoked += DropDownItemInvoked;

            var targetHeight = Math.Max(0, Math.Min(DropDownMaxHeight, Items.Count * ItemHeight));
            listBox.Width = (BoundingRect - Margin).Width;
            listBox.Height = targetHeight;

            var bounds = BoundingRect - Margin;
            // Dropdown opens below the box, or above it when there is no room below.
            Screen.ShowFlyOut(bounds, listBox, false, this, FlyOutPlacement.Below);
            listBox.ScrollSelectedIntoView();
        }

        internal ListBox CreateDropDownListBox()
        {
            var listBox = new ListBox
            {
                BackgroundBrush = DropDownBackgroundBrush,
                ItemHeight = ItemHeight,
                ItemBackgroundBrush = ItemBackgroundBrush,
                SelectedItemBackgroundBrush = SelectedItemBackgroundBrush,
                ItemTextColor = ItemTextColor,
                SelectedItemTextColor = SelectedItemTextColor,
                SelectedIndex = ClampIndex(SelectedIndex),
                ItemTemplate = ItemTemplate
            };

            foreach (var item in Items)
                listBox.Items.Add(item);

            listBox.SelectedIndex = ClampIndex(SelectedIndex);
            return listBox;
        }

        private void DropDownItemInvoked(object? sender, ListBoxItemInvokedEventArgs args)
        {
            SelectedIndex = args.Index;
            Screen?.ClearFlyOut();
        }

        private int ClampIndex(int value)
        {
            return HelperEx.ClampSelectionIndex(value, Items.Count);
        }
    }
}

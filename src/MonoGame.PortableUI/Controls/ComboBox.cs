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
            Padding = new Thickness(Padding.Left, Padding.Top, Padding.Right + GlyphSize + 8, Padding.Bottom);
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

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

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
            if (newTheme.ComboBox.Normal.TextColor is { } styleTextColor && TextColor.Equals(oldTheme.ComboBox.Normal.TextColor ?? oldTheme.ButtonTextColor))
                TextColor = styleTextColor;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            DrawDropDownGlyph(spriteBatch, rect);
        }

        private void DrawDropDownGlyph(SpriteBatch spriteBatch, Rect rect)
        {
            var width = GlyphSize;
            if (width <= 0 || rect.Width < width * 2)
                return;

            var height = width * 0.6f;
            var glyphRect = new Rect(
                rect.Right - width - 10,
                rect.Top + (rect.Height - height) / 2,
                width,
                height);
            var color = Brush.ApplyOpacity(GlyphColor ?? TextColor, RenderOpacity);
            spriteBatch.Draw(GetGlyphTexture(spriteBatch.GraphicsDevice), glyphRect, color);
        }

        private static Texture2D GetGlyphTexture(GraphicsDevice graphicsDevice)
        {
            const int width = 30;
            const int height = 18;
            return BrushTextureCache.GetOrCreate(graphicsDevice, new BrushTextureCacheKey("combobox-glyph-v1", width, height), device =>
            {
                var data = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    // Downward triangle: row y spans inset..width-inset, antialiased via 4x supersampling.
                    for (var x = 0; x < width; x++)
                    {
                        var covered = 0;
                        for (var sy = 0; sy < 2; sy++)
                        {
                            for (var sx = 0; sx < 2; sx++)
                            {
                                var py = y + 0.25f + sy * 0.5f;
                                var px = x + 0.25f + sx * 0.5f;
                                var inset = py / height * (width / 2f);
                                if (px >= inset && px <= width - inset)
                                    covered++;
                            }
                        }

                        var coverage = (byte)(covered * 255 / 4);
                        data[y * width + x] = new Color(coverage, coverage, coverage, coverage);
                    }
                }

                var texture = new Texture2D(device, width, height);
                texture.SetData(data);
                return texture;
            });
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = ClampIndex(value);
                if (_selectedIndex == clamped)
                    return;
                _selectedIndex = clamped;
                Text = SelectedItem?.ToString() ?? "";
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public object? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        public event EventHandler? SelectionChanged;

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
            Screen.ShowFlyOut(new PointF(bounds.Left, bounds.Bottom + targetHeight), listBox, false, this);
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
                SelectedIndex = ClampIndex(SelectedIndex)
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
            if (Items.Count == 0 || value < 0)
                return -1;
            return Math.Max(0, Math.Min(value, Items.Count - 1));
        }
    }
}

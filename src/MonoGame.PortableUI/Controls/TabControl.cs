using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class TabControl : Control
    {
        private int _selectedIndex;
        private readonly List<Button> _headerButtons = new List<Button>();

        public TabControl()
        {
            var theme = PortableTheme.ResolveCurrent();

            Items = new List<TabItem>();
            HeaderHeight = theme.TabHeaderHeight;
            HeaderBackground = theme.TabHeaderBackgroundBrush;
            SelectedHeaderBackground = theme.TabSelectedHeaderBackgroundBrush;
            HeaderTextColor = theme.TabHeaderTextColor;
            SelectedHeaderTextColor = theme.TabSelectedHeaderTextColor;
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (HeaderHeight.Equals(oldTheme.TabHeaderHeight))
                HeaderHeight = newTheme.TabHeaderHeight;
            if (ReferenceEquals(HeaderBackground, oldTheme.TabHeaderBackgroundBrush))
                HeaderBackground = newTheme.TabHeaderBackgroundBrush;
            if (ReferenceEquals(SelectedHeaderBackground, oldTheme.TabSelectedHeaderBackgroundBrush))
                SelectedHeaderBackground = newTheme.TabSelectedHeaderBackgroundBrush;
            if (HeaderTextColor.Equals(oldTheme.TabHeaderTextColor))
                HeaderTextColor = newTheme.TabHeaderTextColor;
            if (SelectedHeaderTextColor.Equals(oldTheme.TabSelectedHeaderTextColor))
                SelectedHeaderTextColor = newTheme.TabSelectedHeaderTextColor;
            InvalidateLayout(true);
        }

        public List<TabItem> Items { get; }

        public float HeaderHeight { get; set; }
        public Brush HeaderBackground { get; set; }
        public Brush SelectedHeaderBackground { get; set; }
        public Color HeaderTextColor { get; set; }
        public Color SelectedHeaderTextColor { get; set; }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = ClampSelectedIndex(value);
                if (_selectedIndex == clamped)
                    return;
                _selectedIndex = clamped;
                InvalidateLayout(true);
            }
        }

        public TabItem? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        public override Size MeasureLayout()
        {
            var size = base.MeasureLayout();
            if (Width.IsFixed() && Height.IsFixed())
                return size;

            var selectedSize = SelectedItem?.MeasureLayout() ?? Size.Empty;
            if (!Width.IsFixed())
                size.Width = System.Math.Max(size.Width, selectedSize.Width);
            if (!Height.IsFixed())
                size.Height = System.Math.Max(size.Height, HeaderHeight + selectedSize.Height);
            return ApplyConstraints(size);
        }

        public override void UpdateLayout(Rect rect)
        {
            SelectedIndex = ClampSelectedIndex(SelectedIndex);
            EnsureHeaderButtons();
            base.UpdateLayout(rect);

            var contentRect = BoundingRect - Margin;
            if (_headerButtons.Count > 0 && contentRect.Width > 0)
            {
                // Distribute the strip proportionally to each header's measured width so long
                // labels are not cut while short ones don't hog space.
                var measured = new float[_headerButtons.Count];
                var total = 0f;
                for (var i = 0; i < _headerButtons.Count; i++)
                {
                    measured[i] = System.Math.Max(1, _headerButtons[i].MeasureLayout().Width);
                    total += measured[i];
                }

                var scale = contentRect.Width / total;
                var left = contentRect.Left;
                for (var i = 0; i < _headerButtons.Count; i++)
                {
                    var width = measured[i] * scale;
                    _headerButtons[i].UpdateLayout(new Rect(left, contentRect.Top, width, HeaderHeight));
                    left += width;
                }
            }

            var selectedItem = SelectedItem;
            if (selectedItem != null)
            {
                selectedItem.Parent = this;
                selectedItem.UpdateLayout(new Rect(contentRect.Left, contentRect.Top + HeaderHeight, contentRect.Width, System.Math.Max(0, contentRect.Height - HeaderHeight)));
            }
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
        }

        public override IEnumerable<Control> GetDescendants()
        {
            EnsureHeaderButtons();
            foreach (var headerButton in _headerButtons)
                yield return headerButton;
            if (SelectedItem != null)
                yield return SelectedItem;
        }

        private void EnsureHeaderButtons()
        {
            while (_headerButtons.Count > Items.Count)
            {
                var last = _headerButtons[_headerButtons.Count - 1];
                last.Parent = null;
                _headerButtons.RemoveAt(_headerButtons.Count - 1);
            }

            while (_headerButtons.Count < Items.Count)
            {
                var index = _headerButtons.Count;
                var button = new Button
                {
                    Height = HeaderHeight,
                    Parent = this,
                    TextAlignment = TextAlignment.Center,
                    Shadow = null,
                    Margin = new Thickness(0),
                    UseThemeStyle = false
                };
                button.Click += (sender, args) =>
                {
                    if (sender is Button { Tag: int tabIndex })
                        SelectedIndex = tabIndex;
                };
                _headerButtons.Add(button);
            }

            for (var i = 0; i < _headerButtons.Count; i++)
            {
                var item = Items[i];
                item.Parent = i == SelectedIndex ? this : null;
                var button = _headerButtons[i];
                button.Tag = i;
                var headerText = string.IsNullOrEmpty(item.Header) ? $"Tab {i + 1}" : item.Header;
                if (button.Text != headerText)
                    button.Text = headerText;

                var headerBrush = i == SelectedIndex ? SelectedHeaderBackground : HeaderBackground;
                if (!ReferenceEquals(button.BackgroundBrush, headerBrush))
                    button.BackgroundBrush = headerBrush;

                var headerTextColor = i == SelectedIndex ? SelectedHeaderTextColor : HeaderTextColor;
                if (button.TextColor != headerTextColor)
                    button.TextColor = headerTextColor;
                if (button.HoverTextColor != headerTextColor)
                    button.HoverTextColor = headerTextColor;
                if (button.PressedTextColor != headerTextColor)
                    button.PressedTextColor = headerTextColor;
            }
        }

        private int ClampSelectedIndex(int value)
        {
            if (Items.Count == 0)
                return -1;
            return System.Math.Max(0, System.Math.Min(value, Items.Count - 1));
        }
    }
}

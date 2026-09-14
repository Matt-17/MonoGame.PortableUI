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
        // SelectedIndex set while Items is still empty would clamp to -1 and be lost; it is kept
        // here and applied once items arrive (on the next layout pass).
        private int _pendingSelectedIndex = -1;
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
                _pendingSelectedIndex = Items.Count == 0 && value >= 0 ? value : -1;
                var clamped = ClampSelectedIndex(value);
                if (_selectedIndex == clamped)
                    return;
                var oldIndex = _selectedIndex;
                _selectedIndex = clamped;
                InvalidateLayout(true);
                SelectionChanged?.Invoke(this, new Events.SelectionChangedEventArgs(oldIndex, clamped));
            }
        }

        public event System.EventHandler<Events.SelectionChangedEventArgs>? SelectionChanged;

        public TabItem? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

        // A page taller than the control must not draw over whatever sits below the TabControl.
        protected internal override bool ClipsDescendants => true;

        public override Size MeasureLayout()
        {
            if (IsGone || (Width.IsFixed() && Height.IsFixed()))
                return base.MeasureLayout();

            // Constraints bound the content box; margin is added once, afterwards.
            var selectedSize = SelectedItem?.Measure() ?? Size.Empty;
            var size = new Size(
                Width.IsFixed() ? Width : selectedSize.Width,
                Height.IsFixed() ? Height : HeaderHeight + selectedSize.Height);
            return ApplyConstraints(size) + Margin;
        }

        public override void UpdateLayout(Rect rect)
        {
            SelectedIndex = _pendingSelectedIndex >= 0 && Items.Count > 0
                ? _pendingSelectedIndex
                : ClampSelectedIndex(SelectedIndex);
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
                    measured[i] = System.Math.Max(1, _headerButtons[i].Measure().Width);
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

        public override IEnumerable<Control> GetDescendants()
        {
            // Full header sync happens in UpdateLayout; only a structural mismatch (tabs added or
            // removed without an invalidation) forces a rebuild in this per-frame path.
            if (_headerButtons.Count != Items.Count)
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

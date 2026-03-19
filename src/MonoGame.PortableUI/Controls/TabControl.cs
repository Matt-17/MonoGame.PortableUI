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
            Items = new List<TabItem>();
            HeaderHeight = 32;
            HeaderBackground = new SolidColorBrush(Color.Silver);
            SelectedHeaderBackground = new SolidColorBrush(Color.White);
        }

        public List<TabItem> Items { get; }

        public float HeaderHeight { get; set; }
        public Brush HeaderBackground { get; set; }
        public Brush SelectedHeaderBackground { get; set; }

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

        public TabItem SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

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
            var headerWidth = Items.Count == 0 ? 0 : contentRect.Width / Items.Count;
            for (var i = 0; i < _headerButtons.Count; i++)
            {
                _headerButtons[i].UpdateLayout(new Rect(contentRect.Left + headerWidth * i, contentRect.Top, headerWidth, HeaderHeight));
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
                    TextAlignment = TextAlignment.Center
                };
                button.Click += (sender, args) => SelectedIndex = (int)((Button)sender).Tag;
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

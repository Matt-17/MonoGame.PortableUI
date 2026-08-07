using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     A small pill/dot that communicates a count or simple state — typically a notification
    ///     count on a tile, tab or icon. Renders as a rounded pill (fully rounded, so a single
    ///     digit reads as a circle) with a coloured background and centred label. Set
    ///     <see cref="Count"/> for a number; a count of 0 hides the badge unless
    ///     <see cref="ShowZero"/> is set. Set <see cref="Dot"/> for a label-less status dot.
    /// </summary>
    public class Badge : ContentControl
    {
        private readonly TextBlock _label;
        private int _count;
        private bool _dot;
        private bool _showZero;
        private Color _badgeColor;

        public Badge()
        {
            var theme = PortableTheme.ResolveCurrent();

            IsFocusable = false;
            // Fully rounded: the renderer clamps the radius to min(w,h)/2, so this yields a pill.
            CornerRadius = 999;
            Padding = new Thickness(6, 1);
            MinWidth = 20;
            MinHeight = 20;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            BackgroundBrush = theme.BadgeBackgroundBrush;
            if (theme.BadgeBackgroundBrush is SolidColorBrush solid)
                _badgeColor = solid.Color;

            _label = new TextBlock
            {
                Text = "0",
                TextColor = theme.BadgeTextColor,
                TextSize = 12,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Content = _label;
            UpdateVisibility();
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(BackgroundBrush, oldTheme.BadgeBackgroundBrush))
            {
                BackgroundBrush = newTheme.BadgeBackgroundBrush;
                if (newTheme.BadgeBackgroundBrush is SolidColorBrush solid)
                    _badgeColor = solid.Color;
            }
            if (_label.TextColor.Equals(oldTheme.BadgeTextColor))
                _label.TextColor = newTheme.BadgeTextColor;
        }

        /// <summary>The number shown in the badge. 0 hides it (unless <see cref="ShowZero"/>).</summary>
        public int Count
        {
            get { return _count; }
            set
            {
                if (_count == value)
                    return;
                _count = value;
                _label.Text = value > 99 ? "99+" : value.ToString();
                UpdateVisibility();
            }
        }

        /// <summary>When true, render a small label-less status dot instead of a number.</summary>
        public bool Dot
        {
            get { return _dot; }
            set
            {
                if (_dot == value)
                    return;
                _dot = value;
                if (value)
                {
                    Padding = new Thickness(0);
                    MinWidth = 10;
                    MinHeight = 10;
                    _label.IsVisible = false;
                }
                else
                {
                    Padding = new Thickness(6, 1);
                    MinWidth = 20;
                    MinHeight = 20;
                    _label.IsVisible = true;
                }
                UpdateVisibility();
                InvalidateLayout(true);
            }
        }

        /// <summary>Show the badge even when <see cref="Count"/> is 0.</summary>
        public bool ShowZero
        {
            get { return _showZero; }
            set
            {
                if (_showZero == value)
                    return;
                _showZero = value;
                UpdateVisibility();
            }
        }

        /// <summary>The badge fill colour.</summary>
        public Color BadgeColor
        {
            get { return _badgeColor; }
            set
            {
                _badgeColor = value;
                BackgroundBrush = new SolidColorBrush(value);
            }
        }

        /// <summary>The label colour.</summary>
        public Color TextColor
        {
            get { return _label.TextColor; }
            set { _label.TextColor = value; }
        }

        private void UpdateVisibility()
        {
            IsVisible = _dot || _showZero || _count > 0;
        }
    }
}

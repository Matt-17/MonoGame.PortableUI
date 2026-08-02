using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ToggleButton : Button
    {
        private Brush? _backgroundColor;
        private bool _isChecked;
        private Color? _toggleTextColor;
        private Brush _toggleBrush = new SolidColorBrush(Color.White);

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                    return;
                _isChecked = value;
                if (_backgroundColor == null)
                    _backgroundColor = BackgroundBrush;
                BackgroundBrush = IsChecked ? ToggleBrush : _backgroundColor;
                OnChecked(IsChecked);
                ChangeVisualState();
            }
        }

        public Brush ToggleBrush
        {
            get { return _toggleBrush; }
            set
            {
                _toggleBrush = value;
                if (IsChecked)
                    BackgroundBrush = _toggleBrush;
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        internal override void ChangeVisualState()
        {
            base.ChangeVisualState();
            if (IsChecked && IsEnabled)
            {
                var textBlock = Content as TextBlock;
                if (textBlock != null && ToggleTextColor.HasValue)
                    textBlock.TextColor = (Color)ToggleTextColor;
            }
        }

        public Color? ToggleTextColor
        {
            get { return _toggleTextColor; }
            set
            {
                _toggleTextColor = value;
                ChangeVisualState();      
            }
        }

        public event EventHandler<CheckedEventArgs>? Checked;

        public ToggleButton()
        {
            var theme = PortableTheme.ResolveCurrent();

            ToggleBrush = theme.ToggleBrush;
            ToggleTextColor = theme.ToggleTextColor;
            Click += ToggleButton_Click;
        }

        private void ToggleButton_Click(object? sender, System.EventArgs e)
        {
            IsChecked = !IsChecked;
        }

        protected override ControlStyle? GetThemeStyle(PortableTheme theme)
        {
            return UseThemeStyle ? theme.ToggleButton : null;
        }

        protected override ControlVisualState GetVisualState()
        {
            if (IsChecked && IsEnabled)
                return ControlVisualState.Checked;
            return base.GetVisualState();
        }

        protected override void OnThemeChanged(PortableTheme oldTheme, PortableTheme newTheme)
        {
            base.OnThemeChanged(oldTheme, newTheme);

            if (ReferenceEquals(ToggleBrush, oldTheme.ToggleBrush))
                _toggleBrush = newTheme.ToggleBrush;
            if (Nullable.Equals(ToggleTextColor, oldTheme.ToggleTextColor))
                _toggleTextColor = newTheme.ToggleTextColor;
            // The unchecked background captured on toggle is a snapshot of the old theme; drop it
            // so the button resolves the new theme again.
            if (_backgroundColor != null && ReferenceEquals(_backgroundColor, oldTheme.ButtonBackgroundBrush))
            {
                _backgroundColor = null;
                if (!IsChecked)
                    ClearBackgroundBrushOverride();
            }

            if (IsChecked)
                BackgroundBrush = _toggleBrush;
            ChangeVisualState();
        }

        protected virtual void OnChecked(bool e)
        {
            CheckedEventArgs args = new CheckedEventArgs { IsChecked = e };
            Checked?.Invoke(this, args);
        }
    }
}

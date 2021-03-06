using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ToggleButton : Button
    {
        private Brush _backgroundColor;
        private bool _isChecked;
        private Color? _toggleTextColor;
        private Brush _toggleBrush;

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
                ChangeVisualState();
                InvalidateLayout(false);
            }
        }

        internal override void ChangeVisualState()
        {
            base.ChangeVisualState();
            if (IsChecked)
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

        public event EventHandler<CheckedEventArgs> Checked;

        public ToggleButton()
        {
            ToggleBrush = new Color(0.3f, 0.3f, 0.3f);
            ToggleTextColor = Color.White;
            Click += ToggleButton_Click;
        }

        private void ToggleButton_Click(object sender, System.EventArgs e)
        {
            IsChecked = !IsChecked;
            OnChecked(IsChecked);
            ChangeVisualState();
        }

        protected virtual void OnChecked(bool e)
        {
            CheckedEventArgs args = new CheckedEventArgs { IsChecked = e };
            Checked?.Invoke(this, args);
        }
    }
}
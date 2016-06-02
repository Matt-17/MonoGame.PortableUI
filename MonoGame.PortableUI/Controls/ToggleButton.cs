using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public class ToggleButton : Button
    {
        private Brush _backgroundColor;
        private bool _isChecked;

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
                OnStateChanged();
            }
        }

        public Brush ToggleBrush { get; set; }
        protected override void OnStateChanged()
        {
            base.OnStateChanged();
            if (IsChecked)
            {
                var textBlock = Content as TextBlock;
                if (textBlock != null && ToggleTextColor.HasValue)
                    textBlock.TextColor = (Color)ToggleTextColor;
            }
        }

        public Color? ToggleTextColor { get; set; }

        public event EventHandler<CheckedEventArgs> Checked;

        public ToggleButton()
        {
            Click += ToggleButton_Click;
        }

        private void ToggleButton_Click(object sender, System.EventArgs e)
        {
            IsChecked = !IsChecked;
            OnChecked(IsChecked);
            OnStateChanged();
        }

        protected virtual void OnChecked(bool e)
        {
            CheckedEventArgs args = new CheckedEventArgs { IsChecked = e };
            Checked?.Invoke(this, args);
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class ToggleButton : Button
    {
        private Color? _backgroundColor;
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
                    _backgroundColor = BackgroundColor;
                BackgroundColor = IsChecked ? ToggleColor : _backgroundColor.Value;
            }
        }

        public Color ToggleColor { get; set; }

        public event EventHandler<CheckedEventArgs> Checked;

        public ToggleButton()
        {
            Click += ToggleButton_Click;
        }

        private void ToggleButton_Click(object sender, System.EventArgs e)
        {
            IsChecked = !IsChecked;
            OnChecked(IsChecked);
        }

        protected virtual void OnChecked(bool e)
        {
            CheckedEventArgs args = new CheckedEventArgs { IsChecked = e };
            Checked?.Invoke(this, args);
        }
    }
}
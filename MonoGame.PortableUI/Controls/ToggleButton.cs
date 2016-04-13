using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class ToggleButton : Button
    {
        public bool IsChecked { get; set; }

        public Color ToggleColor { get; set; }

        public event EventHandler<CheckedEventArgs> Checked;

        public ToggleButton()
        {
            Click += ToggleButton_Click;
        }

        protected internal override void OnUpdate(TimeSpan elapsed, Rectangle rect)
        {
            base.OnUpdate(elapsed, rect);
            if (IsChecked)
                (Child as TextBlock).TextColor = Color.Black;
            if (IsChecked)
                CurrentBackgroundColor = ToggleColor;
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
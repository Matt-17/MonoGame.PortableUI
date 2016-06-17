using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl, IDisposable
    {
        private readonly PointF _position;

        public FlyOut(PointF position)
        {
            _position = position;
            MouseDown += (sender, args) => Screen.FlyOut = null;
            TouchDown += (sender, args) => Screen.FlyOut = null;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var size = Content.MeasureLayout ();
            var pos = new Rect(_position, size);
            pos.Top -= size.Height;
            Content.UpdateLayout(pos);
        }

        public void Dispose()
        {
            Content = null;
        }
    }
}
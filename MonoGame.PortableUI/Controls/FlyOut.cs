using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Events;

namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl, IDisposable
    {
        private readonly PointF _position;

        public FlyOut(PointF position, bool removeOnRelease)
        {
            _position = position;
            if (removeOnRelease)
            {
                MouseUp += (sender, args) => { if (Screen != null) Screen.FlyOut = null; };
                TouchUp += (sender, args) => { if (Screen != null) Screen.FlyOut = null; };
            }
            else
            {
                MouseDown += (sender, args) => Screen.FlyOut = null;
                TouchDown += (sender, args) => Screen.FlyOut = null;
            }
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            var size = Content.MeasureLayout();
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
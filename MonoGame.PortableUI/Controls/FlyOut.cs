using System;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    public class FlyOut : ContentControl, IDisposable
    {
        private readonly Rect _position;

        public FlyOut(Rect position, Control content)
        {
            _position = position;
            Content = content;
            MouseDown += FlyOutMouseDown;
        }

        private void FlyOutMouseDown(object sender, Events.MouseButtonEventHandlerArgs args)
        {
            Screen.FlyOut = null;
        }

        public override void UpdateLayout(Rect rect)
        {
            base.UpdateLayout(rect);
            Content.UpdateLayout(_position);
        }

        public void Dispose()
        {
            Content = null;
        }
    }
}
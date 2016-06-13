using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public interface IFrameworkElement
    {
        Brush BackgroundBrush { get; set; }

        void InvalidateLayout(bool boundsChanged);
        IEnumerable<Control> GetDescendants();
    }
}
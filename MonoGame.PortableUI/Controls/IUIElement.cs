using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    public interface IUIElement
    {
        Color BackgroundColor { get; set; }

        void InvalidateLayout(bool boundsChanged);
        IEnumerable<Control> GetDescendants();
    }
}
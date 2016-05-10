using System.Collections.Generic;

namespace MonoGame.PortableUI.Controls
{
    public interface IUIElement
    {
        void InvalidateLayout(bool boundsChanged);
        IEnumerable<Control> GetDescendants();
    }
}
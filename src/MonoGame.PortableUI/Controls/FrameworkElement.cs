using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    public abstract class FrameworkElement
    {
        public virtual Brush? BackgroundBrush { get; set; }
        public abstract FrameworkElement? Parent { get; internal set; }

        public abstract void InvalidateLayout(bool boundsChanged);
        public abstract IEnumerable<Control> GetDescendants();
    }
}

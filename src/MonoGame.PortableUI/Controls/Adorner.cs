using System.Collections.ObjectModel;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     A non-modal layer above a screen's content (below popups and tool tips): selection handles,
    ///     drag guides, tutorial highlights, badges that follow other controls. It covers the whole screen
    ///     but only takes the pointer presses <see cref="HitTest"/> claims; every other press reaches the
    ///     page as usual. Add it to <see cref="Screen.Adorners"/>.
    /// </summary>
    public abstract class Adorner : ContentControl
    {
        protected Adorner()
        {
            IsFocusable = false;
        }

        /// <summary>
        ///     Whether a press (touch or mouse) at <paramref name="point"/> (layout units) belongs to this adorner;
        ///     it then gets the whole gesture. Default: the press hits the <see cref="ContentControl.Content"/>.
        /// </summary>
        protected internal virtual bool HitTest(PointF point)
            => Content is { IsVisible: true } content && content.ClippingRect.Contains(point);

        /// <summary>A press went to the page, not to this adorner (e.g. to close what it shows).</summary>
        protected internal virtual void OnPressOutside(PointF point)
        {
        }

        /// <summary>
        ///     Runs once per screen update before input; adorners that follow other controls re-place
        ///     themselves here (call <see cref="Control.RequestRedraw"/> when something moved).
        /// </summary>
        protected internal virtual void OnUpdate()
        {
        }
    }

    /// <summary>The adorners of a screen, bottom to top.</summary>
    public sealed class AdornerCollection : Collection<Adorner>
    {
        private readonly Screen _screen;

        internal AdornerCollection(Screen screen)
        {
            _screen = screen;
        }

        protected override void InsertItem(int index, Adorner item)
        {
            base.InsertItem(index, item);
            _screen.AttachAdorner(item);
        }

        protected override void SetItem(int index, Adorner item)
        {
            _screen.DetachAdorner(this[index]);
            base.SetItem(index, item);
            _screen.AttachAdorner(item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            _screen.DetachAdorner(item);
        }

        protected override void ClearItems()
        {
            var items = new Adorner[Count];
            CopyTo(items, 0);
            base.ClearItems();
            foreach (var item in items)
                _screen.DetachAdorner(item);
        }
    }
}

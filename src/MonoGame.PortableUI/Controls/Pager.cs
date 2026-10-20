using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Pages side by side, one screen wide each, swiped like tabs: the next page slides in with the finger, a
    ///     release settles on a whole page, past the first or last page there is only the overscroll stretch. Pages
    ///     may scroll vertically themselves. <see cref="PagePosition"/> follows the swipe continuously (for a tab
    ///     indicator), <see cref="PageChanged"/> fires when another page is chosen.
    /// </summary>
    public class Pager : ScrollViewer
    {
        private readonly PagesPanel _panel;
        private int _page;

        public Pager()
        {
            ScrollOrientation = Orientation.Horizontal;
            SnapsToPages = true;
            EnableRubberBanding = true;
            ShowScrollBars = false;
            _panel = new PagesPanel(this);
            Content = _panel;
            ScrollChanged += (_, _) => OnScrolled();
        }

        /// <summary>The pages, left to right.</summary>
        public ControlCollection Pages => _panel.Children;

        /// <summary>Where the pager stands in pages (0 = the first; 0.5 = halfway to the second).</summary>
        public float PagePosition => Viewport.Width > 0 ? Offset.X / Viewport.Width : _page;

        /// <summary>The page shown (the nearest while a swipe is under way).</summary>
        public int PageIndex => _page;

        /// <summary><see cref="PagePosition"/> changed (every frame of a swipe or glide).</summary>
        public event EventHandler? PagePositionChanged;

        /// <summary>The nearest page changed.</summary>
        public event EventHandler? PageChanged;

        /// <summary>Glides to page <paramref name="index"/> (or jumps there with <paramref name="animate"/> false).</summary>
        public void GoTo(int index, bool animate = true)
        {
            index = Math.Clamp(index, 0, Math.Max(0, Pages.Count - 1));
            var target = new PointF(index * Viewport.Width, 0);
            if (animate && Viewport.Width > 0)
                SnapTo(target);
            else
            {
                _page = index;
                ScrollTo(target);
            }
        }

        public override void UpdateLayout(Rect rect)
        {
            var width = (rect - Margin - Padding).Width;
            if (Math.Abs(width - _panel.PageWidth) > 0.01f)
            {
                // Pages follow the pager's width; keep the page shown in place.
                _panel.PageWidth = width;
                _panel.InvalidateLayout(true);
                base.UpdateLayout(rect);
                ScrollTo(new PointF(_page * Viewport.Width, 0));
                return;
            }
            base.UpdateLayout(rect);
        }

        private void OnScrolled()
        {
            PagePositionChanged?.Invoke(this, EventArgs.Empty);
            var page = Math.Clamp((int)MathF.Round(PagePosition), 0, Math.Max(0, Pages.Count - 1));
            if (page == _page)
                return;
            _page = page;
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Lays the pages out next to each other, each as wide as the pager.</summary>
        private sealed class PagesPanel : Panel
        {
            private readonly Pager _pager;

            public PagesPanel(Pager pager) => _pager = pager;

            public float PageWidth { get; set; }

            public override Size MeasureLayout()
            {
                foreach (var child in Children)
                    child.Measure();
                return new Size(PageWidth * Children.Count, 0);
            }

            public override void UpdateLayout(Rect rect)
            {
                base.UpdateLayout(rect);
                var height = _pager.Viewport.Height;
                for (var i = 0; i < Children.Count; i++)
                    Children[i].UpdateLayout(new Rect(rect.Left + i * PageWidth, rect.Top, PageWidth, height));
            }
        }
    }
}

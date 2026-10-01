using System;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>Which edges a <see cref="SafeAreaPanel"/> keeps clear.</summary>
    [Flags]
    public enum SafeAreaEdges
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
        All = Left | Top | Right | Bottom
    }

    /// <summary>
    ///     Pads its content so it stays inside <see cref="ScreenEngine.SafeAreaInsets"/> (notches,
    ///     system bars, the on-screen keyboard). Only the part of an inset that actually overlaps the
    ///     panel counts, so a panel that already sits away from an edge is not padded twice. Its
    ///     <see cref="Control.BackgroundBrush"/> still fills the whole area, under the cutout.
    /// </summary>
    public class SafeAreaPanel : ContentControl
    {
        private SafeAreaEdges _edges = SafeAreaEdges.All;
        private Thickness _extraPadding;

        public SafeAreaPanel()
        {
        }

        public SafeAreaPanel(Control content)
        {
            Content = content;
        }

        public SafeAreaEdges Edges
        {
            get => _edges;
            set
            {
                if (_edges == value)
                    return;
                _edges = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>Padding added on top of the safe-area inset on every edge.</summary>
        public Thickness ExtraPadding
        {
            get => _extraPadding;
            set
            {
                _extraPadding = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>The padding applied in the last layout pass.</summary>
        public Thickness AppliedInsets { get; private set; }

        public override Size MeasureLayout()
        {
            Padding = ComputePadding(null);
            return base.MeasureLayout();
        }

        public override void UpdateLayout(Rect rect)
        {
            Padding = ComputePadding(rect - Margin);
            AppliedInsets = Padding;
            base.UpdateLayout(rect);
        }

        private Thickness ComputePadding(Rect? box)
        {
            var engine = Screen?.ScreenEngine;
            var insets = engine?.SafeAreaInsets ?? default;
            var screen = engine?.ScreenRect ?? default;
            float left = 0, top = 0, right = 0, bottom = 0;
            if (box is { } b && engine != null)
            {
                // Overlap of each inset band with the panel.
                left = Math.Max(0, screen.Left + insets.Left - b.Left);
                top = Math.Max(0, screen.Top + insets.Top - b.Top);
                right = Math.Max(0, b.Right - (screen.Right - insets.Right));
                bottom = Math.Max(0, b.Bottom - (screen.Bottom - insets.Bottom));
            }
            else
            {
                left = insets.Left;
                top = insets.Top;
                right = insets.Right;
                bottom = insets.Bottom;
            }

            return new Thickness(
                ((_edges & SafeAreaEdges.Left) != 0 ? left : 0) + _extraPadding.Left,
                ((_edges & SafeAreaEdges.Top) != 0 ? top : 0) + _extraPadding.Top,
                ((_edges & SafeAreaEdges.Right) != 0 ? right : 0) + _extraPadding.Right,
                ((_edges & SafeAreaEdges.Bottom) != 0 ? bottom : 0) + _extraPadding.Bottom);
        }
    }
}

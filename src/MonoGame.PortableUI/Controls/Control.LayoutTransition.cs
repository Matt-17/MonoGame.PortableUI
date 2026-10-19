using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    // Layout transitions (FLIP): a control that a layout pass moves is drawn where it was and glides to its new place.
    public abstract partial class Control
    {
        private VectorTransition? _layoutGlide;
        // Drawn offset from the arranged position (layout units); zero at rest.
        private Vector2 _layoutOffset;
        // Arranged top-left at the last tracked arrangement; scrolling (OffsetArrangement) shifts it along.
        private PointF _layoutPosition;
        // How far the last arrangement moved this control, read by descendants arranged in the same pass.
        private PointF _layoutMove;
        private int _layoutMoveStamp = -1;
        private bool _layoutTracked;

        /// <summary>
        ///     Set once any layout transition is configured: until then the per-control tracking in every arrangement
        ///     (a hot path) is skipped entirely. Never reset - tracking must not have gaps once it started.
        /// </summary>
        internal static bool LayoutTransitionsInUse;

        /// <summary>The offset a running layout transition draws the control at (zero at rest).</summary>
        internal Vector2 LayoutOffset => _layoutOffset;

        /// <summary>True while a layout transition (FLIP) glides the control to its arranged position.</summary>
        public bool IsLayoutTransitioning => _layoutGlide is { IsRunning: true };

        /// <summary>The control's own <see cref="TransitionProperty.Layout"/> entry, else its panel's <see cref="Panel.LayoutTransition"/>.</summary>
        internal Transition? ResolveLayoutTransition()
            => _transitions?[TransitionProperty.Layout] ?? (Parent as Panel)?.LayoutTransition;

        /// <summary>
        ///     Called at the end of <see cref="UpdateLayout"/>: compares the arranged position with the last one and
        ///     glides by the difference. A move that an ancestor arranged in the same pass also made is the ancestor's
        ///     (its glide carries this control along), so only the part relative to it glides here.
        /// </summary>
        private void TrackLayoutMove()
        {
            // Scrolled content, swiped pages and recycled list rows move for reasons that are no layout change: they
            // never glide (a list keys its rows by item), but they are tracked so descendants measure their own
            // moves from them - otherwise every gliding control inside would glide along with a scroll.
            var boundary = Parent is ScrollViewer or SwipePresenter or VirtualItemsPanel;
            var spec = boundary ? null : ResolveLayoutTransition();
            if ((spec == null && !boundary) || IsGoneShown || (BoundingRect.Width <= 0 && BoundingRect.Height <= 0))
            {
                _layoutTracked = false;
                return;
            }

            var position = new PointF(BoundingRect.Left, BoundingRect.Top);
            var wasTracked = _layoutTracked;
            _layoutMove = wasTracked ? new PointF(position.X - _layoutPosition.X, position.Y - _layoutPosition.Y) : default;
            _layoutPosition = position;
            _layoutMoveStamp = _layoutPassId;
            _layoutTracked = true;
            if (spec == null || !wasTracked)
                return;

            var carried = AncestorLayoutMove();
            var delta = new Vector2(carried.X - _layoutMove.X, carried.Y - _layoutMove.Y);
            if (delta != Vector2.Zero)
                StartLayoutGlide(spec, _layoutOffset + delta);
        }

        private PointF AncestorLayoutMove()
        {
            for (var ancestor = Parent as Control; ancestor != null; ancestor = ancestor.Parent as Control)
            {
                if (ancestor._layoutTracked && ancestor._layoutMoveStamp == _layoutPassId)
                    return ancestor._layoutMove;
            }
            return default;
        }

        /// <summary>Scrolling shifted the arranged subtree: the remembered position moves along, nothing glides.</summary>
        private void ShiftLayoutPosition(PointF delta)
        {
            _layoutPosition = new PointF(_layoutPosition.X + delta.X, _layoutPosition.Y + delta.Y);
        }

        /// <summary>A panel moved this control without a layout pass (a Canvas position): glide like a layout move.</summary>
        internal void GlideFromArrangementMove(PointF delta)
        {
            if (ResolveLayoutTransition() is { } spec)
                StartLayoutGlide(spec, _layoutOffset - new Vector2(delta.X, delta.Y));
        }

        /// <summary>Draws the control at <paramref name="offset"/> from its arranged position and glides it home.</summary>
        internal void StartLayoutGlide(Transition spec, Vector2 offset)
        {
            // Squash & stretch and the deformation read the drawn motion; the jump of the offset is no motion.
            _lastMotionTranslation += offset - _layoutOffset;
            _layoutOffset = offset;
            _layoutGlide ??= new VectorTransition();
            _layoutGlide.Start(spec, offset, Vector2.Zero, ScreenSystem.TotalTime, TranslationRestDelta);
            _transitionsRunning = true;
            RequestRedraw();
        }

        /// <summary>Ends a running layout transition at the arranged position.</summary>
        internal void FinishLayoutGlide()
        {
            _layoutGlide?.Stop();
            if (_layoutOffset == Vector2.Zero)
                return;
            _lastMotionTranslation -= _layoutOffset;
            _layoutOffset = Vector2.Zero;
            InvalidateLayout(false);
        }

        private bool UpdateLayoutGlide(System.TimeSpan now)
        {
            if (_layoutGlide is not { IsRunning: true } glide)
                return false;
            var offset = glide.Evaluate(now);
            _layoutOffset = glide.IsRunning ? offset : Vector2.Zero;
            return glide.IsRunning;
        }
    }
}

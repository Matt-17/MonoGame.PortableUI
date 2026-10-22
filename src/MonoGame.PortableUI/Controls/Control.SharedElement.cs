using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    // Shared-element screen transitions: a keyed control flies from where its twin was on the screen left behind.
    public abstract partial class Control
    {
        private string? _sharedElementKey;
        private VectorTransition? _sharedScaleGlide;
        // Extra drawn scale of a flight (One at rest), about TransformOrigin like Scale.
        private Vector2 _sharedScale = Vector2.One;

        /// <summary>
        ///     Shared-element (hero) transitions: when navigation leaves a screen with a control of this key and the
        ///     next screen has one too, the new control flies from where and how large the old one was drawn to its
        ///     own place, above both screens and their transition, while the old one is hidden. Works forwards, back
        ///     and for overlays, with any <see cref="ScreenTransition"/> (also None); see
        ///     <see cref="ScreenEngine.SharedElementTransition"/>. Keys must be unique within a screen.
        /// </summary>
        public string? SharedElementKey
        {
            get => _sharedElementKey;
            set
            {
                _sharedElementKey = value;
                if (value != null)
                    ScreenEngine.SharedElementsInUse = true;
            }
        }

        /// <summary>
        ///     While shared elements fly, draw this control (and its subtree) above the flights instead of in its place
        ///     in the tree - a scrim or caption over the area a hero lands in, which a flying control would otherwise pass
        ///     over and then suddenly sit under. It keeps its screen's transform, clip and opacity. Not inside a
        ///     <see cref="CacheMode.Bitmap"/> layer, clip shape, effect island or <see cref="ProgressiveBlur"/> (drawn
        ///     in place there). Without a flight nothing changes.
        /// </summary>
        public bool DrawsAboveSharedFlights { get; set; }

        /// <summary>
        ///     The control's part in a running shared-element flight: drawn apart above every screen, or hidden as the
        ///     twin it flies away from.
        /// </summary>
        internal SharedFlightRole SharedFlight;

        /// <summary>True while the control flies (its glide or scale are still moving).</summary>
        internal bool IsSharedFlightMoving => IsLayoutTransitioning || _sharedScaleGlide is { IsRunning: true };

        /// <summary>The drawn scale of a flight (One when idle).</summary>
        internal Vector2 SharedScale => _sharedScale;

        /// <summary>Starts drawn at <paramref name="scale"/> and <paramref name="offset"/> and moves home with <paramref name="spec"/>.</summary>
        internal void StartSharedFlight(Transition spec, Vector2 scale, Vector2 offset)
        {
            _sharedScale = scale;
            _sharedScaleGlide ??= new VectorTransition();
            _sharedScaleGlide.Start(spec, scale, Vector2.One, ScreenSystem.TotalTime, ScaleRestDelta);
            SharedFlight = SharedFlightRole.Flying;
            StartLayoutGlide(spec, offset);
        }

        /// <summary>A hitch of <paramref name="delay"/> does not count as flight time.</summary>
        internal void PostponeSharedFlight(System.TimeSpan delay)
        {
            _sharedScaleGlide?.Postpone(delay);
            _layoutGlide?.Postpone(delay);
        }

        internal void SetSharedFlightRole(SharedFlightRole role)
        {
            if (SharedFlight == role)
                return;
            SharedFlight = role;
            // Cached layers around it baked it in (or left it out).
            RequestRedraw();
        }

        /// <summary>Lands a running flight at once.</summary>
        internal void FinishSharedFlight()
        {
            FinishLayoutGlide();
            FinishSharedScale();
        }

        private void FinishSharedScale()
        {
            _sharedScaleGlide?.Stop();
            if (_sharedScale == Vector2.One)
                return;
            _sharedScale = Vector2.One;
            InvalidateLayout(false);
        }

        private bool UpdateSharedScale(System.TimeSpan now)
        {
            if (_sharedScaleGlide is not { IsRunning: true } glide)
                return false;
            var scale = glide.Evaluate(now);
            _sharedScale = glide.IsRunning ? scale : Vector2.One;
            return glide.IsRunning;
        }
    }

    internal enum SharedFlightRole : byte
    {
        None,
        /// <summary>The twin left behind: not drawn while the flight runs.</summary>
        Hidden,
        /// <summary>The flying control: drawn above every screen instead of in its tree.</summary>
        Flying
    }
}

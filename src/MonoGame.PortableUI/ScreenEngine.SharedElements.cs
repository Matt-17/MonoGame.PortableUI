using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    // Shared-element (hero) transitions between screens (Control.SharedElementKey).
    public partial class ScreenEngine
    {
        /// <summary>Set once any control gets a <see cref="Control.SharedElementKey"/>: until then navigation walks no trees for it.</summary>
        internal static bool SharedElementsInUse;

        /// <summary>
        ///     How shared elements (<see cref="Control.SharedElementKey"/>) fly from the screen left behind to their new
        ///     place: a spring (default <see cref="SpringSpec.Snappy"/>) or a duration and easing.
        /// </summary>
        public Transition SharedElementTransition { get; set; } = new Transition(SpringSpec.Snappy);

        // Key -> the control on the screen being left and where it was drawn (layout units, on screen).
        private readonly Dictionary<string, (Control Source, Rect Drawn)> _sharedSources = new Dictionary<string, (Control, Rect)>(StringComparer.Ordinal);
        // The screen whose first layout starts the flights.
        private Screen? _sharedTarget;
        private readonly List<(Control Target, Control Source)> _sharedFlights = new List<(Control, Control)>();

        /// <summary>A flight is drawn above every screen.</summary>
        internal bool HasSharedFlights => _sharedFlights.Count > 0;

        /// <summary>The screen drawn last this frame; it draws the flights on top (before its post effects).</summary>
        internal Screen? SharedFlightHost { get; private set; }

        internal IReadOnlyList<(Control Target, Control Source)> SharedFlights => _sharedFlights;

        /// <summary>Before navigating from <paramref name="from"/> to <paramref name="to"/>: remember where keyed controls are drawn.</summary>
        private void CaptureSharedElements(Screen? from, Screen? to)
        {
            FinishSharedFlights();
            _sharedSources.Clear();
            _sharedTarget = null;
            if (!SharedElementsInUse || from == null || to == null || ReferenceEquals(from, to))
                return;

            CollectSharedSources(from.RootGrid, Matrix.Identity);
            if (_sharedSources.Count > 0)
                _sharedTarget = to;
        }

        private void CollectSharedSources(Control control, Matrix parentTransform)
        {
            if (!control.IsVisibleShown || control.IsGoneShown)
                return;
            var transform = control.GetRenderTransform() * parentTransform;
            if (control.SharedElementKey is { } key && control.ClippingRect.Width > 0 && control.ClippingRect.Height > 0)
                _sharedSources.TryAdd(key, (control, Screen.TransformRectForSharedElement(control.ClippingRect, transform)));
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                CollectSharedSources(control.GetVisualChild(i), transform);
        }

        /// <summary>After <paramref name="screen"/> was laid out: start the flights of the keyed controls it shares with the screen left behind.</summary>
        internal void ResolveSharedElements(Screen screen)
        {
            if (!ReferenceEquals(_sharedTarget, screen))
                return;
            _sharedTarget = null;
            StartSharedFlights(screen.RootGrid);
            _sharedSources.Clear();
        }

        private void StartSharedFlights(Control control)
        {
            if (!control.IsVisibleShown || control.IsGoneShown || _sharedSources.Count == 0)
                return;
            if (control.SharedElementKey is { } key && _sharedSources.Remove(key, out var source))
                StartSharedFlight(control, source.Source, source.Drawn);
            var count = control.VisualChildCount;
            for (var i = 0; i < count; i++)
                StartSharedFlights(control.GetVisualChild(i));
        }

        private void StartSharedFlight(Control target, Control source, Rect from)
        {
            var box = target.ClippingRect;
            if (box.Width <= 0 || box.Height <= 0 || from.Width <= 0 || from.Height <= 0)
                return;

            // The flight is drawn without the ancestors' transforms (above every screen), so only the control's own
            // transform places it. With scale s about the drawn origin o, the drawn top-left N lands on the old one O
            // when the extra offset is (O - o) - s (N - o).
            var own = target.GetRenderTransform();
            var rest = Screen.TransformRectForSharedElement(box, own);
            var origin = Vector2.Transform(new Vector2(box.Left + box.Width * target.TransformOrigin.X, box.Top + box.Height * target.TransformOrigin.Y), own);
            var scale = new Vector2(from.Width / rest.Width, from.Height / rest.Height);
            var offset = new Vector2(from.Left, from.Top) - origin - scale * (new Vector2(rest.Left, rest.Top) - origin);
            target.StartSharedFlight(SharedElementTransition, scale, offset);
            target.SetSharedFlightRole(SharedFlightRole.Flying);
            source.SetSharedFlightRole(SharedFlightRole.Hidden);
            _sharedFlights.Add((target, source));
        }

        private void PostponeSharedFlights(TimeSpan hitch)
        {
            foreach (var (target, _) in _sharedFlights)
                target.PostponeSharedFlight(hitch);
        }

        private readonly Dictionary<Control, FlightSnapshot> _flightSnapshots = new();

        internal FlightSnapshot? GetFlightSnapshot(Control control)
            => _flightSnapshots.TryGetValue(control, out var snapshot) && !snapshot.Target.IsDisposed ? snapshot : null;

        internal void StoreFlightSnapshot(Control control, FlightSnapshot snapshot) => _flightSnapshots[control] = snapshot;

        private void ReleaseFlightSnapshot(Control control)
        {
            if (!_flightSnapshots.Remove(control, out var snapshot) || snapshot.Target.IsDisposed)
                return;
            Effects.RenderTargetPool.Return(snapshot.Target.GraphicsDevice, snapshot.Target);
        }

        /// <summary>Ends flights whose control has arrived: both twins are drawn normally again.</summary>
        private void UpdateSharedFlights()
        {
            for (var i = _sharedFlights.Count - 1; i >= 0; i--)
            {
                var (target, source) = _sharedFlights[i];
                if (target.IsSharedFlightMoving)
                    continue;
                target.SetSharedFlightRole(SharedFlightRole.None);
                source.SetSharedFlightRole(SharedFlightRole.None);
                ReleaseFlightSnapshot(target);
                ReleaseFlightSnapshot(source);
                _sharedFlights.RemoveAt(i);
            }
        }

        /// <summary>Lands every flight at once (before the next navigation).</summary>
        private void FinishSharedFlights()
        {
            foreach (var (target, source) in _sharedFlights)
            {
                target.FinishSharedFlight();
                target.SetSharedFlightRole(SharedFlightRole.None);
                source.SetSharedFlightRole(SharedFlightRole.None);
                ReleaseFlightSnapshot(target);
                ReleaseFlightSnapshot(source);
            }
            _sharedFlights.Clear();
        }

        private void ChooseSharedFlightHost(Screen? drawnLast)
        {
            SharedFlightHost = _sharedFlights.Count == 0 ? null : drawnLast;
        }
    }
}

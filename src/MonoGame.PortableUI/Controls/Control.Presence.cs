using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    // Enter/exit transitions: IsVisible/IsGone animate through VisibilityTransition's hidden pose.
    public abstract partial class Control
    {
        private const float PresenceRestDelta = 0.001f;

        private VisibilityTransition? _visibilityTransition;
        private VectorTransition? _presence;
        // 0 = hidden pose, 1 = shown, per axis (X also drives opacity); One while idle.
        private Vector2 _presenceProgress = Vector2.One;
        private bool _presenceExiting;

        /// <summary>
        ///     How the control enters and exits when <see cref="UIElement.IsVisible"/> or <see cref="UIElement.IsGone"/>
        ///     changes (null = at once). Hiding plays the exit before the control leaves the screen and, for IsGone, the
        ///     layout; the properties return the new value at once and the control takes no input while it exits.
        ///     Needs the control to be on a screen; elsewhere it shows and hides at once.
        /// </summary>
        public VisibilityTransition? VisibilityTransition
        {
            get => _visibilityTransition;
            set
            {
                if (ReferenceEquals(_visibilityTransition, value))
                    return;
                _visibilityTransition = value;
                if (value == null)
                    FinishPresence();
            }
        }

        /// <summary>True while an enter or exit transition plays.</summary>
        public bool IsPresenceAnimating => _presence is { IsRunning: true };

        internal override void OnShownChanged(bool shown)
        {
            var spec = _visibilityTransition;
            if (spec == null || Screen?.ScreenEngine == null)
            {
                FinishPresence();
                return;
            }

            var from = IsPresenceAnimating ? _presenceProgress : shown ? Vector2.Zero : Vector2.One;
            _presence ??= new VectorTransition();
            _presence.Start(shown ? spec.Enter : spec.Exit, from, shown ? Vector2.One : Vector2.Zero, ScreenSystem.TotalTime, PresenceRestDelta);
            _presenceProgress = from;
            _presenceExiting = !shown;
            ExitHolds = !shown;
            RequestRedraw();
        }

        private void UpdatePresence()
        {
            if (_presence is not { IsRunning: true } presence)
                return;

            var progress = presence.Evaluate(ScreenSystem.TotalTime);
            // A spring exit ends when it reaches the hidden pose; its swing past it would only delay the removal.
            if (_presenceExiting && (!presence.IsRunning || progress.X <= 0 && progress.Y <= 0))
            {
                FinishPresence();
                return;
            }

            _presenceProgress = presence.IsRunning ? progress : Vector2.One;
            RequestRedraw();
        }

        /// <summary>Ends a running enter or exit at once: an exit removes the control now.</summary>
        private void FinishPresence()
        {
            _presence?.Stop();
            _presenceExiting = false;
            if (_presenceProgress != Vector2.One)
            {
                _presenceProgress = Vector2.One;
                RequestRedraw();
            }

            ExitHolds = false;
        }

        private bool HasPresencePose => _presenceProgress != Vector2.One && _visibilityTransition != null;

        /// <summary>The enter/exit pose's scale factor (One when idle).</summary>
        internal Vector2 PresenceScale => HasPresencePose
            ? Vector2.One + (_visibilityTransition!.Scale - Vector2.One) * (Vector2.One - _presenceProgress)
            : Vector2.One;

        /// <summary>The enter/exit pose's offset in layout units (zero when idle).</summary>
        internal Vector2 PresenceOffset => HasPresencePose
            ? _visibilityTransition!.Offset * (Vector2.One - _presenceProgress)
            : Vector2.Zero;

        /// <summary>The enter/exit pose's opacity factor (1 when idle).</summary>
        internal float PresenceOpacity => HasPresencePose
            ? MathHelper.Lerp(_visibilityTransition!.Opacity, 1, MathHelper.Clamp(_presenceProgress.X, 0, 1))
            : 1;

        /// <summary>The scale drawn: the shown <see cref="Scale"/> times the enter/exit pose.</summary>
        internal Vector2 DrawnScale => _scale * PresenceScale;
    }
}

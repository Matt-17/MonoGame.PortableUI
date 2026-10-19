using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Controls
{
    // Squash & stretch: a volume-preserving deformation along the direction of motion, on top of Scale.
    public abstract partial class Control
    {
        // Speed (layout units per second) that stretches by 25 % at SquashStretch = 1.
        private const float StretchReferenceSpeed = 1000;
        private const float MaxStretch = 1.6f;
        private const float MinStretch = 0.4f;

        private float _squashStretch;
        private SpringSpec _squashStretchSpring = SpringSpec.Bouncy;
        // Factor along _stretchAxis (1/factor across it); 1 = undeformed.
        private float _stretch = 1;
        private float _stretchVelocity;
        private Vector2 _stretchAxis = Vector2.UnitY;
        private bool _stretchAtRest = true;
        private bool _motionTracked;
        private Vector2 _lastMotionTranslation;
        private TimeSpan _lastMotionTime;
        private float _breathe;
        private TimeSpan _breathePeriod = TimeSpan.FromSeconds(3.2);
        // Current breathing scale per axis; One while still or frozen.
        private Vector2 _breath = Vector2.One;

        /// <summary>
        ///     Idle "breathing": a slow, irregular pulse of the size by this fraction (0.03 = 3 %; 0 = off),
        ///     the two axes slightly out of phase, each control in its own rhythm. A decoration: it runs only
        ///     at <see cref="RenderQuality.High"/> and rests below. Requests a frame every update while it runs.
        /// </summary>
        public float Breathe
        {
            get => _breathe;
            set
            {
                _breathe = Math.Max(0, value);
                if (_breathe == 0 && _breath != Vector2.One)
                {
                    _breath = Vector2.One;
                    InvalidateLayout(false);
                }
            }
        }

        /// <summary>Length of one breath (default 3.2 s).</summary>
        public TimeSpan BreathePeriod
        {
            get => _breathePeriod;
            set => _breathePeriod = value > TimeSpan.Zero ? value : TimeSpan.FromSeconds(3.2);
        }

        /// <summary>
        ///     Squash &amp; stretch strength: while <see cref="Translation"/> moves, the control stretches along
        ///     the direction of travel and narrows across it (its area stays the same); when it stops, the
        ///     <see cref="SquashStretchSpring"/> swings it into a squash and back. 0 (default) = off,
        ///     1 = stretch by 25 % at 1000 units per second. Pivot: <see cref="TransformOrigin"/>.
        /// </summary>
        public float SquashStretch
        {
            get => _squashStretch;
            set => _squashStretch = Math.Max(0, value);
        }

        /// <summary>The spring the deformation follows (default <see cref="SpringSpec.Bouncy"/>).</summary>
        public SpringSpec SquashStretchSpring
        {
            get => _squashStretchSpring;
            set
            {
                if (!(value.Stiffness > 0))
                    throw new ArgumentException("Use a SpringSpec preset or constructor; default(SpringSpec) has no stiffness.", nameof(value));
                _squashStretchSpring = value;
            }
        }

        /// <summary>The current deformation factor along the axis of motion (1 = undeformed).</summary>
        public float CurrentStretch => _stretch;

        /// <summary>
        ///     An impact (landing, press, bump): kicks the <see cref="SquashStretchSpring"/> so the control
        ///     squashes by about <paramref name="amount"/> along <paramref name="axis"/> (default vertical)
        ///     and widens across it within a fraction of a second, then swings back. &gt; 0 compresses,
        ///     &lt; 0 stretches. The shape stays continuous: a control still stretched from a fall flows
        ///     from the stretch into the squash instead of jumping. Works without <see cref="SquashStretch"/>.
        /// </summary>
        public void Squash(float amount, Vector2? axis = null)
        {
            var direction = axis ?? Vector2.UnitY;
            if (direction.LengthSquared() < 1e-8f)
                direction = Vector2.UnitY;
            direction.Normalize();

            if (_stretchAtRest)
            {
                _stretch = 1;
                _stretchVelocity = 0;
            }
            else if (Math.Abs(Vector2.Dot(direction, _stretchAxis)) < 0.7071f)
            {
                // The same deformation seen from the other axis: k along one is 1/k along the other.
                _stretchVelocity = -_stretchVelocity / (_stretch * _stretch);
                _stretch = 1 / _stretch;
            }

            _stretchAxis = direction;
            // An impulse, not a jump: the velocity kick that makes the spring, from the shape and
            // velocity it has now, swing out to 1 - amount.
            _stretchVelocity = SolveKick(_squashStretchSpring, _stretch - 1, _stretchVelocity, -amount);
            _stretchAtRest = false;
            RequestRedraw();
        }

        // The start velocity for which a spring at displacement d0 first swings to `peak` (bisection on
        // the first extreme of its motion; the extreme grows monotonically with the velocity).
        private static float SolveKick(in SpringSpec spring, float d0, float v0, float peak)
        {
            if (Math.Abs(peak) < 1e-5f)
                return v0;
            var sign = Math.Sign(peak);
            double low = 0, high = 1;
            while (FirstExtreme(spring, d0, sign * high) * sign < Math.Abs(peak) && high < 1e4)
                high *= 2;
            for (var i = 0; i < 30; i++)
            {
                var mid = (low + high) / 2;
                if (FirstExtreme(spring, d0, sign * mid) * sign < Math.Abs(peak))
                    low = mid;
                else
                    high = mid;
            }

            return (float)(sign * high);
        }

        // Displacement where the motion first turns around (or the start, if it heads the other way).
        private static double FirstExtreme(in SpringSpec spring, double d0, double v0)
        {
            var extreme = d0;
            for (var t = 1 / 240.0; t < 2; t += 1 / 240.0)
            {
                spring.Evaluate(d0, v0, t, out var displacement, out var velocity);
                if (v0 >= 0 ? displacement > extreme : displacement < extreme)
                    extreme = displacement;
                if (Math.Sign(velocity) != Math.Sign(v0))
                    break;
            }

            return extreme;
        }

        /// <summary>True while squash &amp; stretch deforms the control (a cached layer is drawn live then).</summary>
        internal bool IsMotionDeformed => !_stretchAtRest || _breath != Vector2.One || _deformation is { IsActive: true };

        /// <summary>Axis-aligned part of the deformation, folded into the render scale (line widths, and
        /// cached layers below are drawn live while it is not 1).</summary>
        internal Vector2 StretchDiagonal
        {
            get
            {
                if (_stretchAtRest)
                    return _breath;
                var inverse = 1 / _stretch;
                var c = _stretchAxis.X;
                var s = _stretchAxis.Y;
                return new Vector2(_stretch * c * c + inverse * s * s, _stretch * s * s + inverse * c * c) * _breath;
            }
        }

        /// <summary>The squash &amp; stretch and breathing matrix about the origin (identity at rest).</summary>
        private Matrix GetStretchMatrix()
        {
            if (_stretchAtRest)
                return _breath == Vector2.One ? Matrix.Identity : Matrix.CreateScale(_breath.X, _breath.Y, 1);

            // Scale by k along the axis and 1/k across it: R(θ) · diag(k, 1/k) · R(-θ), symmetric.
            var k = _stretch;
            var inverse = 1 / k;
            var c = _stretchAxis.X;
            var s = _stretchAxis.Y;
            var m11 = k * c * c + inverse * s * s;
            var m22 = k * s * s + inverse * c * c;
            var m12 = (k - inverse) * c * s;
            return new Matrix(
                m11 * _breath.X, m12 * _breath.Y, 0, 0,
                m12 * _breath.X, m22 * _breath.Y, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

        private void UpdateBreath()
        {
            if (_breathe == 0)
                return;

            var engine = ScreenEngine.For(this);
            if (engine != null && engine.EffectiveRenderQuality != RenderQuality.High)
            {
                if (_breath != Vector2.One)
                {
                    _breath = Vector2.One;
                    RequestRedraw();
                }
                return;
            }

            // Two incommensurate waves read as irregular; the phase differs per control.
            var phase = (RuntimeHelpers.GetHashCode(this) & 1023) / 1024.0;
            var t = ScreenSystem.TotalTime.TotalSeconds / _breathePeriod.TotalSeconds + phase;
            _breath = new Vector2(1 + _breathe * BreathWave(t), 1 + _breathe * BreathWave(t - 0.12));
            RequestRedraw();
        }

        private static float BreathWave(double t)
            => (float)(0.7 * Math.Sin(MathHelper.TwoPi * t) + 0.3 * Math.Sin(MathHelper.TwoPi * 1.618 * t + 1.3));

        private Deformation? _deformation;

        /// <summary>
        ///     Soft-body deformation: the control is drawn as a texture on a grid whose points lag, swell, wobble
        ///     and bend (<see cref="Animation.Deformation"/>). Null (default) = a plain rectangle.
        /// </summary>
        public Deformation? Deformation
        {
            get => _deformation;
            set
            {
                if (ReferenceEquals(_deformation, value))
                    return;
                _deformation = value;
                RequestRedraw();
            }
        }

        /// <summary>The control's own rest pose is being rendered into a deformation texture: no own transform.</summary>
        internal bool RenderTransformSuppressed;

        private void UpdateMotion()
        {
            UpdateBreath();
            if (_squashStretch == 0 && _stretchAtRest && _deformation == null)
            {
                _motionTracked = false;
                return;
            }

            var now = ScreenSystem.TotalTime;
            if (!_motionTracked)
            {
                _motionTracked = true;
                _lastMotionTranslation = _translation + _layoutOffset;
                _lastMotionTime = now;
                return;
            }

            var seconds = (now - _lastMotionTime).TotalSeconds;
            if (seconds <= 0)
                return;
            // After a pause (hidden screen, breakpoint) the step would be one huge jump.
            seconds = Math.Min(seconds, 0.1);
            // A layout transition's glide is motion too (a Canvas child moved with a transition squashes).
            var drawn = _translation + _layoutOffset;
            var delta = drawn - _lastMotionTranslation;
            _lastMotionTranslation = drawn;
            _lastMotionTime = now;

            if (_deformation != null)
            {
                var engine = ScreenEngine.For(this);
                if (engine != null && engine.EffectiveRenderQuality == RenderQuality.Low)
                {
                    if (_deformation.IsActive)
                    {
                        _deformation.Reset();
                        RequestRedraw();
                    }
                }
                else
                {
                    var wasActive = _deformation.IsActive;
                    _deformation.Update(new Vector2(ClippingRect.Width, ClippingRect.Height), delta / (float)seconds, seconds);
                    if (_deformation.IsAnimating || wasActive != _deformation.IsActive)
                        RequestRedraw();
                }
            }

            if (_squashStretch == 0 && _stretchAtRest)
                return;

            var target = 1f;
            if (_squashStretch > 0)
            {
                var distance = delta.Length();
                var speed = distance / (float)seconds;
                if (speed > 1)
                {
                    _stretchAxis = delta / distance;
                    target = Math.Min(1 + _squashStretch * 0.25f * speed / StretchReferenceSpeed, MaxStretch);
                }
            }

            if (_stretchAtRest && target == 1)
                return;

            _squashStretchSpring.Evaluate(_stretch - target, _stretchVelocity, seconds, out var displacement, out var velocity);
            _stretch = MathHelper.Clamp(target + (float)displacement, MinStretch, MaxStretch);
            _stretchVelocity = (float)velocity;
            _stretchAtRest = target == 1 && Math.Abs(_stretch - 1) < 0.0005f && Math.Abs(_stretchVelocity) < 0.005f;
            if (_stretchAtRest)
            {
                _stretch = 1;
                _stretchVelocity = 0;
            }

            RequestRedraw();
        }
    }
}

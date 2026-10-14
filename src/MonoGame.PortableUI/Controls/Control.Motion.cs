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
        ///     Squash & stretch strength: while <see cref="Translation"/> moves, the control stretches along
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
        ///     Deforms the control at once and lets <see cref="SquashStretchSpring"/> swing it back - an
        ///     impact (landing, press, bump). <paramref name="amount"/> &gt; 0 compresses along
        ///     <paramref name="axis"/> (default vertical) and widens across it, &lt; 0 stretches. Works without
        ///     <see cref="SquashStretch"/>.
        /// </summary>
        public void Squash(float amount, Vector2? axis = null)
        {
            var direction = axis ?? Vector2.UnitY;
            if (direction.LengthSquared() < 1e-8f)
                direction = Vector2.UnitY;
            direction.Normalize();
            _stretchAxis = direction;
            _stretch = MathHelper.Clamp(1 - amount, MinStretch, MaxStretch);
            _stretchVelocity = 0;
            _stretchAtRest = false;
            RequestRedraw();
        }

        /// <summary>True while squash & stretch deforms the control (a cached layer is drawn live then).</summary>
        internal bool IsMotionDeformed => !_stretchAtRest || _breath != Vector2.One;

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

        /// <summary>The squash & stretch and breathing matrix about the origin (identity at rest).</summary>
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

        private void UpdateMotion()
        {
            UpdateBreath();
            if (_squashStretch == 0 && _stretchAtRest)
            {
                _motionTracked = false;
                return;
            }

            var now = ScreenSystem.TotalTime;
            if (!_motionTracked)
            {
                _motionTracked = true;
                _lastMotionTranslation = _translation;
                _lastMotionTime = now;
                return;
            }

            var seconds = (now - _lastMotionTime).TotalSeconds;
            if (seconds <= 0)
                return;
            // After a pause (hidden screen, breakpoint) the step would be one huge jump.
            seconds = Math.Min(seconds, 0.1);
            var delta = _translation - _lastMotionTranslation;
            _lastMotionTranslation = _translation;
            _lastMotionTime = now;

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

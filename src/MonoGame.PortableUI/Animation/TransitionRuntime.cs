using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Animation
{
    /// <summary>Progress of a running <see cref="Transition"/>; shared by the vector and colour runners.</summary>
    internal static class TransitionTiming
    {
        // A spring that never comes to rest (no damping) still ends after this long.
        private const double MaxSpringSeconds = 30;

        /// <summary>Progress 0..1 from the old to the new value (an eased curve, or a spring's
        /// approach clamped at the target) <paramref name="seconds"/> after the transition began.</summary>
        public static float Progress(Transition spec, double seconds, out bool done)
        {
            if (seconds <= 0)
            {
                done = false;
                return 0;
            }

            if (spec.IsSpring)
            {
                spec.SpringX.Evaluate(1, 0, seconds, out var remaining, out var velocity);
                done = seconds >= MaxSpringSeconds || Math.Abs(remaining) < 0.001 && Math.Abs(velocity) < 0.01;
                return done ? 1 : (float)Math.Max(0, Math.Min(1, 1 - remaining));
            }

            var duration = spec.Duration.TotalSeconds;
            if (duration <= 0 || seconds >= duration)
            {
                done = true;
                return 1;
            }

            done = false;
            return (float)Math.Max(0, Math.Min(1, spec.Easing(seconds / duration)));
        }

        public static bool SpringDone(double seconds) => seconds >= MaxSpringSeconds;

        public static Color Unpremultiply(Color color)
        {
            if (color.A == 255)
                return color;
            if (color.A == 0)
                return Color.Transparent;
            var scale = 255f / color.A;
            return new Color(
                (int)Math.Min(255, color.R * scale + 0.5f),
                (int)Math.Min(255, color.G * scale + 0.5f),
                (int)Math.Min(255, color.B * scale + 0.5f),
                (int)color.A);
        }
    }

    /// <summary>
    ///     A running implicit transition of a vector property (Scale, Translation; Opacity in X), updated
    ///     once per frame. A new target starts from the value shown and, for springs, its velocity.
    /// </summary>
    internal sealed class VectorTransition
    {
        private Transition _spec = null!;
        private Vector2 _start;
        private Vector2 _target;
        private Vector2 _initialVelocity;
        private Vector2 _velocity;
        private TimeSpan _startedAt;
        private float _restDelta;

        public bool IsRunning { get; private set; }
        public Vector2 Target => _target;

        public void Start(Transition spec, Vector2 from, Vector2 to, TimeSpan now, float restDelta)
        {
            // Retargeted mid-flight: keep moving the way it was moving.
            _initialVelocity = IsRunning ? _velocity : Vector2.Zero;
            _velocity = _initialVelocity;
            _spec = spec;
            _start = from;
            _target = to;
            _startedAt = now + spec.Delay;
            _restDelta = restDelta;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            _velocity = Vector2.Zero;
        }

        /// <summary>Moves the start later by <paramref name="delay"/> (a hitch that should not count as animation time).</summary>
        public void Postpone(TimeSpan delay) => _startedAt += delay;

        /// <summary>The value at <paramref name="now"/>; stops (returning the target) once it arrived.</summary>
        public Vector2 Evaluate(TimeSpan now)
        {
            var seconds = (now - _startedAt).TotalSeconds;
            if (seconds <= 0)
                return _start;

            if (_spec.IsSpring)
            {
                _spec.SpringX.Evaluate(_start.X - _target.X, _initialVelocity.X, seconds, out var dx, out var vx);
                _spec.SpringY.Evaluate(_start.Y - _target.Y, _initialVelocity.Y, seconds, out var dy, out var vy);
                _velocity = new Vector2((float)vx, (float)vy);
                if (AtRest(dx, vx) && AtRest(dy, vy) || TransitionTiming.SpringDone(seconds))
                {
                    Stop();
                    return _target;
                }

                return new Vector2(_target.X + (float)dx, _target.Y + (float)dy);
            }

            var duration = _spec.Duration.TotalSeconds;
            if (duration <= 0 || seconds >= duration)
            {
                Stop();
                return _target;
            }

            var progress = seconds / duration;
            var eased = _spec.Easing(progress);
            const double h = 0.001;
            var slope = (_spec.Easing(Math.Min(1, progress + h)) - _spec.Easing(Math.Max(0, progress - h))) / (2 * h);
            _velocity = (_target - _start) * (float)(slope / duration);
            return Vector2.Lerp(_start, _target, (float)eased);
        }

        private bool AtRest(double displacement, double velocity)
            => Math.Abs(displacement) < _restDelta && Math.Abs(velocity) < _restDelta * 10;
    }

    /// <summary>
    ///     A colour that fades to a new value when the value read at draw time changes (text colour per
    ///     visual state). The first value, a theme switch and <see cref="Snap"/> show at once.
    /// </summary>
    internal sealed class ColorFade
    {
        private Color _from;
        private Color _to;
        private Transition? _spec;
        private TimeSpan _startedAt;
        private long _themeVersion;
        private bool _hasValue;
        private bool _snap;

        public bool IsRunning { get; private set; }

        public void Snap() => _snap = true;

        /// <summary>The colour to draw for <paramref name="target"/>; asks for the next frame while fading.</summary>
        public Color Resolve(Color target, Transition? spec, TimeSpan now, long themeVersion)
        {
            if (!_hasValue || _snap || spec == null || themeVersion != _themeVersion)
            {
                _hasValue = true;
                _snap = false;
                _themeVersion = themeVersion;
                _to = target;
                IsRunning = false;
                return target;
            }

            if (target != _to)
            {
                _from = IsRunning ? Current(now) : _to;
                _to = target;
                _spec = spec;
                _startedAt = now + spec.Delay;
                IsRunning = true;
            }

            if (!IsRunning)
                return _to;

            var color = Current(now);
            if (IsRunning)
                ScreenEngine.RequestAnimationFrame();
            return color;
        }

        private Color Current(TimeSpan now)
        {
            var progress = TransitionTiming.Progress(_spec!, (now - _startedAt).TotalSeconds, out var done);
            if (done)
            {
                IsRunning = false;
                return _to;
            }

            // In premultiplied space, so a fade from transparent does not pass through black.
            return TransitionTiming.Unpremultiply(Color.Lerp(Brush.Premultiply(_from), Brush.Premultiply(_to), progress));
        }
    }

    /// <summary>
    ///     A brush that fades to a new brush when the brush read at draw time changes (a theme's hover and
    ///     pressed backgrounds). Solid colours (and null, as transparent) blend; other brushes cross-fade.
    ///     The first brush, a theme switch and <see cref="Snap"/> show at once.
    /// </summary>
    internal sealed class BrushFade
    {
        private Brush? _from;
        private Color _fromColor;
        private bool _fromIsColor;
        private Brush? _to;
        private Transition? _spec;
        private TimeSpan _startedAt;
        private long _themeVersion;
        private bool _hasValue;
        private bool _snap;
        private float _progress;

        public bool IsRunning { get; private set; }

        public void Snap() => _snap = true;

        /// <summary>Takes the brush to draw now; true while a fade is in progress (draw with
        /// <see cref="Draw"/> or <see cref="TryGetColor"/> then).</summary>
        public bool Update(Brush? target, Transition? spec, TimeSpan now, long themeVersion)
        {
            if (!_hasValue || _snap || spec == null || themeVersion != _themeVersion)
            {
                _hasValue = true;
                _snap = false;
                _themeVersion = themeVersion;
                _to = target;
                IsRunning = false;
                return false;
            }

            if (!ReferenceEquals(target, _to))
            {
                if (target is SolidColorBrush a && _to is SolidColorBrush b && a.Color == b.Color)
                {
                    _to = target; // same look, new instance
                }
                else
                {
                    if (!IsRunning)
                    {
                        _from = _to;
                        _fromIsColor = false;
                    }
                    else if (TryBlend(out var shown))
                    {
                        _fromColor = shown;
                        _fromIsColor = true;
                        _from = null;
                    }
                    else if (_progress >= 0.5f)
                    {
                        _from = _to;
                        _fromIsColor = false;
                    }

                    _to = target;
                    _spec = spec;
                    _startedAt = now + spec.Delay;
                    _progress = 0;
                    IsRunning = true;
                }
            }

            if (!IsRunning)
                return false;

            _progress = TransitionTiming.Progress(_spec!, (now - _startedAt).TotalSeconds, out var done);
            if (done)
            {
                IsRunning = false;
                return false;
            }

            ScreenEngine.RequestAnimationFrame();
            return true;
        }

        /// <summary>The blended straight-alpha colour when both ends are solid colours (or none).</summary>
        public bool TryGetColor(out Color color)
        {
            if (TryBlend(out var premultiplied))
            {
                color = TransitionTiming.Unpremultiply(premultiplied);
                return true;
            }

            color = default;
            return false;
        }

        // Premultiplied blend of the two ends at the current progress.
        private bool TryBlend(out Color premultiplied)
        {
            if (!TryGetSolid(_fromIsColor, _fromColor, _from, out var from) || !TryGetSolid(false, default, _to, out var to))
            {
                premultiplied = default;
                return false;
            }

            premultiplied = Color.Lerp(Brush.Premultiply(from), Brush.Premultiply(to), _progress);
            return true;
        }

        private static bool TryGetSolid(bool isColor, Color color, Brush? brush, out Color solid)
        {
            if (isColor)
            {
                solid = color;
                return true;
            }

            switch (brush)
            {
                case null:
                    solid = Color.Transparent;
                    return true;
                case SolidColorBrush s:
                    solid = s.Color;
                    return true;
                default:
                    solid = default;
                    return false;
            }
        }

        /// <summary>Draws the fade into <paramref name="context"/>'s rect.</summary>
        public void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            if (TryBlend(out var blend))
            {
                RoundedRectRenderer.DrawSolid(spriteBatch, context.Rect, context.Radius, blend * context.Opacity);
                return;
            }

            // Cross-fade: the old look fades out a little later than the new one fades in, so an
            // opaque face does not let the background shine through halfway.
            var fromOpacity = 1 - _progress * _progress;
            DrawEnd(spriteBatch, context, _fromIsColor, _fromColor, _from, context.Opacity * fromOpacity);
            DrawEnd(spriteBatch, context, false, default, _to, context.Opacity * _progress);
        }

        /// <summary>The two ends and their opacities, for drawing a fade that cannot blend colours.</summary>
        public void GetEnds(out Brush? from, out Color fromColor, out bool fromIsColor, out float fromOpacity, out Brush? to, out float toOpacity)
        {
            from = _from;
            fromColor = _fromColor;
            fromIsColor = _fromIsColor;
            fromOpacity = 1 - _progress * _progress;
            to = _to;
            toOpacity = _progress;
        }

        private static void DrawEnd(SpriteBatch spriteBatch, in BrushContext context, bool isColor, Color color, Brush? brush, float opacity)
        {
            if (opacity <= 0)
                return;
            if (isColor)
            {
                RoundedRectRenderer.DrawSolid(spriteBatch, context.Rect, context.Radius, Brush.ApplyOpacity(color, opacity));
                return;
            }

            if (brush == null)
                return;
            var endContext = new BrushContext(context.Rect, context.Radius, opacity, context.Device, context.TimeSeconds, context.PointerPosition, context.Scale);
            brush.Draw(spriteBatch, in endContext);
        }
    }
}

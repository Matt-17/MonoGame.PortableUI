using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Animation;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    // Implicit transitions (CSS "transition"): setting a property with a transition animates it.
    public abstract partial class Control
    {
        private const float ScaleRestDelta = 0.0005f;
        private const float TranslationRestDelta = 0.01f;

        private TransitionCollection? _transitions;
        private VectorTransition? _scaleTransition;
        private VectorTransition? _translationTransition;
        private VectorTransition? _opacityTransition;
        private VectorTransition? _rotationTransition;
        private VectorTransition? _skewTransition;
        private bool _transitionsRunning;
        // Opacity is a double; the runner moves a float, so the exact target is kept here.
        private double _opacityTarget;
        private BrushFade? _backgroundFade;
        private BrushFade? _borderFade;
        private Transition? _stateTransition;

        /// <summary>
        ///     Implicit transitions, like CSS <c>transition</c>: once a property has one, setting it animates
        ///     from what is shown to the new value, and a new value mid-flight retargets smoothly (springs keep
        ///     their velocity). The property getters return the new value at once; drawing shows the motion.
        ///     <code>card.Transitions.Add(TransitionProperty.Scale, SpringSpec.Snappy);
        /// card.Scale = new Vector2(1.05f); // springs there</code>
        ///     Without an entry here, background, border and text colours still fade between visual states
        ///     (hover, pressed) over the theme style's <see cref="ControlStyle.TransitionDuration"/>.
        /// </summary>
        public TransitionCollection Transitions => _transitions ??= new TransitionCollection(OnTransitionsChanged);

        /// <summary>True while an implicit transition of Scale, Translation, Opacity, Rotation or Skew is moving.</summary>
        public bool IsTransitioning =>
            _scaleTransition is { IsRunning: true } || _translationTransition is { IsRunning: true } || _opacityTransition is { IsRunning: true }
            || _rotationTransition is { IsRunning: true } || _skewTransition is { IsRunning: true };

        /// <summary>
        ///     Ends every running transition at its target (an exit removes the control now), here and in the
        ///     subtree, and shows the next colour change without a fade - e.g. after rebinding a recycled list row to another item.
        /// </summary>
        public void CompleteTransitions()
        {
            FinishVectorTransitions();
            FinishPresence();
            _backgroundFade?.Snap();
            _borderFade?.Snap();
            OnCompleteTransitions();
            var count = VisualChildCount;
            for (var i = 0; i < count; i++)
                GetVisualChild(i)?.CompleteTransitions();
        }

        /// <summary>Snaps transitions a subclass keeps itself (text colour).</summary>
        internal virtual void OnCompleteTransitions()
        {
        }

        private void OnTransitionsChanged()
        {
            // A removed transition ends where it was heading.
            if (_scaleTransition is { IsRunning: true } && _transitions![TransitionProperty.Scale] == null)
                SetScaleShown(_scaleTransition.Target);
            if (_translationTransition is { IsRunning: true } && _transitions![TransitionProperty.Translation] == null)
                SetTranslationShown(_translationTransition.Target);
            if (_opacityTransition is { IsRunning: true } && _transitions![TransitionProperty.Opacity] == null)
                SetOpacityShown(_opacityTarget);
            if (_rotationTransition is { IsRunning: true } && _transitions![TransitionProperty.Rotation] == null)
                SetRotationShown(_rotationTransition.Target.X);
            if (_skewTransition is { IsRunning: true } && _transitions![TransitionProperty.Skew] == null)
                SetSkewShown(_skewTransition.Target);
        }

        private void FinishVectorTransitions()
        {
            if (!_transitionsRunning)
                return;
            if (_scaleTransition is { IsRunning: true })
                SetScaleShown(_scaleTransition.Target);
            if (_translationTransition is { IsRunning: true })
                SetTranslationShown(_translationTransition.Target);
            if (_opacityTransition is { IsRunning: true })
                SetOpacityShown(_opacityTarget);
            if (_rotationTransition is { IsRunning: true })
                SetRotationShown(_rotationTransition.Target.X);
            if (_skewTransition is { IsRunning: true })
                SetSkewShown(_skewTransition.Target);
            _transitionsRunning = false;
        }

        /// <summary>Starts (or retargets) the transition of <paramref name="property"/>; false when it has none.</summary>
        private bool TryTransition(TransitionProperty property, ref VectorTransition? runner, Vector2 shown, Vector2 target, float restDelta)
        {
            var spec = _transitions?[property];
            if (spec == null)
            {
                runner?.Stop();
                return false;
            }

            runner ??= new VectorTransition();
            runner.Start(spec, shown, target, ScreenSystem.TotalTime, restDelta);
            _transitionsRunning = true;
            RequestRedraw();
            return true;
        }

        private void UpdateTransitions()
        {
            if (!_transitionsRunning)
                return;

            var now = ScreenSystem.TotalTime;
            var running = false;
            if (_scaleTransition is { IsRunning: true } scale)
            {
                _scale = scale.Evaluate(now);
                running |= scale.IsRunning;
            }

            if (_translationTransition is { IsRunning: true } translation)
            {
                _translation = translation.Evaluate(now);
                running |= translation.IsRunning;
            }

            if (_opacityTransition is { IsRunning: true } opacity)
            {
                var shown = opacity.Evaluate(now).X;
                _opacity = opacity.IsRunning ? shown : _opacityTarget;
                running |= opacity.IsRunning;
            }

            if (_rotationTransition is { IsRunning: true } rotation)
            {
                _rotation = rotation.Evaluate(now).X;
                running |= rotation.IsRunning;
            }

            if (_skewTransition is { IsRunning: true } skew)
            {
                _skew = ClampSkew(skew.Evaluate(now));
                running |= skew.IsRunning;
            }

            _transitionsRunning = running;
            RequestRedraw();
        }

        // What is drawn now; differs from the property while a transition moves it. Explicit animations
        // read and write these: they take over from a transition of the same property.
        internal Vector2 ShownScale => _scale;
        internal Vector2 ShownTranslation => _translation;
        internal double ShownOpacity => _opacity;
        internal float ShownRotation => _rotation;
        internal Vector2 ShownSkew => _skew;

        internal void SetScaleShown(Vector2 value)
        {
            _scaleTransition?.Stop();
            if (_scale == value)
                return;
            _scale = value;
            InvalidateLayout(false);
        }

        internal void SetTranslationShown(Vector2 value)
        {
            _translationTransition?.Stop();
            if (_translation == value)
                return;
            _translation = value;
            InvalidateLayout(false);
        }

        internal void SetOpacityShown(double value)
        {
            _opacityTransition?.Stop();
            if (_opacity == value)
                return;
            _opacity = value;
            InvalidateLayout(false);
        }

        internal void SetRotationShown(float value)
        {
            _rotationTransition?.Stop();
            if (_rotation == value)
                return;
            _rotation = value;
            InvalidateLayout(false);
        }

        internal void SetSkewShown(Vector2 value)
        {
            _skewTransition?.Stop();
            value = ClampSkew(value);
            if (_skew == value)
                return;
            _skew = value;
            InvalidateLayout(false);
        }

        /// <summary>
        ///     The transition for a colour that is read at draw time: an explicit entry in
        ///     <see cref="Transitions"/>, otherwise the theme style's state transition.
        /// </summary>
        internal Transition? ResolveColorTransition(TransitionProperty property)
        {
            return _transitions?[property] ?? ResolveStateTransition();
        }

        /// <summary>The theme style's <see cref="ControlStyle.TransitionDuration"/> as a transition (null at zero).</summary>
        internal Transition? ResolveStateTransition()
        {
            var duration = ResolveStyle()?.TransitionDuration ?? TimeSpan.Zero;
            return StateTransitionFor(ref _stateTransition, duration);
        }

        internal static Transition? StateTransitionFor(ref Transition? cached, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                return null;
            if (cached == null || cached.Duration != duration)
                cached = new Transition(duration, Easings.QuadOut);
            return cached;
        }

        /// <summary>True when <paramref name="target"/> is drawn as a fade from a previous brush now.</summary>
        private bool UpdateFade(ref BrushFade? fade, Brush? target, TransitionProperty property)
        {
            var spec = ResolveColorTransition(property);
            if (fade == null)
            {
                if (spec == null)
                    return false;
                fade = new BrushFade();
            }

            return fade.Update(target, spec, ScreenSystem.TotalTime, ThemeVersion.Current);
        }

        private void DrawBorderFade(SpriteBatch spriteBatch, Rect rect, CornerRadius radius, Thickness border)
        {
            var fade = _borderFade!;
            if (fade.TryGetColor(out var color))
            {
                RoundedRectRenderer.DrawBorder(spriteBatch, rect, radius, border, Brush.ApplyOpacity(color, RenderOpacity));
                return;
            }

            fade.GetEnds(out var from, out var fromColor, out var fromIsColor, out var fromOpacity, out var to, out var toOpacity);
            if (fromIsColor)
                RoundedRectRenderer.DrawBorder(spriteBatch, rect, radius, border, Brush.ApplyOpacity(fromColor, RenderOpacity * fromOpacity));
            else if (from != null)
                DrawBorder(spriteBatch, rect, border, from, RenderOpacity * fromOpacity);
            if (to != null)
                DrawBorder(spriteBatch, rect, border, to, RenderOpacity * toOpacity);
        }
    }
}

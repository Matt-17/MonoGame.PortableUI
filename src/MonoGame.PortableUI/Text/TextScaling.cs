using System;
using System.Threading;

namespace MonoGame.PortableUI.Text
{
    /// <summary>Where <see cref="TextScaling.Factor"/> comes from.</summary>
    public enum TextScaleSource
    {
        /// <summary>Always 1: text is drawn at the theme sizes, whatever the OS or app say.</summary>
        Fixed,
        /// <summary>The app's own setting, <see cref="TextScaling.AppScale"/>.</summary>
        App,
        /// <summary>The operating system's font scale, as reported by the host in <see cref="TextScaling.SystemScale"/>.</summary>
        System
    }

    /// <summary>
    ///     The toolkit-wide text size factor. Every text-drawing control multiplies its font size by
    ///     <see cref="Factor"/>, and layout is measured at the scaled size. Screens pick up a change on
    ///     their next update and relayout.
    /// </summary>
    /// <remarks>
    ///     The system value is not read by the toolkit itself: a platform host reports it (Android:
    ///     <c>Resources.Configuration.FontScale</c>, also from <c>OnConfigurationChanged</c>).
    ///     The factor is clamped to <see cref="MinFactor"/>..<see cref="MaxFactor"/> (default 0.8..2.0)
    ///     so an extreme system value cannot break a layout.
    /// </remarks>
    public static class TextScaling
    {
        private static TextScaleSource _source = TextScaleSource.App;
        private static float _appScale = 1f;
        private static float _systemScale = 1f;
        private static float _minFactor = 0.8f;
        private static float _maxFactor = 2f;
        private static float _factor = 1f;
        private static long _version;

        /// <summary>Raised after <see cref="Factor"/> changed.</summary>
        public static event EventHandler? Changed;

        public static TextScaleSource Source
        {
            get => _source;
            set { _source = value; Recompute(); }
        }

        /// <summary>The app's own text size setting, used when <see cref="Source"/> is App.</summary>
        public static float AppScale
        {
            get => _appScale;
            set { _appScale = Sanitize(value); Recompute(); }
        }

        /// <summary>The OS font scale; platform hosts set it at startup and when it changes.</summary>
        public static float SystemScale
        {
            get => _systemScale;
            set { _systemScale = Sanitize(value); Recompute(); }
        }

        public static float MinFactor
        {
            get => _minFactor;
            set { _minFactor = Sanitize(value); Recompute(); }
        }

        public static float MaxFactor
        {
            get => _maxFactor;
            set { _maxFactor = Sanitize(value); Recompute(); }
        }

        /// <summary>The effective, clamped factor applied to all text.</summary>
        public static float Factor => _factor;

        internal static long Version => Interlocked.Read(ref _version);

        /// <summary>Restores the defaults (App source, scale 1, clamp 0.8..2).</summary>
        public static void Reset()
        {
            _source = TextScaleSource.App;
            _appScale = 1f;
            _systemScale = 1f;
            _minFactor = 0.8f;
            _maxFactor = 2f;
            Recompute();
        }

        private static float Sanitize(float value) => float.IsFinite(value) && value > 0 ? value : 1f;

        private static void Recompute()
        {
            var raw = _source switch
            {
                TextScaleSource.App => _appScale,
                TextScaleSource.System => _systemScale,
                _ => 1f
            };
            var factor = Math.Clamp(raw, Math.Min(_minFactor, _maxFactor), Math.Max(_minFactor, _maxFactor));
            if (factor == _factor)
                return;
            _factor = factor;
            Interlocked.Increment(ref _version);
            Changed?.Invoke(null, EventArgs.Empty);
        }
    }
}

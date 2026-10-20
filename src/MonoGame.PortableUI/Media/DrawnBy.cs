using System;
using System.Threading;

namespace MonoGame.PortableUI.Media
{
    /// <summary>
    ///     Which engine draws a shareable, mutable look (a brush, a text outline or glow), so a change to it redraws that
    ///     engine and drops its cached layers - or every engine when it is drawn by several or outside one.
    /// </summary>
    internal struct DrawnBy
    {
        private static int _globalVersion;
        private WeakReference<ScreenEngine>? _engine;
        private bool _many;

        /// <summary>Bumped when a look changes whose engine is unknown; engines poll it each update.</summary>
        public static int GlobalVersion => Volatile.Read(ref _globalVersion);

        /// <summary>Call while drawing the look.</summary>
        public void Mark()
        {
            if (_many)
                return;
            if (ScreenEngine.DrawingEngine is not { } engine)
            {
                // Drawn outside an engine (a host's own batch): remember that it is shown, owner unknown.
                if (_engine == null)
                    _many = true;
                return;
            }
            if (_engine == null)
                _engine = new WeakReference<ScreenEngine>(engine);
            else if (!_engine.TryGetTarget(out var previous))
                _engine.SetTarget(engine);
            else if (!ReferenceEquals(previous, engine))
                _many = true;
        }

        /// <summary>Call when the look changed: whoever shows it draws a new frame and re-renders its cached layers.</summary>
        public readonly void Changed()
        {
            // Never drawn yet (being built, e.g. in a constructor or a new theme): nobody shows it.
            if (_engine == null && !_many)
                return;
            if (!_many && _engine!.TryGetTarget(out var engine))
                engine.OnBrushChanged();
            else
                Interlocked.Increment(ref _globalVersion);
        }
    }
}

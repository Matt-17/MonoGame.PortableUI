using System;
using System.Threading;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    // Redraw bookkeeping for RenderMode.OnDemand: everything that changes what is on screen raises
    // a request here; the ScreenComponent draws only while one is pending.
    public partial class ScreenEngine
    {
        /// <summary>How long frames keep being drawn after input or reactivation: covers visual
        /// changes that follow an input without invalidating anything themselves.</summary>
        internal static readonly TimeSpan RedrawGrace = TimeSpan.FromMilliseconds(250);

        [ThreadStatic] private static ScreenEngine? _drawingEngine;

        private int _redrawRequested = 1;
        private TimeSpan _redrawAt = TimeSpan.MaxValue;
        private TimeSpan _redrawUntil;

        /// <summary>Frames drawn and frames skipped (OnDemand idle) since start — diagnostics.</summary>
        public long FramesDrawn { get; private set; }

        /// <inheritdoc cref="FramesDrawn"/>
        public long FramesSkipped { get; private set; }

        /// <summary>
        ///     Asks for the next frame to be drawn (<see cref="RenderMode.OnDemand"/>; a no-op in effect
        ///     when rendering continuously). Property changes, layout passes, input and animations call
        ///     this already; call it for changes the UI cannot see, e.g. a host drawing its own content.
        ///     Thread-safe.
        /// </summary>
        public void RequestRedraw() => Interlocked.Exchange(ref _redrawRequested, 1);

        /// <summary>Asks for a frame at <paramref name="time"/> (on the <see cref="ScreenSystem.TotalTime"/>
        /// clock), e.g. the next caret blink. Game thread only.</summary>
        public void RequestRedrawAt(TimeSpan time)
        {
            if (time < _redrawAt)
                _redrawAt = time;
        }

        /// <summary>Keeps drawing every frame until <paramref name="duration"/> has passed.</summary>
        internal void RequestRedrawFor(TimeSpan duration)
        {
            RequestRedraw();
            var until = ScreenSystem.TotalTime + duration;
            if (until > _redrawUntil)
                _redrawUntil = until;
        }

        /// <summary>
        ///     Called while drawing by a visual that changes with time on its own (an animated brush,
        ///     a spinner, a fading scroll bar): asks the engine that is drawing it for the next frame.
        ///     Does nothing outside a draw.
        /// </summary>
        public static void RequestAnimationFrame() => _drawingEngine?.RequestRedraw();

        /// <summary>Like <see cref="RequestAnimationFrame"/>, for a visual that next changes at
        /// <paramref name="time"/> (e.g. a blinking caret).</summary>
        public static void RequestAnimationFrameAt(TimeSpan time) => _drawingEngine?.RequestRedrawAt(time);

        /// <summary>Input arrived: draw now and for a short grace period.</summary>
        internal void NoteInputActivity() => RequestRedrawFor(RedrawGrace);

        /// <summary>
        ///     Whether a frame must be drawn now. Clears the pending request, so a request raised while
        ///     drawing (an animation asking for its next frame) carries over to the next update.
        /// </summary>
        internal bool ConsumeRedrawRequest()
        {
            var requested = Interlocked.Exchange(ref _redrawRequested, 0) != 0;
            var now = ScreenSystem.TotalTime;
            var due = now >= _redrawAt;
            if (due)
                _redrawAt = TimeSpan.MaxValue;
            return requested || due || now < _redrawUntil || IsTransitioning || DebugOverlayEnabled;
        }

        internal void RecordFrame(bool drawn)
        {
            if (drawn)
                FramesDrawn++;
            else
                FramesSkipped++;
        }

        /// <summary>Marks this engine as the one drawing, for <see cref="RequestAnimationFrame"/>.</summary>
        private ScreenEngine? EnterDraw()
        {
            var previous = _drawingEngine;
            _drawingEngine = this;
            return previous;
        }

        private static void ExitDraw(ScreenEngine? previous) => _drawingEngine = previous;
    }
}

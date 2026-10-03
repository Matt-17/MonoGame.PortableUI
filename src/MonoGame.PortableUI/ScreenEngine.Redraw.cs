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
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private int _gameThreadId = -1;
        private TimeSpan _redrawAt = TimeSpan.MaxValue;
        private TimeSpan _redrawUntil;
        private int _redrawFrames;

        /// <summary>Frames drawn after the window comes back (resume, device reset). Counted in frames,
        /// not time: game time jumps on resume, and Android only drops its task snapshot once the new
        /// surface has received a few frames - a single one is not enough.</summary>
        internal const int SurfaceRestoreFrames = 10;

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
        public void RequestRedraw()
        {
            Interlocked.Exchange(ref _redrawRequested, 1);
            // From another thread the loop may be idling: wake it. (Game-thread calls are frequent and
            // never happen while the loop waits.)
            if (Environment.CurrentManagedThreadId != _gameThreadId)
                _wake.Set();
        }

        /// <summary>
        ///     Ends an idle wait of the <see cref="RenderMode.OnDemand"/> loop now, so input that arrives
        ///     on another thread (e.g. the Android input bridge) is handled without waiting out
        ///     <see cref="ScreenEngineOptions.IdleUpdateInterval"/>. Thread-safe.
        /// </summary>
        public void WakeUp() => _wake.Set();

        /// <summary>The idle loop's wait: returns after <paramref name="timeout"/>, at <see cref="WakeUp"/>,
        /// or right away when woken since the last wait.</summary>
        internal void WaitIdle(TimeSpan timeout)
        {
            if (timeout.TotalMilliseconds >= 1)
                _wake.WaitOne(timeout);
        }

        /// <summary>When the next scheduled frame (<see cref="RequestRedrawAt"/>) is due; MaxValue for none.</summary>
        internal TimeSpan NextScheduledRedraw => _redrawAt;

        internal void MarkGameThread() => _gameThreadId = Environment.CurrentManagedThreadId;

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
        public static void RequestAnimationFrame()
        {
            if (_drawingEngine is not { } engine)
                return;
            engine.RequestRedraw();
            engine.AnimationFrameRequests++;
        }

        /// <summary>Like <see cref="RequestAnimationFrame"/>, for a visual that next changes at
        /// <paramref name="time"/> (e.g. a blinking caret).</summary>
        public static void RequestAnimationFrameAt(TimeSpan time)
        {
            if (_drawingEngine is not { } engine)
                return;
            engine.RequestRedrawAt(time);
            if (time < engine.EarliestAnimationFrameAt)
                engine.EarliestAnimationFrameAt = time;
        }

        /// <summary>Draws at least the next <paramref name="frames"/> frames. Thread-safe.</summary>
        internal void RequestRedrawFrames(int frames)
        {
            int current;
            do
            {
                current = Volatile.Read(ref _redrawFrames);
                if (current >= frames)
                    break;
            }
            while (Interlocked.CompareExchange(ref _redrawFrames, frames, current) != current);
            RequestRedraw();
        }

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
            var counted = false;
            int frames;
            while ((frames = Volatile.Read(ref _redrawFrames)) > 0)
            {
                if (Interlocked.CompareExchange(ref _redrawFrames, frames - 1, frames) == frames)
                {
                    counted = true;
                    break;
                }
            }
            return requested || due || counted || now < _redrawUntil || IsTransitioning || DebugOverlayEnabled;
        }

        internal void RecordFrame(bool drawn)
        {
            if (drawn)
                FramesDrawn++;
            else
                FramesSkipped++;
        }

        /// <summary>The quality level in effect (<see cref="ScreenEngineOptions.RenderQuality"/> with Auto
        /// resolved), refreshed every update.</summary>
        public RenderQuality EffectiveRenderQuality { get; private set; } = RenderQuality.High;

        /// <summary>Frames per second drawing is capped at (0 = no cap): <see cref="ScreenEngineOptions.MaxFrameRate"/>,
        /// or 30 at <see cref="RenderQuality.Low"/>.</summary>
        public int EffectiveMaxFrameRate
            => Options.MaxFrameRate > 0 ? Options.MaxFrameRate : EffectiveRenderQuality == RenderQuality.Low ? 30 : 0;

        /// <summary>Quality of the engine currently drawing (High outside a draw), for brushes and effects.</summary>
        internal static RenderQuality DrawingQuality => _drawingEngine?.EffectiveRenderQuality ?? RenderQuality.High;

        /// <summary>Whether decorations that never rest (glass sweeps, film-grain noise) animate.</summary>
        internal static bool AnimatesDecorations => DrawingQuality == RenderQuality.High;

        internal void UpdateRenderQuality()
        {
            var quality = Options.RenderQuality;
            if (quality == RenderQuality.Auto)
                quality = PlatformPowerState.IsPowerSaveMode ? RenderQuality.Low : RenderQuality.High;
            if (quality == EffectiveRenderQuality)
                return;
            EffectiveRenderQuality = quality;
            InvalidateLayerCaches();
            RequestRedraw();
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

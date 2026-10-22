using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI
{
    /// <summary>How the old look gives way to the new one in <see cref="ScreenEngine.TransitionTheme"/>.</summary>
    public enum ThemeTransitionStyle
    {
        /// <summary>The old frame fades out over the new one.</summary>
        Crossfade,

        /// <summary>The new look spreads as a growing circle from a point (the switch that was tapped).</summary>
        Reveal,

        /// <summary>
        ///     Each element changes on its own (for long, unprompted changes - a sunset schedule, the system's dark mode),
        ///     as its <see cref="Controls.Control.ThemeFade"/> says: evenly and gently by default, while elements marked
        ///     <see cref="Controls.ThemeFadeMode.Flip"/> (a selection indicator, a knob, high-contrast text) stay near their
        ///     old look, flip quickly when their parent passes the middle and settle, so the UI stays legible all along.
        ///     Needs a live change that rebuilds the content (the old and new tree are paired element by element, by place
        ///     or by <see cref="Controls.Control.Tag"/>); otherwise it is a cross-fade.
        /// </summary>
        ElementFade
    }

    // A theme change without a jump: the last frame in the old look is kept as a picture, the new theme is applied,
    // and the old look fades out (or opens up as a circle) over the live UI in the new look. Live (the default): when
    // apply replaced the visible screens or their content, the old trees go on - laid out, animated, drawn every frame
    // with the old theme, without input - as ghost screens, and the old look is drawn from them each frame; where it
    // changed controls in place there is no old tree, and the picture stays.
    // Changes stack: one started while another runs takes what is on screen then as its old look, and the earlier change
    // goes on inside it (live, it is composited into the new one's old look every frame), so nothing cuts. A change
    // that ends takes the earlier ones along (they lie inside its old look, which is gone).
    // The UI stays usable during a change, however long: ghost scroll viewers with a StateKey follow the new tree's
    // positions, a navigation (other screens shown) finishes the change within a quarter second, and a ghost is drawn
    // again only when something in it changed.
    public partial class ScreenEngine
    {
        // (Zero, InverseSourceAlpha): what is drawn punches a hole into the picture.
        private static readonly BlendState Punch = new BlendState
        {
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.InverseSourceAlpha
        };

        private Action? _themeCapture;
        private Action? _themeApply;
        // The change waiting for its picture (_themeCapture) or its apply (_themeApply).
        private ThemeLayer? _themePending;
        // Running changes, oldest first.
        private readonly List<ThemeLayer> _themeLayers = new List<ThemeLayer>();
        private RenderTargetBinding[]? _themePreviousTargets;
        private RenderTargetBinding[]? _themeFrameTargets;
        // Set from any thread when the frame was lost; the game thread drops the pictures on its next update.
        private volatile bool _themeFrameLost;

        private bool _themeWarmUp;

        /// <summary>
        ///     Pays the first theme change's one-time costs ahead of time - at app start, or when a scheduled change is
        ///     near: the frame-sized targets the change draws into are created at the next draw, and the element pairing
        ///     runs once. Optional; without it the first change pays this in its first frames.
        /// </summary>
        public void PrepareThemeTransition()
        {
            _themeWarmUp = true;
            RequestRedraw();
        }

        /// <summary>Whether a theme change is fading in.</summary>
        public bool IsThemeTransitioning => _themeCapture != null || _themeApply != null || _themeLayers.Count > 0;

        /// <summary>
        ///     Changes the look softly: the next frame is kept as a picture in the old look, then <paramref name="apply"/>
        ///     runs (switch the theme, rebuild what depends on it) and the picture gives way to the new look over
        ///     <paramref name="duration"/> (Crossfade 350 ms, Reveal 550 ms by default).
        ///     <paramref name="backdrop"/>: what the game clears the frame with under the UI, so the picture is opaque.
        ///     <paramref name="origin"/> (layout units): the centre of a <see cref="ThemeTransitionStyle.Reveal"/>;
        ///     the screen's centre when null. A zero duration applies at once.
        ///     <paramref name="live"/> (default): when <paramref name="apply"/> replaces the visible screens or their
        ///     <see cref="Screen.Content"/> (a rebuild in the new look), the old trees keep running in the old part - their
        ///     animations (a sliding indicator, a ripple, a switch) go on - instead of a still picture; where it changes the
        ///     same controls in place, the picture is used. False: always the picture.
        ///     Called while a change still runs, the new one starts from what is on screen then; the running one goes on
        ///     inside its old look.
        ///     <paramref name="easing"/>: how the change runs over its duration (0..1 in, 0..1 out); null = ease in and
        ///     out. For long, unprompted changes <see cref="ThemeTransitionStyle.ElementFade"/> keeps text legible.
        /// </summary>
        public void TransitionTheme(Action apply, ThemeTransitionStyle style = ThemeTransitionStyle.Crossfade, PointF? origin = null,
            TimeSpan? duration = null, Color? backdrop = null, bool live = true, Animation.Easing? easing = null)
        {
            var length = duration ?? TimeSpan.FromMilliseconds(style == ThemeTransitionStyle.Reveal ? 550 : 350);
            if (length <= TimeSpan.Zero || ActiveScreen == null)
            {
                apply();
                return;
            }
            // A change that still waits for its picture or its apply is applied first; the new picture shows it.
            if (_themeApply is { } waiting)
            {
                _themeApply = null;
                StartPendingTheme(waiting);
            }
            if (_themeCapture is { } uncaptured)
            {
                _themeCapture = null;
                _themePending?.ReleaseTarget();
                _themePending = null;
                uncaptured();
            }
            _themeCapture = apply;
            _themePending = new ThemeLayer
            {
                Style = style,
                Origin = origin,
                Duration = length,
                Backdrop = backdrop ?? Color.Transparent,
                Live = live,
                Easing = easing
            };
            RequestRedraw();
        }

        /// <summary>At the start of an update: the picture was taken, the new look comes in now.</summary>
        private void ApplyPendingTheme()
        {
            if (_themeFrameLost)
            {
                _themeFrameLost = false;
                EndThemeTransition();
            }
            if (_themeApply is not { } apply)
                return;
            _themeApply = null;
            StartPendingTheme(apply);
        }

        private void StartPendingTheme(Action apply)
        {
            var layer = _themePending;
            _themePending = null;
            if (layer == null)
            {
                apply();
                return;
            }
            if (!layer.Live)
            {
                apply();
                layer.Shown.AddRange(VisibleScreens);
            }
            else
            {
                // What is shown before apply: if each screen (or its content) is replaced, the old ones go on as ghosts.
                var oldTheme = Options.Theme;
                var screens = new List<Screen>(VisibleScreens);
                var contents = new Control?[screens.Count];
                var backgrounds = new Media.Brush?[screens.Count];
                for (var i = 0; i < screens.Count; i++)
                {
                    contents[i] = screens[i].Content;
                    backgrounds[i] = screens[i].BackgroundBrush;
                }
                apply();
                layer.Shown.AddRange(VisibleScreens);
                StartLiveTheme(layer, oldTheme, screens, contents, backgrounds);
            }
            // Element by element needs every old tree beside its new one; else the whole look cross-fades.
            if (layer.Style == ThemeTransitionStyle.ElementFade && !PairsElements(layer.Style, layer.Ghosts))
                layer.Style = ThemeTransitionStyle.Crossfade;
            if (layer.Style == ThemeTransitionStyle.ElementFade)
                layer.ReleaseTarget();
            layer.Start = ScreenSystem.TotalTime;
            _themeLayers.Add(layer);
            RequestRedraw();
        }

        private void StartLiveTheme(ThemeLayer layer, PortableTheme oldTheme, List<Screen> screens, Control?[] contents, Media.Brush?[] backgrounds)
        {
            var current = VisibleScreens;
            var ghosts = new List<Screen>(screens.Count);
            for (var i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                var replaced = true;
                for (var j = 0; j < current.Count; j++)
                    replaced &= !ReferenceEquals(current[j], screen);
                // Changed in place: no old tree to draw, the picture stays.
                if (!replaced && (contents[i] == null || ReferenceEquals(screen.Content, contents[i])))
                    return;
                ghosts.Add(replaced ? screen : new GhostScreen(contents[i]!, backgrounds[i], screen));
            }

            // Paired, the old trees are never drawn as a whole and keep the layout they had on screen.
            var keep = PairsElements(layer.Style, ghosts);
            foreach (var ghost in ghosts)
            {
                if (ghost is not GhostScreen)
                    layer.Reused.Add((ghost, ghost.InputSource, ghost.ScreenEngine));
                ghost.ThemeOverride = oldTheme;
                ghost.ScreenEngine = this;
                ghost.InputSource = Input.NullInputSource.Instance;
                if (keep)
                    ghost.KeepArrangement();
                else
                    ghost.InvalidateLayout(true);
                layer.Ghosts.Add(ghost);
                if (ghost is GhostScreen { Content: { } content } paired)
                    CollectPairedScrolls(content, paired.Source, layer.PairedScrolls);
            }
        }

        private static bool PairsElements(ThemeTransitionStyle style, List<Screen> ghosts)
            => style == ThemeTransitionStyle.ElementFade && ghosts.Count > 0 && ghosts.TrueForAll(g => g is GhostScreen);

        // Scroll viewers with a StateKey in an old tree follow the position kept on the screen the new tree lives in.
        private static void CollectPairedScrolls(Control control, Screen source, List<(ScrollViewer Viewer, Screen Source)> pairs)
        {
            if (control is ScrollViewer { StateKey: not null } viewer)
                pairs.Add((viewer, source));
            foreach (var child in control.GetDescendants())
                CollectPairedScrolls(child, source, pairs);
        }

        private static bool SameScreens(List<Screen> shown, IReadOnlyList<Screen> now)
        {
            if (shown.Count != now.Count)
                return false;
            for (var i = 0; i < shown.Count; i++)
            {
                if (!ReferenceEquals(shown[i], now[i]))
                    return false;
            }
            return true;
        }

        /// <summary>The screens drawing the old look of the newest live change (empty: none, or the picture is used).</summary>
        internal IReadOnlyList<Screen> ThemeGhosts => _themeLayers.Count > 0 ? _themeLayers[_themeLayers.Count - 1].Ghosts : Array.Empty<Screen>();

        /// <summary>How many changes run at once (tests).</summary>
        internal int ThemeLayerCount => _themeLayers.Count;

        /// <summary>How far change <paramref name="index"/> (oldest first) has come, 0..1 (tests).</summary>
        internal float ThemeLayerProgress(int index) => Math.Min(1, Progress(_themeLayers[index]));

        /// <summary>Tests: as if the picture of the old look was taken (that happens while drawing).</summary>
        internal void TakeThemePictureForTests()
        {
            _themeApply = _themeCapture;
            _themeCapture = null;
        }

        /// <summary>Hurry: what is left of a change after a navigation, in milliseconds.</summary>
        private const double ThemeHurryMilliseconds = 250;

        /// <summary>The old trees of live changes take their step: layout, transitions, animations (no input). Their
        /// paired scroll viewers follow the new tree; a navigation finishes the changes quickly.</summary>
        private void UpdateLiveTheme()
        {
            if (_themeLayers.Count == 0)
                return;
            var shown = VisibleScreens;
            var now = ScreenSystem.TotalTime;
            foreach (var layer in _themeLayers)
            {
                if (layer.HurryStart == null && !SameScreens(layer.Shown, shown))
                {
                    layer.HurryFrom = Progress(layer);
                    layer.HurryStart = now;
                }
                SyncPairedScrolls(layer);
                foreach (var ghost in layer.Ghosts)
                    ghost.Update();
                if (layer.Style == ThemeTransitionStyle.ElementFade)
                {
                    foreach (var ghost in layer.Ghosts)
                    {
                        if (ghost is GhostScreen paired)
                            BuildThemeTwins(paired, paired.Source, layer);
                    }
                }
            }
        }

        // The old look's keyed scroll viewers take the new tree's positions (flinging included, frame by frame).
        private static void SyncPairedScrolls(ThemeLayer layer)
        {
            foreach (var (viewer, source) in layer.PairedScrolls)
            {
                if (source.ScrollStates.TryGetValue(viewer.StateKey!, out var offset) && viewer.Offset != offset)
                    viewer.ScrollTo(offset, revealScrollBar: false);
            }
        }

        private static void EndLayer(ThemeLayer layer)
        {
            foreach (var ghost in layer.Ghosts)
            {
                if (ghost is GhostScreen { Source: { } source } && ReferenceEquals(source.ThemeTwinGhost, ghost))
                {
                    source.ThemeTwins = null;
                    source.ThemeTwinGhost = null;
                    source.InvalidateLayout(false);
                }
                ghost.ThemeOverride = null;
                if (ghost is GhostScreen own)
                    own.Content = null;
            }
            foreach (var (screen, input, engine) in layer.Reused)
            {
                // Back to how it was handed over (it may be navigated to again).
                screen.InputSource = input;
                screen.ScreenEngine = engine;
                screen.InvalidateLayout(true);
            }
            layer.Ghosts.Clear();
            layer.Reused.Clear();
            layer.PairedScrolls.Clear();
            layer.ReleaseTarget();
        }

        /// <summary>Hosts the content a live theme change replaced, in the old look.</summary>
        private sealed class GhostScreen : Screen
        {
            public GhostScreen(Control content, Media.Brush? background, Screen source)
            {
                BackgroundBrush = background;
                Content = content;
                Source = source;
            }

            /// <summary>The screen the content came from (where the new tree lives now).</summary>
            public Screen Source { get; }
        }

        /// <summary>One change: its old look (a picture, or redrawn from ghosts each frame) and how it gives way.</summary>
        private sealed class ThemeLayer
        {
            public RenderTarget2D? Target;
            public TimeSpan Start;
            public ThemeTransitionStyle Style;
            public PointF? Origin;
            public TimeSpan Duration;
            public Color Backdrop;
            public bool Live;
            public Animation.Easing? Easing;
            // The ghosts must be drawn again (first frame, or they asked for an animation frame while drawing).
            public bool NeedsRender = true;
            public TimeSpan? HurryStart;
            public float HurryFrom;
            // The screens shown right after apply: when they change, the user navigated.
            public readonly List<Screen> Shown = new List<Screen>();
            public readonly List<(ScrollViewer Viewer, Screen Source)> PairedScrolls = new();
            public readonly List<Screen> Ghosts = new List<Screen>();

            public void ReleaseTarget()
            {
                if (Target is { } target)
                    Effects.RenderTargetPool.Return(target.GraphicsDevice, target);
                Target = null;
            }
            public readonly List<(Screen Screen, Input.IInputSource Input, ScreenEngine? Engine)> Reused = new();
        }

        /// <summary>Draws the stack for this frame: into the picture first when a theme change waits for it.</summary>
        internal void DrawStackWithThemeTransition(SpriteBatch spriteBatch)
        {
            if (_themeWarmUp)
            {
                _themeWarmUp = false;
                WarmUpThemeTransition(spriteBatch.GraphicsDevice);
            }
            if (_themeCapture != null)
            {
                CaptureThemeSnapshot(spriteBatch);
                return;
            }
            DrawStack(spriteBatch);
            DrawThemeLayers(spriteBatch);
        }

        private void WarmUpThemeTransition(GraphicsDevice device)
        {
            // Two: the picture, and a full-frame pass (a ProgressiveBlur) drawn into it.
            var width = Math.Max(1, device.Viewport.Width);
            var height = Math.Max(1, device.Viewport.Height);
            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themePreviousTargets);
            var first = Effects.RenderTargetPool.Rent(device, width, height);
            var second = Effects.RenderTargetPool.Rent(device, width, height);
            foreach (var target in new[] { first, second })
            {
                device.SetRenderTarget(target);
                device.Clear(Color.Transparent);
            }
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
            Effects.RenderTargetPool.Return(device, first);
            Effects.RenderTargetPool.Return(device, second);

            var twins = new Dictionary<Control, ThemeTwin>();
            foreach (var screen in VisibleScreens)
            {
                if (screen.Content is { } content)
                    Pair(content, content, new ThemeTwin(null, 0, 1, false, false), twins);
            }
            foreach (var twin in twins.Values)
                twin.Mix(0.5f, TimeSpan.FromSeconds(8));
        }

        private void CaptureThemeSnapshot(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var width = Math.Max(1, device.Viewport.Width);
            var height = Math.Max(1, device.Viewport.Height);
            var layer = _themePending!;
            layer.ReleaseTarget();
            // Pooled: creating a frame-sized target stalls the GPU for a frame or more.
            layer.Target = Effects.RenderTargetPool.Rent(device, width, height);

            // What is on screen now - running changes included - is the old look.
            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themePreviousTargets);
            device.SetRenderTarget(layer.Target);
            device.Clear(layer.Backdrop);
            DrawStack(spriteBatch);
            DrawThemeLayers(spriteBatch);
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            // This frame shows the picture itself; the new look starts with the next update.
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(layer.Target, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
            RecordBatchFlush();

            _themeApply = _themeCapture;
            _themeCapture = null;
            RequestRedraw();
        }

        // Composites the running changes over what is bound: each live change's old look is redrawn from its ghosts with
        // the change before it composited in, then the newest one goes over the frame.
        private void DrawThemeLayers(SpriteBatch spriteBatch)
        {
            if (_themeFrameLost || _themeLayers.Count == 0)
                return;

            // A change that ended takes the ones before it along: they lay inside its old look.
            for (var i = _themeLayers.Count - 1; i >= 0; i--)
            {
                if (Progress(_themeLayers[i]) < 1)
                    continue;
                for (var j = 0; j <= i; j++)
                    EndLayer(_themeLayers[j]);
                _themeLayers.RemoveRange(0, i + 1);
                break;
            }
            if (_themeLayers.Count == 0)
                return;

            var device = spriteBatch.GraphicsDevice;
            var frameTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themeFrameTargets);
            for (var i = 0; i < _themeLayers.Count; i++)
            {
                var layer = _themeLayers[i];
                // An element fade draws inside the new tree itself.
                if (layer.Style == ThemeTransitionStyle.ElementFade || layer.Ghosts.Count == 0 || layer.Target == null || layer.Target.IsDisposed)
                    continue;
                // An old look that did not change keeps its target (a reveal's hole only grows, a fade draws it fainter);
                // one with an earlier change inside it changes every frame.
                // The new tree is drawn: its scroll positions are final for this frame, the old look takes them now.
                SyncPairedScrolls(layer);
                var dirty = layer.NeedsRender || i > 0;
                foreach (var ghost in layer.Ghosts)
                    dirty |= ghost.VisualDirty;
                if (!dirty)
                    continue;
                device.SetRenderTarget(layer.Target);
                device.Clear(layer.Backdrop);
                var requests = AnimationFrameRequests;
                var previous = EnterDraw();
                try
                {
                    foreach (var ghost in layer.Ghosts)
                    {
                        ghost.VisualDirty = false;
                        ghost.Draw(spriteBatch);
                    }
                }
                finally
                {
                    ExitDraw(previous);
                }
                layer.NeedsRender = AnimationFrameRequests != requests;
                if (i > 0 && _themeLayers[i - 1].Style != ThemeTransitionStyle.ElementFade)
                    ComposeLayer(spriteBatch, _themeLayers[i - 1], layer.Target, frameTargets);
            }

            var newest = _themeLayers[_themeLayers.Count - 1];
            if (newest.Style != ThemeTransitionStyle.ElementFade)
                ComposeLayer(spriteBatch, newest, null, frameTargets);
            RecordBatchFlush();
            RequestRedraw();
        }

        /// <summary>Middle stretch of a flip, in seconds: the blend goes from 20 % to 80 % in this time.</summary>
        private const double FlipCrossingSeconds = 0.6;

        // A ThemeFadeMode.Flip element: ease in and out when short; a long one drifts to 20 % over the first half less the
        // crossing, crosses to 80 % in FlipCrossingSeconds (grey on grey only for a moment), and drifts on to the end.
        internal static float FlipCurve(float t, TimeSpan duration)
        {
            static float Smooth(float x) => x <= 0 ? 0 : x >= 1 ? 1 : x * x * (3 - 2 * x);
            var crossing = (float)(FlipCrossingSeconds / Math.Max(0.001, duration.TotalSeconds));
            if (crossing >= 0.6f)
                return Smooth(t);
            var start = 0.5f - crossing / 2;
            if (t < start)
                return 0.2f * Smooth(t / start);
            if (t < start + crossing)
                return 0.2f + 0.6f * Smooth((t - start) / crossing);
            return 0.8f + 0.2f * Smooth((t - start - crossing) / start);
        }

        private static float Progress(ThemeLayer layer)
        {
            if (layer.HurryStart is { } hurry)
            {
                var rest = (float)Math.Min(1, (ScreenSystem.TotalTime - hurry).TotalMilliseconds / ThemeHurryMilliseconds);
                return layer.HurryFrom + (1 - layer.HurryFrom) * Math.Max(0, rest);
            }
            return Math.Max(0, (float)((ScreenSystem.TotalTime - layer.Start).TotalMilliseconds / Math.Max(1, layer.Duration.TotalMilliseconds)));
        }

        // Lays a change's old look over a target (null: the frame's targets), punched by the growing circle or faded.
        private void ComposeLayer(SpriteBatch spriteBatch, ThemeLayer layer, RenderTarget2D? onto, RenderTargetBinding[] frameTargets)
        {
            if (layer.Target == null || layer.Target.IsDisposed)
                return;
            var device = spriteBatch.GraphicsDevice;
            var width = layer.Target.Width;
            var height = layer.Target.Height;
            var t = Math.Min(1, Progress(layer));

            var fade = 1f;
            if (layer.Style == ThemeTransitionStyle.Reveal)
            {
                // The hole only grows, so it is punched into the picture itself, frame by frame.
                var pixels = width / Math.Max(1f, ScreenRect.Width);
                var origin = layer.Origin is { } o ? new Vector2(o.X * pixels, o.Y * pixels) : new Vector2(width / 2f, height / 2f);
                var farthest = 0f;
                foreach (var corner in new[] { Vector2.Zero, new Vector2(width, 0), new Vector2(0, height), new Vector2(width, height) })
                    farthest = Math.Max(farthest, Vector2.Distance(origin, corner));
                // Ease in and out; the soft rim (a few % of the radius) is past the farthest corner at the end.
                var eased = layer.Easing is { } easing ? (float)easing(t) : t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
                var radius = eased * farthest * 1.08f;
                var disc = RevealMask(device);
                device.SetRenderTarget(layer.Target);
                spriteBatch.Begin(SpriteSortMode.Deferred, Punch);
                spriteBatch.Draw(disc, origin, null, Color.White, 0, new Vector2(disc.Width / 2f), radius * 2 / disc.Width, SpriteEffects.None, 0);
                spriteBatch.End();
            }
            else
            {
                fade = 1 - (layer.Easing is { } easing ? (float)easing(t) : t * t * (3 - 2 * t));
            }

            if (onto != null)
                device.SetRenderTarget(onto);
            else if (frameTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(frameTargets);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(layer.Target, new Rectangle(0, 0, width, height), Color.White * fade);
            spriteBatch.End();
        }

        private static Texture2D? _revealMask;

        // A white disc whose rim fades over its outer 2.5 %: magnified to a whole screen, a one-texel edge would
        // show as stairs, a ramp several texels wide stays round.
        private static Texture2D RevealMask(GraphicsDevice device)
        {
            if (_revealMask != null && !_revealMask.IsDisposed && ReferenceEquals(_revealMask.GraphicsDevice, device))
                return _revealMask;
            const int size = 512;
            const float half = size / 2f;
            const float rim = 0.025f;
            var data = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = MathF.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                    var t = MathHelper.Clamp((1 - d) / rim, 0, 1);
                    data[y * size + x] = Color.White * (t * t * (3 - 2 * t));
                }
            }
            _revealMask = new Texture2D(device, size, size, false, SurfaceFormat.Color);
            _revealMask.SetData(data);
            return _revealMask;
        }

        /// <summary>
        ///     The frame was lost with the surface (any thread: Android raises <c>Game.Activated</c> on its UI thread,
        ///     where disposing a render target would wait for the paused game thread): the pictures go on the next update.
        /// </summary>
        internal void ThemeFrameLost() => _themeFrameLost = true;

        /// <summary>Drops the pictures (done, or their frame was lost with the surface). On the game thread.</summary>
        internal void EndThemeTransition()
        {
            foreach (var layer in _themeLayers)
                EndLayer(layer);
            _themeLayers.Clear();
            if (_themeCapture is { } capture)
            {
                // Never taken: apply without the soft change.
                _themeCapture = null;
                _themePending?.ReleaseTarget();
                _themePending = null;
                capture();
            }
        }
    }
}

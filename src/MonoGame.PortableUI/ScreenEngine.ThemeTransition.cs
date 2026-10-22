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
        Reveal
    }

    // A theme change without a jump: the last frame in the old look is kept as a picture, the new theme is applied,
    // and the old look fades out (or opens up as a circle) over the live UI in the new look. Live (the default): when
    // apply replaced the visible screens or their content, the old trees go on - laid out, animated, drawn every frame
    // with the old theme, without input - as ghost screens, and the old look is drawn from them each frame; where it
    // changed controls in place there is no old tree, and the picture stays.
    // Changes stack: one started while another runs takes what is on screen then as its old look, and the earlier change
    // goes on inside it (live, it is composited into the new one's old look every frame), so nothing cuts. A change
    // that ends takes the earlier ones along (they lie inside its old look, which is gone).
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
        /// </summary>
        public void TransitionTheme(Action apply, ThemeTransitionStyle style = ThemeTransitionStyle.Crossfade, PointF? origin = null,
            TimeSpan? duration = null, Color? backdrop = null, bool live = true)
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
                _themePending?.Target?.Dispose();
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
                Live = live
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
                StartLiveTheme(layer, oldTheme, screens, contents, backgrounds);
            }
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
                ghosts.Add(replaced ? screen : new GhostScreen(contents[i]!, backgrounds[i]));
            }

            foreach (var ghost in ghosts)
            {
                if (ghost is not GhostScreen)
                    layer.Reused.Add((ghost, ghost.InputSource, ghost.ScreenEngine));
                ghost.ThemeOverride = oldTheme;
                ghost.ScreenEngine = this;
                ghost.InputSource = Input.NullInputSource.Instance;
                ghost.InvalidateLayout(true);
                layer.Ghosts.Add(ghost);
            }
        }

        /// <summary>The screens drawing the old look of the newest live change (empty: none, or the picture is used).</summary>
        internal IReadOnlyList<Screen> ThemeGhosts => _themeLayers.Count > 0 ? _themeLayers[_themeLayers.Count - 1].Ghosts : Array.Empty<Screen>();

        /// <summary>How many changes run at once (tests).</summary>
        internal int ThemeLayerCount => _themeLayers.Count;

        /// <summary>Tests: as if the picture of the old look was taken (that happens while drawing).</summary>
        internal void TakeThemePictureForTests()
        {
            _themeApply = _themeCapture;
            _themeCapture = null;
        }

        /// <summary>The old trees of live changes take their step: layout, transitions, animations (no input).</summary>
        private void UpdateLiveTheme()
        {
            foreach (var layer in _themeLayers)
            {
                foreach (var ghost in layer.Ghosts)
                    ghost.Update();
            }
        }

        private static void EndLayer(ThemeLayer layer)
        {
            foreach (var ghost in layer.Ghosts)
            {
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
            layer.Target?.Dispose();
            layer.Target = null;
        }

        /// <summary>Hosts the content a live theme change replaced, in the old look.</summary>
        private sealed class GhostScreen : Screen
        {
            public GhostScreen(Control content, Media.Brush? background)
            {
                BackgroundBrush = background;
                Content = content;
            }
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
            public readonly List<Screen> Ghosts = new List<Screen>();
            public readonly List<(Screen Screen, Input.IInputSource Input, ScreenEngine? Engine)> Reused = new();
        }

        /// <summary>Draws the stack for this frame: into the picture first when a theme change waits for it.</summary>
        internal void DrawStackWithThemeTransition(SpriteBatch spriteBatch)
        {
            if (_themeCapture != null)
            {
                CaptureThemeSnapshot(spriteBatch);
                return;
            }
            DrawStack(spriteBatch);
            DrawThemeLayers(spriteBatch);
        }

        private void CaptureThemeSnapshot(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var width = Math.Max(1, device.Viewport.Width);
            var height = Math.Max(1, device.Viewport.Height);
            var layer = _themePending!;
            layer.Target?.Dispose();
            layer.Target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

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
                if (layer.Ghosts.Count == 0 || layer.Target == null || layer.Target.IsDisposed)
                    continue;
                device.SetRenderTarget(layer.Target);
                device.Clear(layer.Backdrop);
                var previous = EnterDraw();
                try
                {
                    foreach (var ghost in layer.Ghosts)
                        ghost.Draw(spriteBatch);
                }
                finally
                {
                    ExitDraw(previous);
                }
                if (i > 0)
                    ComposeLayer(spriteBatch, _themeLayers[i - 1], layer.Target, frameTargets);
            }

            ComposeLayer(spriteBatch, _themeLayers[_themeLayers.Count - 1], null, frameTargets);
            RecordBatchFlush();
            RequestRedraw();
        }

        private static float Progress(ThemeLayer layer)
            => Math.Max(0, (float)((ScreenSystem.TotalTime - layer.Start).TotalMilliseconds / Math.Max(1, layer.Duration.TotalMilliseconds)));

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
                var eased = t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
                var radius = eased * farthest * 1.08f;
                var disc = RevealMask(device);
                device.SetRenderTarget(layer.Target);
                spriteBatch.Begin(SpriteSortMode.Deferred, Punch);
                spriteBatch.Draw(disc, origin, null, Color.White, 0, new Vector2(disc.Width / 2f), radius * 2 / disc.Width, SpriteEffects.None, 0);
                spriteBatch.End();
            }
            else
            {
                fade = 1 - t * t * (3 - 2 * t);
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
                _themePending?.Target?.Dispose();
                _themePending = null;
                capture();
            }
        }
    }
}

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
        private RenderTarget2D? _themeSnapshot;
        private RenderTargetBinding[]? _themePreviousTargets;
        private TimeSpan? _themeStart;
        private ThemeTransitionStyle _themeStyle;
        private PointF? _themeOrigin;
        private TimeSpan _themeDuration;
        private Color _themeBackdrop;
        private bool _themeLive;
        // The old look while a live change runs, bottom first; reused screens are the ones apply replaced.
        private readonly List<Screen> _themeGhosts = new List<Screen>();
        private readonly List<(Screen Screen, Input.IInputSource Input, ScreenEngine? Engine)> _themeReusedScreens = new();
        // Set from any thread when the frame was lost; the game thread drops the picture on its next update.
        private volatile bool _themeFrameLost;

        /// <summary>Whether a theme change is fading in.</summary>
        public bool IsThemeTransitioning => _themeCapture != null || _themeApply != null || _themeStart != null;

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
            // A change while one is still waiting: the waiting one is applied first, the new picture shows both.
            if (_themeApply is { } waiting)
            {
                _themeApply = null;
                waiting();
            }
            _themeCapture = apply;
            _themeStart = null;
            _themeStyle = style;
            _themeOrigin = origin;
            _themeDuration = length;
            _themeBackdrop = backdrop ?? Color.Transparent;
            _themeLive = live;
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
            EndLiveTheme();
            if (!_themeLive)
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
                StartLiveTheme(oldTheme, screens, contents, backgrounds);
            }
            _themeStart = ScreenSystem.TotalTime;
            RequestRedraw();
        }

        private void StartLiveTheme(PortableTheme oldTheme, List<Screen> screens, Control?[] contents, Media.Brush?[] backgrounds)
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
                    _themeReusedScreens.Add((ghost, ghost.InputSource, ghost.ScreenEngine));
                ghost.ThemeOverride = oldTheme;
                ghost.ScreenEngine = this;
                ghost.InputSource = Input.NullInputSource.Instance;
                ghost.InvalidateLayout(true);
                _themeGhosts.Add(ghost);
            }
        }

        /// <summary>The screens drawing the old look of a live change (empty: none, or the picture is used).</summary>
        internal IReadOnlyList<Screen> ThemeGhosts => _themeGhosts;

        /// <summary>Tests: as if the picture of the old look was taken (that happens while drawing).</summary>
        internal void TakeThemePictureForTests()
        {
            _themeApply = _themeCapture;
            _themeCapture = null;
        }

        /// <summary>The old trees of a live change take their step: layout, transitions, animations (no input).</summary>
        private void UpdateLiveTheme()
        {
            foreach (var ghost in _themeGhosts)
                ghost.Update();
        }

        private void EndLiveTheme()
        {
            foreach (var ghost in _themeGhosts)
            {
                ghost.ThemeOverride = null;
                if (ghost is GhostScreen own)
                    own.Content = null;
            }
            foreach (var (screen, input, engine) in _themeReusedScreens)
            {
                // Back to how it was handed over (it may be navigated to again).
                screen.InputSource = input;
                screen.ScreenEngine = engine;
                screen.InvalidateLayout(true);
            }
            _themeGhosts.Clear();
            _themeReusedScreens.Clear();
        }

        // The old look of the live change into the picture's target, every frame.
        private void DrawLiveTheme(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themePreviousTargets);
            device.SetRenderTarget(_themeSnapshot);
            device.Clear(_themeBackdrop);
            var previous = EnterDraw();
            try
            {
                foreach (var ghost in _themeGhosts)
                    ghost.Draw(spriteBatch);
            }
            finally
            {
                ExitDraw(previous);
            }
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);
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

        /// <summary>Draws the stack for this frame: into the picture first when a theme change waits for it.</summary>
        internal void DrawStackWithThemeTransition(SpriteBatch spriteBatch)
        {
            if (_themeCapture != null)
            {
                CaptureThemeSnapshot(spriteBatch);
                return;
            }
            DrawStack(spriteBatch);
            DrawThemeSnapshot(spriteBatch);
        }

        private void CaptureThemeSnapshot(SpriteBatch spriteBatch)
        {
            var device = spriteBatch.GraphicsDevice;
            var width = Math.Max(1, device.Viewport.Width);
            var height = Math.Max(1, device.Viewport.Height);
            if (_themeSnapshot == null || _themeSnapshot.IsDisposed || _themeSnapshot.Width != width || _themeSnapshot.Height != height)
            {
                _themeSnapshot?.Dispose();
                _themeSnapshot = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themePreviousTargets);
            device.SetRenderTarget(_themeSnapshot);
            device.Clear(_themeBackdrop);
            DrawStack(spriteBatch);
            if (previousTargets.Length == 0)
                device.SetRenderTarget(null);
            else
                device.SetRenderTargets(previousTargets);

            // This frame shows the picture itself; the new look starts with the next update.
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_themeSnapshot, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
            RecordBatchFlush();

            _themeApply = _themeCapture;
            _themeCapture = null;
            RequestRedraw();
        }

        private void DrawThemeSnapshot(SpriteBatch spriteBatch)
        {
            if (_themeFrameLost)
                return;
            if (_themeStart is not { } start || _themeSnapshot == null || _themeSnapshot.IsDisposed)
                return;
            var t = (float)((ScreenSystem.TotalTime - start).TotalMilliseconds / Math.Max(1, _themeDuration.TotalMilliseconds));
            if (t >= 1)
            {
                EndThemeTransition();
                return;
            }
            t = Math.Max(0, t);
            var device = spriteBatch.GraphicsDevice;
            var width = device.Viewport.Width;
            var height = device.Viewport.Height;
            if (_themeGhosts.Count > 0)
                DrawLiveTheme(spriteBatch);

            if (_themeStyle == ThemeTransitionStyle.Reveal)
            {
                // The hole only grows, so it is punched into the picture itself, frame by frame.
                var pixels = width / Math.Max(1f, ScreenRect.Width);
                var origin = _themeOrigin is { } o ? new Vector2(o.X * pixels, o.Y * pixels) : new Vector2(width / 2f, height / 2f);
                var farthest = 0f;
                foreach (var corner in new[] { Vector2.Zero, new Vector2(width, 0), new Vector2(0, height), new Vector2(width, height) })
                    farthest = Math.Max(farthest, Vector2.Distance(origin, corner));
                // Ease in and out; the soft rim (a few % of the radius) is past the farthest corner at the end.
                var eased = t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
                var radius = eased * farthest * 1.08f;
                var disc = RevealMask(device);
                var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(device, ref _themePreviousTargets);
                device.SetRenderTarget(_themeSnapshot);
                spriteBatch.Begin(SpriteSortMode.Deferred, Punch);
                spriteBatch.Draw(disc, origin, null, Color.White, 0, new Vector2(disc.Width / 2f), radius * 2 / disc.Width, SpriteEffects.None, 0);
                spriteBatch.End();
                if (previousTargets.Length == 0)
                    device.SetRenderTarget(null);
                else
                    device.SetRenderTargets(previousTargets);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(_themeSnapshot, new Rectangle(0, 0, width, height), Color.White);
                spriteBatch.End();
            }
            else
            {
                var fade = 1 - t * t * (3 - 2 * t);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(_themeSnapshot, new Rectangle(0, 0, width, height), Color.White * fade);
                spriteBatch.End();
            }
            RecordBatchFlush();
            RequestRedraw();
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
        ///     where disposing a render target would wait for the paused game thread): the picture goes on the next update.
        /// </summary>
        internal void ThemeFrameLost() => _themeFrameLost = true;

        /// <summary>Drops the picture (done, or its frame was lost with the surface). On the game thread.</summary>
        internal void EndThemeTransition()
        {
            EndLiveTheme();
            _themeStart = null;
            _themeSnapshot?.Dispose();
            _themeSnapshot = null;
            if (_themeCapture is { } capture)
            {
                // Never taken: apply without the soft change.
                _themeCapture = null;
                capture();
            }
        }
    }
}

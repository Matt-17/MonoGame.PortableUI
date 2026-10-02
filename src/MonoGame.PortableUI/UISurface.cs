using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI
{
    public sealed class UISurface : IDisposable
    {
        private readonly Game _game;
        private SpriteBatch? _spriteBatch;
        private RenderTarget2D? _target;
        private RenderTargetBinding[]? _previousTargets;
        private int _width;
        private int _height;
        private float _layoutScale = 1f;

        public UISurface(Game game, Screen screen, int width, int height, PortableTheme? theme = null)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            Screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _width = Math.Max(1, width);
            _height = Math.Max(1, height);
            Engine = ScreenEngine.CreateSurfaceEngine(game, new ScreenEngineOptions
            {
                AddComponentToGame = false,
                ScreenSizeMode = ScreenSizeMode.Manual,
                Theme = theme ?? PortableTheme.CreateDefault()
            });
            Engine.SetScreenSize(_width, _height);
            Engine.NavigateToScreen(Screen);
        }

        public ScreenEngine Engine { get; }

        /// <summary>
        ///     Effects of the in-world display this surface is shown on (e.g. CRT curvature,
        ///     scanlines); independent of the theme. Shortcut for <c>Engine.Options.PostEffects</c>.
        /// </summary>
        public IReadOnlyList<PostEffect> PostEffects
        {
            get => Engine.Options.PostEffects;
            set => Engine.Options.PostEffects = value;
        }
        public Screen Screen { get; }
        public RenderTarget2D Target => EnsureTarget();
        public PortableTheme Theme
        {
            get { return Engine.Options.Theme; }
            set { Engine.Options.Theme = value; }
        }

        /// <summary>
        /// Optional external scene texture the screen's glass brushes blur behind the UI
        /// (see <see cref="Screen.ExternalBackdrop"/>).
        /// </summary>
        public Texture2D? ExternalBackdrop
        {
            get { return Screen.ExternalBackdrop; }
            set { Screen.ExternalBackdrop = value; }
        }

        /// <summary>
        ///     Pixels per layout unit: the surface is laid out at its width × height but rendered at
        ///     width × height × LayoutScale (HiDPI, density-independent layouts). Default 1.
        /// </summary>
        public float LayoutScale
        {
            get => _layoutScale;
            set
            {
                value = value > 0 ? value : 1f;
                if (_layoutScale.Equals(value))
                    return;
                _layoutScale = value;
                Engine.SetNativeRenderScale(value);
                _target?.Dispose();
                _target = null;
            }
        }

        private int PixelWidth => Math.Max(1, (int)Math.Ceiling(_width * _layoutScale));
        private int PixelHeight => Math.Max(1, (int)Math.Ceiling(_height * _layoutScale));

        public bool IsInteractive { get; set; } = true;
        public bool HasKeyboardFocus { get; internal set; }
        public float ScaleFactor { get; set; } = 1;
        public bool ShowSoftwareCursor { get; set; } = true;
        public PointF SoftwareCursorPosition { get; set; }
        public Color SoftwareCursorColor { get; set; } = Color.White;
        public IInputSource InputSource
        {
            get { return Screen.InputSource; }
            set { Screen.InputSource = value ?? NullInputSource.Instance; }
        }

        public PostProcessManager? PostProcessManager { get; private set; }

        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_width == width && _height == height)
                return;

            _width = width;
            _height = height;
            _target?.Dispose();
            _target = null;
            Engine.SetScreenSize(width, height);
        }

        public void Update(GameTime gameTime)
        {
            if (!IsInteractive)
                return;

            Engine.Update(gameTime);
        }

        public RenderTarget2D Draw(GameTime gameTime)
        {
            var target = EnsureTarget();
            _spriteBatch ??= new SpriteBatch(_game.GraphicsDevice);
            PostProcessManager ??= new PostProcessManager(_game.GraphicsDevice);
            PostProcessManager.BeginFrame();

            // Restore whatever was bound (e.g. the host screen's post-FX target), not just null.
            var previousTargets = Effects.RenderTargetHelper.SnapshotRenderTargets(_game.GraphicsDevice, ref _previousTargets);
            _game.GraphicsDevice.SetRenderTarget(target);
            _game.GraphicsDevice.Clear(Color.Transparent);
            // The whole stack: overlays/modals pushed on this surface's engine and its toasts too.
            Engine.DrawStack(_spriteBatch);
            if (ShowSoftwareCursor)
                DrawSoftwareCursor(_spriteBatch);
            if (previousTargets.Length == 0)
                _game.GraphicsDevice.SetRenderTarget(null);
            else
                _game.GraphicsDevice.SetRenderTargets(previousTargets);
            return target;
        }

        public void Dispose()
        {
            _target?.Dispose();
            _spriteBatch?.Dispose();
            PostProcessManager?.Dispose();
            Engine.Dispose();
        }

        private RenderTarget2D EnsureTarget()
        {
            if (_target != null && _target.Width == PixelWidth && _target.Height == PixelHeight)
                return _target;

            _target?.Dispose();
            // PreserveContents: Screen.Draw may switch to blur/post-FX targets mid-frame and come back.
            _target = new RenderTarget2D(_game.GraphicsDevice, PixelWidth, PixelHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _target;
        }

        /// <summary>
        ///     True when <paramref name="surfacePoint"/> (surface units, e.g. from mapping a ray onto the
        ///     quad) lies on the visible picture — inside the surface and, with a CRT barrel among the
        ///     theme or display effects, inside the curved image rather than its dark border. Hosts use
        ///     it to route input and to switch between the in-game and the system cursor.
        /// </summary>
        public bool IsPointOnDisplay(PointF surfacePoint)
        {
            var rect = new Rect(0, 0, _width, _height);
            var distortion = BarrelDistortion();
            if (distortion > 0)
                surfacePoint = PostProcessManager.InverseBarrel(surfacePoint, rect, distortion);
            return rect.Contains(surfacePoint);
        }

        private float BarrelDistortion()
        {
            var display = Engine.Options.PostEffects;
            var barrel = display.Count > 0 ? Screen.FindEnabledBarrel(display) : null;
            if (barrel == null && Engine.Options.Theme?.PostEffects is { Count: > 0 } themeEffects)
                barrel = Screen.FindEnabledBarrel(themeEffects);
            return barrel == null ? 0 : MathHelper.Clamp(barrel.Distortion, 0, 0.5f);
        }

        // Classic arrow pointer, 12×19 (Windows shape): '#' outline, '.' fill, ' ' transparent.
        private static readonly string[] ArrowRows =
        {
            "#",
            "##",
            "#.#",
            "#..#",
            "#...#",
            "#....#",
            "#.....#",
            "#......#",
            "#.......#",
            "#........#",
            "#.........#",
            "#......#####",
            "#...#..#",
            "#..##..#",
            "#.#  #..#",
            "##   #..#",
            "#     #..#",
            "      #..#",
            "       ##"
        };

        /// <summary>Outline color of the software cursor (the fill is <see cref="SoftwareCursorColor"/>).</summary>
        public Color SoftwareCursorOutlineColor { get; set; } = Color.Black;

        private void DrawSoftwareCursor(SpriteBatch spriteBatch)
        {
            // Target pixels: layout position × LayoutScale; one bitmap pixel per layout unit.
            var scale = _layoutScale;
            var x = SoftwareCursorPosition.X * scale;
            var y = SoftwareCursorPosition.Y * scale;
            var pixel = Media.Primitives.Pixel(spriteBatch);
            spriteBatch.Begin();
            for (var row = 0; row < ArrowRows.Length; row++)
            {
                var line = ArrowRows[row];
                var start = 0;
                while (start < line.Length)
                {
                    var c = line[start];
                    var end = start;
                    while (end < line.Length && line[end] == c)
                        end++;
                    if (c != ' ')
                    {
                        var color = c == '#' ? SoftwareCursorOutlineColor : SoftwareCursorColor;
                        spriteBatch.Draw(pixel, new Rect(x + start * scale, y + row * scale, (end - start) * scale, scale), color);
                    }
                    start = end;
                }
            }
            spriteBatch.End();
            Engine.RecordBatchFlush();
        }
    }
}

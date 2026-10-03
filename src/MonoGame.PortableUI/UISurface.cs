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
        /// <summary>
        ///     True while this surface receives the keyboard: the game window's text input then goes
        ///     to this surface only (not to the main UI, not to other surfaces). Set it directly for a
        ///     single in-world screen, or let a <see cref="SurfaceFocusManager"/> switch between several.
        /// </summary>
        public bool HasKeyboardFocus
        {
            // Derived from the single owner, so focusing one surface unfocuses any other.
            get => ReferenceEquals(ScreenEngine.GetTextInputOwner(_game), Engine);
            set
            {
                if (value)
                    ScreenEngine.SetTextInputOwner(_game, Engine);
                else if (HasKeyboardFocus)
                    ScreenEngine.SetTextInputOwner(_game, null);
            }
        }
        public float ScaleFactor { get; set; } = 1;
        /// <summary>
        ///     Draws the theme's pointer (<see cref="PortableTheme.Cursor"/>) inside the surface, bent by
        ///     its display effects. Shortcut for <c>Engine.Options.ShowSoftwareCursor</c>; the pointer
        ///     position comes from <see cref="InputSource"/>.
        /// </summary>
        /// <summary>Text-mode grid of this surface's display (shortcut for <c>Engine.Options.TextGrid</c>); per surface.</summary>
        public TextGrid? TextGrid
        {
            get => Engine.Options.TextGrid;
            set => Engine.Options.TextGrid = value;
        }

        public bool ShowSoftwareCursor
        {
            get => Engine.Options.ShowSoftwareCursor;
            set => Engine.Options.ShowSoftwareCursor = value;
        }
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
            if (previousTargets.Length == 0)
                _game.GraphicsDevice.SetRenderTarget(null);
            else
                _game.GraphicsDevice.SetRenderTargets(previousTargets);
            return target;
        }

        public void Dispose()
        {
            HasKeyboardFocus = false;
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
            // Half a pixel of slack: a point exactly on the edge survives the barrel round trip.
            return surfacePoint.X > -0.5f && surfacePoint.Y > -0.5f && surfacePoint.X < _width + 0.5f && surfacePoint.Y < _height + 0.5f;
        }

        /// <summary>The UI point under a point of the (curved) picture — undoes a CRT barrel.</summary>
        public PointF MapDisplayToUi(PointF surfacePoint)
        {
            var distortion = BarrelDistortion();
            return distortion > 0 ? PostProcessManager.InverseBarrel(surfacePoint, new Rect(0, 0, _width, _height), distortion) : surfacePoint;
        }

        /// <summary>Where a UI point appears on the (curved) picture — applies a CRT barrel.</summary>
        public PointF MapUiToDisplay(PointF uiPoint)
        {
            var distortion = BarrelDistortion();
            return distortion > 0 ? PostProcessManager.ForwardBarrel(uiPoint, new Rect(0, 0, _width, _height), distortion) : uiPoint;
        }

        private float BarrelDistortion()
        {
            // Only display effects curve the picture (a theme's curvature is ignored), the same
            // rule the screen's input mapping follows.
            var display = Engine.Options.PostEffects;
            var barrel = display.Count > 0 ? Screen.FindEnabledBarrel(display) : null;
            return barrel == null ? 0 : MathHelper.Clamp(barrel.Distortion, 0, 0.5f);
        }

    }
}

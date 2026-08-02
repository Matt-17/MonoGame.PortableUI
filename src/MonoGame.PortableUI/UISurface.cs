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
        private int _width;
        private int _height;

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
        public Screen Screen { get; }
        public RenderTarget2D Target => EnsureTarget();
        public PortableTheme Theme
        {
            get { return Engine.Options.Theme; }
            set { Engine.Options.Theme = value; }
        }

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

            _game.GraphicsDevice.SetRenderTarget(target);
            _game.GraphicsDevice.Clear(Color.Transparent);
            Screen.Draw(_spriteBatch);
            if (ShowSoftwareCursor)
                DrawSoftwareCursor(_spriteBatch);
            _game.GraphicsDevice.SetRenderTarget(null);
            return target;
        }

        public void Dispose()
        {
            _target?.Dispose();
            _spriteBatch?.Dispose();
        }

        private RenderTarget2D EnsureTarget()
        {
            if (_target != null && _target.Width == _width && _target.Height == _height)
                return _target;

            _target?.Dispose();
            // PreserveContents: Screen.Draw may switch to blur/post-FX targets mid-frame and come back.
            _target = new RenderTarget2D(_game.GraphicsDevice, _width, _height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _target;
        }

        private void DrawSoftwareCursor(SpriteBatch spriteBatch)
        {
            var x = SoftwareCursorPosition.X;
            var y = SoftwareCursorPosition.Y;
            spriteBatch.Begin();
            spriteBatch.Draw(Media.SolidColorBrush.Pixel, new Rect(x, y, 10, 2), SoftwareCursorColor);
            spriteBatch.Draw(Media.SolidColorBrush.Pixel, new Rect(x, y, 2, 14), SoftwareCursorColor);
            spriteBatch.Draw(Media.SolidColorBrush.Pixel, new Rect(x + 2, y + 10, 8, 2), SoftwareCursorColor);
            spriteBatch.End();
            Engine.RecordBatchFlush();
        }
    }
}

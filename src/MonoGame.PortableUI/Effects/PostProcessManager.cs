using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Effects
{
    /// <summary>
    ///     Applies the per-theme post-process chain (R9). All effects are implemented shader-free:
    ///     scanlines/dot-matrix/vignette/grain are cached overlay textures, the CRT barrel is a
    ///     distortion mesh drawn with <see cref="BasicEffect"/>, and bloom reuses the
    ///     <see cref="BackdropManager"/> blur chain drawn additively.
    /// </summary>
    public sealed class PostProcessManager : IDisposable
    {
        private const int BarrelColumns = 32;
        private const int BarrelRows = 24;

        private readonly GraphicsDevice _graphicsDevice;
        private RenderTarget2D? _uiTarget;
        private RenderTarget2D? _islandTarget;
        private BasicEffect? _basicEffect;
        private VertexPositionColorTexture[]? _barrelVertices;
        private short[]? _barrelIndices;
        private int _grainSeed;

        public PostProcessManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            Capabilities = new RenderCapabilities(graphicsDevice);
        }

        /// <summary>The graphics device this manager's render targets are bound to.</summary>
        public GraphicsDevice GraphicsDevice => _graphicsDevice;

        public RenderCapabilities Capabilities { get; }

        public int AppliedEffectsThisFrame { get; private set; }

        public void BeginFrame()
        {
            AppliedEffectsThisFrame = 0;
        }

        public bool CanApply(PostEffect effect)
        {
            return effect is { Enabled: true };
        }

        public int CountEnabled(IReadOnlyList<PostEffect> effects)
        {
            var count = 0;
            foreach (var effect in effects)
            {
                if (effect.Enabled)
                    count++;
            }

            return count;
        }

        public void RecordApplied(PostEffect effect)
        {
            if (CanApply(effect))
                AppliedEffectsThisFrame++;
        }

        /// <summary>Render target the UI is drawn into when a post-process chain is active.</summary>
        internal RenderTarget2D EnsureUiTarget(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_uiTarget != null && !_uiTarget.IsDisposed && _uiTarget.Width == width && _uiTarget.Height == height)
                return _uiTarget;

            _uiTarget?.Dispose();
            _uiTarget = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _uiTarget;
        }

        /// <summary>Target used to render a ThemeIsland subtree before composing its post-FX chain.</summary>
        internal RenderTarget2D EnsureIslandTarget(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            if (_islandTarget != null && !_islandTarget.IsDisposed && _islandTarget.Width == width && _islandTarget.Height == height)
                return _islandTarget;

            _islandTarget?.Dispose();
            _islandTarget = new RenderTarget2D(_graphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            return _islandTarget;
        }

        /// <summary>
        ///     Maps a physically displayed point back to the undistorted UI point for the barrel
        ///     forward map used by <see cref="DrawBarrel"/>. Pure and allocation-free so input
        ///     routing can call it per pointer sample.
        /// </summary>
        public static PointF InverseBarrel(PointF displayed, Rect rect, float distortion)
        {
            distortion = MathHelper.Clamp(distortion, 0, 0.5f);
            if (distortion <= 0 || rect.Width <= 0 || rect.Height <= 0)
                return displayed;

            var px = (displayed.X - rect.Left) / rect.Width * 2 - 1;
            var py = (displayed.Y - rect.Top) / rect.Height * 2 - 1;
            var q = (px * px + py * py) / 2f;

            // Forward: displayed = n·(1−d·ρ) with ρ=(nx²+ny²)/2, so q = ρ·(1−d·ρ)².
            // Newton on f(ρ) = ρ·(1−d·ρ)² − q; quadratic convergence, ~4 iterations for
            // theme-typical d≈0.08. (For d > 1/3 the forward map folds at the far corners;
            // Newton then lands on the nearest root, which is the best available answer.)
            var rho = q;
            for (var i = 0; i < 12; i++)
            {
                var inner = 1 - distortion * rho;
                var f = rho * inner * inner - q;
                var derivative = inner * (1 - 3 * distortion * rho);
                if (Math.Abs(derivative) < 1e-6f)
                    break;

                var next = rho - f / derivative;
                if (next < 0)
                    next = 0;
                if (Math.Abs(next - rho) < 1e-7f)
                {
                    rho = next;
                    break;
                }

                rho = next;
            }

            var s = Math.Max(0.5f, 1 - distortion * rho);
            var nx = px / s;
            var ny = py / s;
            return new PointF(
                rect.Left + (nx * 0.5f + 0.5f) * rect.Width,
                rect.Top + (ny * 0.5f + 0.5f) * rect.Height);
        }

        /// <summary>
        ///     Draws the rendered UI to the currently bound target, applying the effect chain.
        ///     <paramref name="sourceRect"/> selects the region of <paramref name="ui"/> holding the
        ///     content (used for ThemeIsland composition); defaults to the full texture.
        /// </summary>
        internal void Compose(SpriteBatch spriteBatch, RenderTarget2D ui, IReadOnlyList<PostEffect> effects, Rect screenRect, BackdropManager backdrop, Rect? sourceRect = null)
        {
            var source = sourceRect ?? new Rect(0, 0, ui.Width, ui.Height);
            Texture2D? bloomTexture = null;
            var bloom = Find<BloomPostEffect>(effects);
            if (bloom != null)
            {
                var previousTargets = _graphicsDevice.GetRenderTargets();
                bloomTexture = backdrop.Blur(spriteBatch, ui);
                RestoreTargets(previousTargets);
            }

            var barrel = Find<CrtBarrelPostEffect>(effects);
            // Shader path (R3): without a barrel mesh, scanlines/dot-matrix/vignette/grain are
            // applied in a single pass while drawing the UI texture.
            var shaderOverlaysApplied = false;
            if (barrel != null)
            {
                DrawBarrel(ui, screenRect, barrel, source);
                RecordApplied(barrel);
            }
            else if (EffectCache.TryGetEffect(_graphicsDevice, EffectNames.PostFx, out var postFx) && postFx != null)
            {
                shaderOverlaysApplied = DrawWithPostFxShader(spriteBatch, postFx, ui, effects, screenRect, source);
            }

            if (!shaderOverlaysApplied && barrel == null)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
                spriteBatch.Draw(ui, (Rectangle)screenRect, (Rectangle)source, Color.White);
                spriteBatch.End();
            }

            if (bloom != null && bloomTexture != null)
            {
                var bloomScaleX = bloomTexture.Width / (float)ui.Width;
                var bloomScaleY = bloomTexture.Height / (float)ui.Height;
                var bloomSource = new Rectangle(
                    (int)(source.Left * bloomScaleX),
                    (int)(source.Top * bloomScaleY),
                    Math.Max(1, (int)(source.Width * bloomScaleX)),
                    Math.Max(1, (int)(source.Height * bloomScaleY)));
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
                spriteBatch.Draw(bloomTexture, (Rectangle)screenRect, bloomSource, Color.White * MathHelper.Clamp(bloom.Strength, 0, 1));
                spriteBatch.End();
                RecordApplied(bloom);
            }

            if (!shaderOverlaysApplied)
            {
                foreach (var effect in effects)
                {
                    if (!CanApply(effect))
                        continue;

                    switch (effect)
                    {
                        case ScanlinePostEffect scanlines:
                            DrawTiledOverlay(spriteBatch, GetScanlineTexture(scanlines), screenRect, scanlines.Strength, randomOffset: false);
                            RecordApplied(scanlines);
                            break;
                        case DotMatrixPostEffect dotMatrix:
                            DrawTiledOverlay(spriteBatch, GetDotMatrixTexture(dotMatrix), screenRect, dotMatrix.Strength, randomOffset: false);
                            RecordApplied(dotMatrix);
                            break;
                        case VignettePostEffect vignette:
                            DrawVignette(spriteBatch, screenRect, vignette.Strength);
                            RecordApplied(vignette);
                            break;
                        case FilmGrainPostEffect grain:
                            DrawTiledOverlay(spriteBatch, GetGrainTexture(), screenRect, grain.Strength, randomOffset: true);
                            RecordApplied(grain);
                            break;
                    }
                }
            }

            if (barrel != null && barrel.Vignette > 0)
                DrawVignette(spriteBatch, screenRect, barrel.Vignette);
        }

        private bool DrawWithPostFxShader(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Graphics.Effect postFx, RenderTarget2D ui, IReadOnlyList<PostEffect> effects, Rect screenRect, Rect source)
        {
            var scanlines = Find<ScanlinePostEffect>(effects);
            var dotMatrix = Find<DotMatrixPostEffect>(effects);
            var vignette = Find<VignettePostEffect>(effects);
            var grain = Find<FilmGrainPostEffect>(effects);

            _grainSeed = unchecked(_grainSeed * 1103515245 + 12345);
            postFx.Parameters["TexelSize"]?.SetValue(new Vector2(1f / ui.Width, 1f / ui.Height));
            postFx.Parameters["SourceSize"]?.SetValue(new Vector2(source.Width, source.Height));
            postFx.Parameters["ScanlineStrength"]?.SetValue(scanlines == null ? 0 : MathHelper.Clamp(scanlines.Strength, 0, 1));
            postFx.Parameters["ScanlineSpacing"]?.SetValue(scanlines == null ? 3f : Math.Max(2f, scanlines.Spacing));
            postFx.Parameters["DotMatrixStrength"]?.SetValue(dotMatrix == null ? 0 : MathHelper.Clamp(dotMatrix.Strength, 0, 1));
            postFx.Parameters["DotMatrixCellSize"]?.SetValue(dotMatrix == null ? 3f : Math.Max(2f, dotMatrix.CellSize));
            postFx.Parameters["VignetteStrength"]?.SetValue(vignette == null ? 0 : MathHelper.Clamp(vignette.Strength, 0, 1));
            postFx.Parameters["GrainStrength"]?.SetValue(grain == null ? 0 : MathHelper.Clamp(grain.Strength, 0, 1) * 0.35f);
            postFx.Parameters["GrainSeed"]?.SetValue(Math.Abs(_grainSeed % 1000) / 10f);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, effect: postFx);
            spriteBatch.Draw(ui, (Rectangle)screenRect, (Rectangle)source, Color.White);
            spriteBatch.End();

            if (scanlines != null)
                RecordApplied(scanlines);
            if (dotMatrix != null)
                RecordApplied(dotMatrix);
            if (vignette != null)
                RecordApplied(vignette);
            if (grain != null)
                RecordApplied(grain);
            return true;
        }

        public void Dispose()
        {
            _uiTarget?.Dispose();
            _islandTarget?.Dispose();
            _basicEffect?.Dispose();
        }

        private static T? Find<T>(IReadOnlyList<PostEffect> effects) where T : PostEffect
        {
            foreach (var effect in effects)
            {
                if (effect is T match && match.Enabled)
                    return match;
            }

            return null;
        }

        private void RestoreTargets(RenderTargetBinding[] targets)
        {
            if (targets.Length == 0)
                _graphicsDevice.SetRenderTarget(null);
            else
                _graphicsDevice.SetRenderTargets(targets);
        }

        private void DrawTiledOverlay(SpriteBatch spriteBatch, Texture2D texture, Rect screenRect, float strength, bool randomOffset)
        {
            strength = MathHelper.Clamp(strength, 0, 1);
            if (strength <= 0)
                return;

            var offsetX = 0;
            var offsetY = 0;
            if (randomOffset)
            {
                _grainSeed = unchecked(_grainSeed * 1103515245 + 12345);
                offsetX = (_grainSeed >> 8) & (texture.Width - 1);
                offsetY = (_grainSeed >> 16) & (texture.Height - 1);
            }

            var source = new Rectangle(offsetX, offsetY, (int)Math.Ceiling(screenRect.Width), (int)Math.Ceiling(screenRect.Height));
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            spriteBatch.Draw(texture, (Rectangle)screenRect, source, Color.White * strength);
            spriteBatch.End();
        }

        private void DrawVignette(SpriteBatch spriteBatch, Rect screenRect, float strength)
        {
            strength = MathHelper.Clamp(strength, 0, 1);
            if (strength <= 0)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            spriteBatch.Draw(GetVignetteTexture(), (Rectangle)screenRect, Color.White * strength);
            spriteBatch.End();
        }

        private void DrawBarrel(RenderTarget2D ui, Rect screenRect, CrtBarrelPostEffect barrel, Rect source)
        {
            var vertexCount = (BarrelColumns + 1) * (BarrelRows + 1);
            _barrelVertices ??= new VertexPositionColorTexture[vertexCount];
            _barrelIndices ??= CreateGridIndices(BarrelColumns, BarrelRows);

            var distortion = MathHelper.Clamp(barrel.Distortion, 0, 0.5f);
            for (var row = 0; row <= BarrelRows; row++)
            {
                for (var column = 0; column <= BarrelColumns; column++)
                {
                    var u = column / (float)BarrelColumns;
                    var v = row / (float)BarrelRows;
                    var ndcX = u * 2 - 1;
                    var ndcY = v * 2 - 1;
                    var r2 = (ndcX * ndcX + ndcY * ndcY) / 2f;
                    var scale = 1 - distortion * r2;
                    var x = screenRect.Left + (ndcX * scale * 0.5f + 0.5f) * screenRect.Width;
                    var y = screenRect.Top + (ndcY * scale * 0.5f + 0.5f) * screenRect.Height;
                    _barrelVertices[row * (BarrelColumns + 1) + column] = new VertexPositionColorTexture(
                        new Vector3(x, y, 0),
                        Color.White,
                        new Vector2(
                            (source.Left + u * source.Width) / ui.Width,
                            (source.Top + v * source.Height) / ui.Height));
                }
            }

            _basicEffect ??= new BasicEffect(_graphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity
            };
            var viewport = _graphicsDevice.Viewport;
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            _basicEffect.Texture = ui;

            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _barrelVertices,
                    0,
                    vertexCount,
                    _barrelIndices,
                    0,
                    _barrelIndices.Length / 3);
            }
        }

        private static short[] CreateGridIndices(int columns, int rows)
        {
            var indices = new short[columns * rows * 6];
            var index = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var topLeft = (short)(row * (columns + 1) + column);
                    var topRight = (short)(topLeft + 1);
                    var bottomLeft = (short)(topLeft + columns + 1);
                    var bottomRight = (short)(bottomLeft + 1);
                    indices[index++] = topLeft;
                    indices[index++] = topRight;
                    indices[index++] = bottomLeft;
                    indices[index++] = topRight;
                    indices[index++] = bottomRight;
                    indices[index++] = bottomLeft;
                }
            }

            return indices;
        }

        private Texture2D GetScanlineTexture(ScanlinePostEffect scanlines)
        {
            var spacing = Math.Max(2, (int)Math.Round(scanlines.Spacing));
            return BrushTextureCache.GetOrCreate(_graphicsDevice, new BrushTextureCacheKey("postfx-scanline", spacing), device =>
            {
                var data = new Color[spacing];
                for (var y = 0; y < spacing; y++)
                    data[y] = y == spacing - 1 ? new Color(0, 0, 0, 255) : Color.Transparent;

                var texture = new Texture2D(device, 1, spacing);
                texture.SetData(data);
                return texture;
            });
        }

        private Texture2D GetDotMatrixTexture(DotMatrixPostEffect dotMatrix)
        {
            var cell = Math.Max(2, (int)Math.Round(dotMatrix.CellSize));
            return BrushTextureCache.GetOrCreate(_graphicsDevice, new BrushTextureCacheKey("postfx-dotmatrix", cell), device =>
            {
                var data = new Color[cell * cell];
                for (var y = 0; y < cell; y++)
                {
                    for (var x = 0; x < cell; x++)
                        data[y * cell + x] = x == cell - 1 || y == cell - 1 ? new Color(0, 0, 0, 255) : Color.Transparent;
                }

                var texture = new Texture2D(device, cell, cell);
                texture.SetData(data);
                return texture;
            });
        }

        private Texture2D GetVignetteTexture()
        {
            const int size = 256;
            return BrushTextureCache.GetOrCreate(_graphicsDevice, new BrushTextureCacheKey("postfx-vignette", size), device =>
            {
                var data = new Color[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x / (float)(size - 1) * 2 - 1;
                        var dy = y / (float)(size - 1) * 2 - 1;
                        var distance = (float)Math.Sqrt(dx * dx + dy * dy) / 1.41421356f;
                        var falloff = MathHelper.Clamp((distance - 0.45f) / 0.55f, 0, 1);
                        data[y * size + x] = new Color((byte)0, (byte)0, (byte)0, (byte)(falloff * falloff * 255));
                    }
                }

                var texture = new Texture2D(device, size, size);
                texture.SetData(data);
                return texture;
            });
        }

        private Texture2D GetGrainTexture()
        {
            const int size = 128;
            return BrushTextureCache.GetOrCreate(_graphicsDevice, new BrushTextureCacheKey("postfx-grain", size), device =>
            {
                var random = new Random(902713);
                var data = new Color[size * size];
                for (var i = 0; i < data.Length; i++)
                {
                    var bright = random.Next(2) == 0;
                    var alpha = (byte)random.Next(160, 256);
                    data[i] = bright ? new Color(alpha, alpha, alpha, alpha) : new Color((byte)0, (byte)0, (byte)0, alpha);
                }

                var texture = new Texture2D(device, size, size);
                texture.SetData(data);
                return texture;
            });
        }
    }
}

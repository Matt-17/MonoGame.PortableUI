using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    // Control.Deformation: the control's rest pose rendered into a texture, drawn on a deformed grid.
    public abstract partial class Screen
    {
        private static readonly RasterizerState MeshRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };

        /// <summary>The control whose rest pose is being rendered for its mesh (drawn normally inside).</summary>
        private Control? _deformInProgress;

        private VertexPositionColorTexture[] _meshVertices = Array.Empty<VertexPositionColorTexture>();
        private short[] _meshIndices = Array.Empty<short>();
        private int _meshIndexColumns;
        private int _meshIndexRows;

        /// <summary>
        ///     Renders <paramref name="control"/> without its own transform into a rented texture, then draws that
        ///     texture on the deformation grid, moved by the control's transform (rotation and skew included).
        ///     Without <paramref name="deforms"/> the grid is one undeformed quad. False: draw it normally.
        /// </summary>
        private bool TryDrawDeformed(SpriteBatch spriteBatch, Control control, RenderContext parentContext, bool deforms)
        {
            var engine = ScreenEngine;
            var deformation = deforms ? control.Deformation : null;
            if (engine == null)
                return false;

            var device = spriteBatch.GraphicsDevice;
            int left, top, width, height;
            Rect layoutBox;
            RenderTarget2D texture;
            control.RenderTransformSuppressed = true;
            try
            {
                var restContext = parentContext.ForControl(control);
                if (!TryMeasureLayer(control, restContext, out var bounds, out var uncacheable) || uncacheable)
                    return false;
                // One transparent pixel around the ink: filtering against it keeps the deformed edges soft.
                left = (int)Math.Floor(bounds.Left) - 1;
                top = (int)Math.Floor(bounds.Top) - 1;
                width = (int)Math.Ceiling(bounds.Right) + 1 - left;
                height = (int)Math.Ceiling(bounds.Bottom) + 1 - top;
                if (width <= 0 || height <= 0 || width > MaxLayerSize || height > MaxLayerSize)
                    return false;

                FlushLayerComposites(spriteBatch);
                texture = RenderTargetPool.Rent(device, width, height);
                RenderRestPose(spriteBatch, control, parentContext, texture, left, top);
                layoutBox = control.ClippingRect;
            }
            finally
            {
                control.RenderTransformSuppressed = false;
            }

            if (Math.Abs(parentContext.Transform.Determinant()) < 1e-8f)
            {
                RenderTargetPool.Return(device, texture);
                return false;
            }

            var toLayout = Matrix.Invert(parentContext.Transform);
            var toScreen = control.GetRenderTransform() * parentContext.Transform;
            var columns = deformation?.Columns ?? 1;
            var rows = deformation?.Rows ?? 1;
            EnsureMesh(columns, rows);
            var size = new Vector2(Math.Max(1, layoutBox.Width), Math.Max(1, layoutBox.Height));
            var time = ScreenSystem.TotalTime.TotalSeconds;
            var color = Color.White * parentContext.Opacity;
            var stride = columns + 1;
            for (var row = 0; row <= rows; row++)
            {
                for (var column = 0; column <= columns; column++)
                {
                    var uv = new Vector2(column / (float)columns, row / (float)rows);
                    // Texture point -> rest layout point -> deformed -> moved by the control's own transform.
                    var layout = Vector2.Transform(new Vector2(left + uv.X * width, top + uv.Y * height), toLayout);
                    var u = (layout.X - layoutBox.Left) / size.X;
                    var v = (layout.Y - layoutBox.Top) / size.Y;
                    var deformed = deformation == null ? layout : layout + deformation.OffsetAt(u, v, size, time);
                    var screen = Vector2.Transform(deformed, toScreen);
                    _meshVertices[row * stride + column] = new VertexPositionColorTexture(new Vector3(screen, 0), color, uv);
                }
            }

            var effect = MeshEffect(device);
            effect.Texture = texture;
            var viewport = device.Viewport;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            var oldScissor = device.ScissorRectangle;
            device.ScissorRectangle = ToScissorRectangle(parentContext.ChildClipRect);
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = MeshRasterizer;
            device.SamplerStates[0] = SamplerState.LinearClamp;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _meshVertices, 0, (columns + 1) * (rows + 1), _meshIndices, 0, columns * rows * 2);
            device.ScissorRectangle = oldScissor;
            RenderTargetPool.Return(device, texture);
            engine.RecordBatchFlush();
            return true;
        }

        private void RenderRestPose(SpriteBatch spriteBatch, Control control, RenderContext parentContext, RenderTarget2D texture, int left, int top)
        {
            var device = spriteBatch.GraphicsDevice;
            if (_layerTargetBuffers.Count <= _layerDepth)
                _layerTargetBuffers.Add(null);
            var buffer = _layerTargetBuffers[_layerDepth];
            var previousTargets = RenderTargetHelper.SnapshotRenderTargets(device, ref buffer);
            _layerTargetBuffers[_layerDepth] = buffer;
            _layerDepth++;
            var oldScissor = device.ScissorRectangle;
            var outer = _deformInProgress;
            _deformInProgress = control;
            try
            {
                device.SetRenderTarget(texture);
                device.Clear(Color.Transparent);
                device.ScissorRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                DrawControlBatched(spriteBatch, control, parentContext.ForLayer(-left, -top, texture.Width, texture.Height));
                FlushLayerComposites(spriteBatch);
            }
            finally
            {
                _layerDepth--;
                _deformInProgress = outer;
                if (previousTargets.Length == 0)
                    device.SetRenderTarget(null);
                else
                    device.SetRenderTargets(previousTargets);
                device.ScissorRectangle = oldScissor;
            }
        }

        // Two triangles per grid cell; rebuilt only when the grid size changes.
        private void EnsureMesh(int columns, int rows)
        {
            var vertices = (columns + 1) * (rows + 1);
            if (_meshVertices.Length < vertices)
                _meshVertices = new VertexPositionColorTexture[vertices];
            if (_meshIndexColumns == columns && _meshIndexRows == rows)
                return;

            _meshIndices = new short[columns * rows * 6];
            var i = 0;
            var stride = columns + 1;
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var a = (short)(row * stride + column);
                    var b = (short)(a + 1);
                    var c = (short)(a + stride);
                    var d = (short)(c + 1);
                    _meshIndices[i++] = a;
                    _meshIndices[i++] = b;
                    _meshIndices[i++] = c;
                    _meshIndices[i++] = b;
                    _meshIndices[i++] = d;
                    _meshIndices[i++] = c;
                }
            }

            _meshIndexColumns = columns;
            _meshIndexRows = rows;
        }

        private static readonly System.Collections.Generic.Dictionary<GraphicsDevice, BasicEffect> MeshEffects = new System.Collections.Generic.Dictionary<GraphicsDevice, BasicEffect>();

        private static BasicEffect MeshEffect(GraphicsDevice device)
        {
            lock (MeshEffects)
            {
                if (MeshEffects.TryGetValue(device, out var effect) && !effect.IsDisposed)
                    return effect;
            }

            return CreateMeshEffect(device);
        }

        // Apart from MeshEffect: the dispose handler's closure would otherwise be allocated on every call.
        private static BasicEffect CreateMeshEffect(GraphicsDevice device)
        {
            lock (MeshEffects)
            {
                var effect = new BasicEffect(device)
                {
                    TextureEnabled = true,
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    World = Matrix.Identity,
                    View = Matrix.Identity
                };
                MeshEffects[device] = effect;
                device.Disposing += (_, _) =>
                {
                    lock (MeshEffects)
                    {
                        if (MeshEffects.Remove(device, out var disposed))
                            disposed.Dispose();
                    }
                };
                return effect;
            }
        }
    }
}

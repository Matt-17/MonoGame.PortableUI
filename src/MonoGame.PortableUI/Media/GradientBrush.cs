using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI.Media
{
    public class GradientBrush : Brush
    {
        private Color _startColor;
        private Color _endColor;
        private GradientDirection _direction;

        public GradientBrush(Color startColor, Color endColor)
            : this(startColor, endColor, GradientDirection.Vertical)
        {
        }

        public GradientBrush(Color startColor, Color endColor, GradientDirection direction)
        {
            _startColor = startColor;
            _endColor = endColor;
            _direction = direction;
        }

        public Color StartColor
        {
            get { return _startColor; }
            set
            {
                _startColor = value;
            }
        }

        public Color EndColor
        {
            get { return _endColor; }
            set
            {
                _endColor = value;
            }
        }

        public GradientDirection Direction
        {
            get { return _direction; }
            set
            {
                _direction = value;
            }
        }

        internal BrushTextureCacheKey CreateTextureCacheKey()
        {
            return new BrushTextureCacheKey(
                "linear-gradient-v1",
                unchecked((int)StartColor.PackedValue),
                unchecked((int)EndColor.PackedValue),
                (int)Direction);
        }

        private Texture2D GetTexture(SpriteBatch spriteBatch)
        {
            return BrushTextureCache.GetOrCreate(spriteBatch.GraphicsDevice, CreateTextureCacheKey(), graphicsDevice =>
            {
                var start = Premultiply(StartColor);
                var mid = Premultiply(MidColor);
                var end = Premultiply(EndColor);
                Texture2D texture;
            switch (Direction)
            {
                case GradientDirection.Horizontal:
                    texture = new Texture2D(graphicsDevice, 2, 1);
                    texture.SetData(new[] { start, end });
                    break;
                case GradientDirection.DiagonalDown:
                    texture = new Texture2D(graphicsDevice, 2, 2);
                    texture.SetData(new[] { start, mid, mid, end });
                    break;
                case GradientDirection.DiagonalUp:
                    texture = new Texture2D(graphicsDevice, 2, 2);
                    texture.SetData(new[] { mid, end, start, mid });
                    break;
                default:
                    texture = new Texture2D(graphicsDevice, 1, 2);
                    texture.SetData(new[] { start, end });
                    break;
            }
                return texture;
            });
        }

        private Color MidColor => Color.Lerp(StartColor, EndColor, 0.5f);

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            spriteBatch.Draw(GetTexture(spriteBatch), rect, Color.White);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            spriteBatch.Draw(GetTexture(spriteBatch), rect, ApplyOpacity(Color.White, opacity));
        }

        public override void Draw(SpriteBatch spriteBatch, in BrushContext context)
        {
            if (context.Rect.Width <= 0 || context.Rect.Height <= 0)
                return;

            if (context.Radius.IsEmpty)
            {
                Draw(spriteBatch, context.Rect, context.Opacity);
                return;
            }

            var width = Math.Max(1, (int)Math.Ceiling(context.Rect.Width));
            var height = Math.Max(1, (int)Math.Ceiling(context.Rect.Height));
            var radius = context.Radius;
            var key = new BrushTextureCacheKey(
                "linear-gradient-rounded-v1",
                unchecked((int)StartColor.PackedValue),
                unchecked((int)EndColor.PackedValue),
                System.HashCode.Combine((int)Direction, width, height),
                radius.GetHashCode());
            var texture = BrushTextureCache.GetOrCreate(spriteBatch.GraphicsDevice, key, graphicsDevice =>
            {
                var data = new Color[width * height];
                for (var y = 0; y < height; y++)
                {
                    var v = height == 1 ? 0 : y / (float)(height - 1);
                    for (var x = 0; x < width; x++)
                    {
                        var u = width == 1 ? 0 : x / (float)(width - 1);
                        var t = Direction switch
                        {
                            GradientDirection.Horizontal => u,
                            GradientDirection.DiagonalDown => (u + v) / 2,
                            GradientDirection.DiagonalUp => (u + (1 - v)) / 2,
                            _ => v
                        };
                        data[y * width + x] = Premultiply(Color.Lerp(StartColor, EndColor, t));
                    }
                }

                RoundedRectRenderer.ApplyCornerMask(data, width, height, radius);
                var rounded = new Texture2D(graphicsDevice, width, height);
                rounded.SetData(data);
                return rounded;
            });
            spriteBatch.Draw(texture, context.Rect, ApplyOpacity(Color.White, context.Opacity));
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI.Media
{
    public class FrostedGlassBrush : Brush
    {
        private const int TextureSize = 32;
        private Color _tintColor;
        private Color _sheenColor;
        private float _blurRadius;
        private float _grainOpacity;

        public FrostedGlassBrush()
            : this(new Color(255, 255, 255, 92), new Color(255, 255, 255, 160), 8, 0.12f)
        {
        }

        public FrostedGlassBrush(Color tintColor)
            : this(tintColor, new Color(255, 255, 255, 160), 8, 0.12f)
        {
        }

        public FrostedGlassBrush(Color tintColor, Color sheenColor, float blurRadius = 8, float grainOpacity = 0.12f)
        {
            _tintColor = tintColor;
            _sheenColor = sheenColor;
            BlurRadius = blurRadius;
            GrainOpacity = grainOpacity;
        }

        public override bool RequiresBackdrop => BlurRadius > 0;

        public Color TintColor
        {
            get { return _tintColor; }
            set { _tintColor = value; }
        }

        public Color SheenColor
        {
            get { return _sheenColor; }
            set
            {
                if (_sheenColor == value)
                    return;

                _sheenColor = value;
            }
        }

        public float BlurRadius
        {
            get { return _blurRadius; }
            set
            {
                var clamped = MathHelper.Clamp(value, 0, 24);
                if (Math.Abs(_blurRadius - clamped) < float.Epsilon)
                    return;

                _blurRadius = clamped;
            }
        }

        public float GrainOpacity
        {
            get { return _grainOpacity; }
            set
            {
                var clamped = MathHelper.Clamp(value, 0, 1);
                if (Math.Abs(_grainOpacity - clamped) < float.Epsilon)
                    return;

                _grainOpacity = clamped;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect)
        {
            Draw(spriteBatch, rect, 1);
        }

        public override void Draw(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            opacity = MathHelper.Clamp(opacity, 0, 1);
            if (BlurRadius > 0 && TryDrawBackdrop(spriteBatch, rect, opacity))
            {
                spriteBatch.Draw(SolidColorBrush.Pixel, rect, ApplyOpacity(TintColor, opacity));
                spriteBatch.Draw(GetTexture(spriteBatch), rect, ApplyOpacity(Color.White, opacity * 0.55f));
                DrawHighlights(spriteBatch, rect, opacity);
                return;
            }

            spriteBatch.Draw(SolidColorBrush.Pixel, rect, ApplyOpacity(TintColor, opacity));

            spriteBatch.Draw(GetTexture(spriteBatch), rect, ApplyOpacity(Color.White, opacity));

            DrawHighlights(spriteBatch, rect, opacity);
        }

        private static bool TryDrawBackdrop(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            if (!BackdropSource.TryGet(spriteBatch.GraphicsDevice, out var backdrop, out var screenRect) || backdrop == null)
                return false;

            var scaleX = backdrop.Width / Math.Max(1f, screenRect.Width);
            var scaleY = backdrop.Height / Math.Max(1f, screenRect.Height);
            var source = new Rectangle(
                (int)((rect.Left - screenRect.Left) * scaleX),
                (int)((rect.Top - screenRect.Top) * scaleY),
                Math.Max(1, (int)Math.Ceiling(rect.Width * scaleX)),
                Math.Max(1, (int)Math.Ceiling(rect.Height * scaleY)));
            source = Rectangle.Intersect(source, backdrop.Bounds);
            if (source.Width <= 0 || source.Height <= 0)
                return false;

            spriteBatch.Draw(backdrop, rect, source, Color.White * opacity);
            return true;
        }

        internal BrushTextureCacheKey CreateTextureCacheKey()
        {
            return new BrushTextureCacheKey(
                "frosted-glass-v1",
                unchecked((int)SheenColor.PackedValue),
                BitConverter.SingleToInt32Bits(BlurRadius),
                BitConverter.SingleToInt32Bits(GrainOpacity));
        }

        private Texture2D GetTexture(SpriteBatch spriteBatch)
        {
            return BrushTextureCache.GetOrCreate(spriteBatch.GraphicsDevice, CreateTextureCacheKey(), graphicsDevice =>
            {
            var noise = new float[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var first = Noise(x, y);
                    var second = Noise(x * 3 + 17, y * 3 + 29);
                    noise[y * TextureSize + x] = first * 0.65f + second * 0.35f;
                }
            }

            var blurRadius = Math.Max(1, (int)Math.Round(BlurRadius / 3f));
            var data = new Color[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var blurredNoise = BoxBlur(noise, x, y, blurRadius);
                    var frost = Math.Abs(blurredNoise - 0.5f) * 2;
                    var diagonal = MathHelper.Clamp(1 - Math.Abs((x + y) / (float)(TextureSize * 2) - 0.42f) * 4.5f, 0, 1);
                    var radial = RadialFalloff(x, y);
                    var alpha = (frost * GrainOpacity * 34) + (diagonal * 12) + (radial * 8);
                    alpha = Math.Min(alpha, Math.Min((int)SheenColor.A, 42));
                    data[y * TextureSize + x] = Color.FromNonPremultiplied(SheenColor.R, SheenColor.G, SheenColor.B, (int)alpha);
                }
            }

                var texture = new Texture2D(graphicsDevice, TextureSize, TextureSize);
                texture.SetData(data);
                return texture;
            });
        }

        private void DrawHighlights(SpriteBatch spriteBatch, Rect rect, float opacity)
        {
            var topHeight = Math.Min(2, Math.Max(1, rect.Height * 0.04f));
            var leftWidth = Math.Min(2, Math.Max(1, rect.Width * 0.02f));
            var sheenAlpha = (int)SheenColor.A;
            var topColor = WithAlpha(SheenColor, Math.Min(sheenAlpha, 118));
            var leftColor = WithAlpha(SheenColor, Math.Min(sheenAlpha, 62));
            var washColor = WithAlpha(SheenColor, Math.Min(sheenAlpha, 14));
            var shadeColor = WithAlpha(Color.Black, Math.Min((int)TintColor.A, 42));

            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Top, rect.Width, topHeight), ApplyOpacity(topColor, opacity));
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Top, leftWidth, rect.Height), ApplyOpacity(leftColor, opacity));
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Top, rect.Width, Math.Min(rect.Height * 0.28f, 32)), ApplyOpacity(washColor, opacity));
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Left, rect.Bottom - 1, rect.Width, 1), ApplyOpacity(shadeColor, opacity));
            spriteBatch.Draw(SolidColorBrush.Pixel, new Rect(rect.Right - 1, rect.Top, 1, rect.Height), ApplyOpacity(shadeColor, opacity));
        }

        private static Color WithAlpha(Color color, int alpha)
        {
            return new Color(color.R, color.G, color.B, (byte)MathHelper.Clamp(alpha, 0, 255));
        }

        private static float BoxBlur(float[] source, int centerX, int centerY, int radius)
        {
            var sum = 0f;
            var count = 0;

            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                var sampleY = Wrap(y);
                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    var sampleX = Wrap(x);
                    sum += source[sampleY * TextureSize + sampleX];
                    count++;
                }
            }

            return sum / count;
        }

        private static int Wrap(int value)
        {
            value %= TextureSize;
            return value < 0 ? value + TextureSize : value;
        }

        private static float RadialFalloff(int x, int y)
        {
            var dx = x / (float)(TextureSize - 1) - 0.22f;
            var dy = y / (float)(TextureSize - 1) - 0.18f;
            var distanceSquared = dx * dx + dy * dy;
            return MathHelper.Clamp(1 - distanceSquared * 7.5f, 0, 1);
        }

        private static float Noise(int x, int y)
        {
            unchecked
            {
                var n = x * 374761393 + y * 668265263;
                n = (n ^ (n >> 13)) * 1274126177;
                n ^= n >> 16;
                return (n & int.MaxValue) / (float)int.MaxValue;
            }
        }
    }
}

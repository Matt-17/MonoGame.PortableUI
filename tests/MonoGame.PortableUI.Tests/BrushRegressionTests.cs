using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class BrushRegressionTests
    {
        [TestMethod]
        public void Gradient_brush_defaults_to_vertical_direction()
        {
            var brush = new GradientBrush(Color.Red, Color.Blue);

            Assert.AreEqual(Color.Red, brush.StartColor);
            Assert.AreEqual(Color.Blue, brush.EndColor);
            Assert.AreEqual(GradientDirection.Vertical, brush.Direction);
        }

        [TestMethod]
        public void Gradient_brush_direction_can_be_configured()
        {
            var brush = new GradientBrush(Color.Red, Color.Blue, GradientDirection.DiagonalUp);

            Assert.AreEqual(GradientDirection.DiagonalUp, brush.Direction);

            brush.Direction = GradientDirection.Horizontal;

            Assert.AreEqual(GradientDirection.Horizontal, brush.Direction);
        }

        [TestMethod]
        public void Gradient_brush_texture_cache_key_tracks_parameters()
        {
            var brush = new GradientBrush(Color.Red, Color.Blue, GradientDirection.DiagonalUp);
            var equivalent = new GradientBrush(Color.Red, Color.Blue, GradientDirection.DiagonalUp);
            var different = new GradientBrush(Color.Red, Color.Green, GradientDirection.DiagonalUp);

            Assert.AreEqual(brush.CreateTextureCacheKey(), equivalent.CreateTextureCacheKey());
            Assert.AreNotEqual(brush.CreateTextureCacheKey(), different.CreateTextureCacheKey());
        }

        [TestMethod]
        public void Linear_gradient_brush_v2_supports_multi_stop_angle_cache_keys()
        {
            var brush = new LinearGradientBrush(
                new GradientStop(0, Color.Red),
                new GradientStop(0.5f, Color.Green),
                new GradientStop(1, Color.Blue))
            {
                AngleDegrees = 32
            };
            var equivalent = new LinearGradientBrush(
                new GradientStop(1, Color.Blue),
                new GradientStop(0, Color.Red),
                new GradientStop(0.5f, Color.Green))
            {
                AngleDegrees = 32
            };
            var different = new LinearGradientBrush(new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue))
            {
                AngleDegrees = 90
            };

            Assert.AreEqual(brush.CreateTextureCacheKey(120, 30), equivalent.CreateTextureCacheKey(120, 30));
            Assert.AreNotEqual(brush.CreateTextureCacheKey(120, 30), different.CreateTextureCacheKey(120, 30));
        }

        [TestMethod]
        public void Radial_gradient_brush_tracks_center_radius_and_stops()
        {
            var brush = new RadialGradientBrush(new GradientStop(0, Color.White), new GradientStop(1, Color.Black))
            {
                Center = new PointF(0.25f, 0.75f),
                RadiusX = 0.4f,
                RadiusY = 0.6f
            };
            var different = new RadialGradientBrush(new GradientStop(0, Color.White), new GradientStop(1, Color.Black))
            {
                Center = new PointF(0.5f, 0.75f),
                RadiusX = 0.4f,
                RadiusY = 0.6f
            };

            Assert.AreNotEqual(brush.CreateTextureCacheKey(), different.CreateTextureCacheKey());
        }

        [TestMethod]
        public void Frosted_glass_brush_defaults_to_translucent_tint()
        {
            var brush = new FrostedGlassBrush();

            Assert.IsTrue(brush.TintColor.A < byte.MaxValue);
            Assert.IsTrue(brush.SheenColor.A < byte.MaxValue);
            Assert.IsTrue(brush.BlurRadius > 0);
            Assert.IsTrue(brush.GrainOpacity > 0);
        }

        [TestMethod]
        public void Frosted_glass_brush_clamps_blur_and_grain()
        {
            var brush = new FrostedGlassBrush(Color.White, Color.White, -4, 2);

            Assert.AreEqual(0f, brush.BlurRadius, 0.001f);
            Assert.AreEqual(1f, brush.GrainOpacity, 0.001f);
            Assert.IsFalse(brush.RequiresBackdrop);

            brush.BlurRadius = 40;
            brush.GrainOpacity = -0.5f;

            Assert.AreEqual(24f, brush.BlurRadius, 0.001f);
            Assert.AreEqual(0f, brush.GrainOpacity, 0.001f);
            Assert.IsTrue(brush.RequiresBackdrop);
        }

        [TestMethod]
        public void Acrylic_and_liquid_glass_expose_backdrop_material_settings()
        {
            var acrylic = new AcrylicBrush();
            var liquid = new LiquidGlassBrush();

            Assert.IsTrue(acrylic.RequiresBackdrop);
            Assert.IsTrue(liquid.RequiresBackdrop);
            Assert.IsTrue(liquid.EdgeRefractionStrength > 0);
            Assert.IsTrue(liquid.SpecularSweepStrength > 0);
            Assert.AreEqual(CornerStyle.Squircle, liquid.CornerStyle);
            Assert.IsTrue(liquid.SaturationBoost > acrylic.SaturationBoost);
        }

        [TestMethod]
        public void Frosted_glass_texture_cache_key_tracks_generated_texture_parameters()
        {
            var brush = new FrostedGlassBrush(Color.Red, Color.White, 8, 0.2f);
            var sameTexture = new FrostedGlassBrush(Color.Blue, Color.White, 8, 0.2f);
            var differentTexture = new FrostedGlassBrush(Color.Red, Color.Yellow, 8, 0.2f);

            Assert.AreEqual(brush.CreateTextureCacheKey(), sameTexture.CreateTextureCacheKey());
            Assert.AreNotEqual(brush.CreateTextureCacheKey(), differentTexture.CreateTextureCacheKey());
        }

        [TestMethod]
        public void Tile_brush_defaults_are_safe_for_unassigned_source()
        {
            var brush = new TileBrush();

            Assert.IsNull(brush.Source);
            Assert.AreEqual(1, brush.Scale);
            Assert.AreEqual(Color.White, brush.TintColor);
        }

        [TestMethod]
        public void Tile_brush_calculates_partial_edge_tiles()
        {
            var tiles = TileBrush.GetTileRects(new Rect(0, 0, 25, 15), 10, 10, 1).ToArray();

            Assert.AreEqual(6, tiles.Length);
            Assert.AreEqual(new Rect(0, 0, 10, 10), tiles[0]);
            Assert.AreEqual(new Rect(10, 0, 10, 10), tiles[1]);
            Assert.AreEqual(new Rect(20, 0, 5, 10), tiles[2]);
            Assert.AreEqual(new Rect(0, 10, 10, 5), tiles[3]);
            Assert.AreEqual(new Rect(10, 10, 10, 5), tiles[4]);
            Assert.AreEqual(new Rect(20, 10, 5, 5), tiles[5]);
        }

        [TestMethod]
        public void Tile_brush_clips_source_rectangles_for_partial_edge_tiles()
        {
            var tiles = TileBrush.GetTileSegments(new Rect(0, 0, 25, 15), 10, 10, 1).ToArray();

            Assert.AreEqual(new Rectangle(0, 0, 10, 10), tiles[0].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 5, 10), tiles[2].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 10, 5), tiles[3].SourceRectangle);
            Assert.AreEqual(new Rectangle(0, 0, 5, 5), tiles[5].SourceRectangle);
        }

        [TestMethod]
        public void Tile_brush_scale_changes_tile_size()
        {
            var tiles = TileBrush.GetTileRects(new Rect(0, 0, 30, 12), 10, 10, 0.5f).ToArray();

            Assert.AreEqual(new Rect(0, 0, 5, 5), tiles[0]);
            Assert.AreEqual(new Rect(25, 10, 5, 2), tiles[17]);
        }

        [TestMethod]
        public void Nine_tile_brush_defaults_to_center_stretch()
        {
            var brush = new NineTileBrush();
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 30, 20), 12, 8, brush.SliceMargins).ToArray();

            Assert.IsNull(brush.Source);
            Assert.AreEqual(Color.White, brush.TintColor);
            Assert.AreEqual(1, segments.Length);
            Assert.AreEqual(new Rectangle(0, 0, 12, 8), segments[0].SourceRectangle);
            Assert.AreEqual(new Rect(0, 0, 30, 20), segments[0].DestinationRect);
        }

        [TestMethod]
        public void Nine_tile_brush_calculates_stretched_center_segment()
        {
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 30, 20), 20, 10, new Thickness(4, 2, 6, 3)).ToArray();

            Assert.AreEqual(9, segments.Length);
            Assert.AreEqual(new Rectangle(0, 0, 4, 2), segments[0].SourceRectangle);
            Assert.AreEqual(new Rect(0, 0, 4, 2), segments[0].DestinationRect);
            Assert.AreEqual(new Rectangle(4, 2, 10, 5), segments[4].SourceRectangle);
            Assert.AreEqual(new Rect(4, 2, 20, 15), segments[4].DestinationRect);
        }

        [TestMethod]
        public void Nine_tile_brush_clamps_edge_slices_for_small_destinations()
        {
            var segments = NineTileBrush.GetSegments(new Rect(0, 0, 5, 4), 20, 10, new Thickness(4, 2, 6, 3)).ToArray();

            Assert.AreEqual(4, segments.Length);
            AssertRect(new Rect(0, 0, 2, 1.6f), segments[0].DestinationRect);
            AssertRect(new Rect(2, 0, 3, 1.6f), segments[1].DestinationRect);
            AssertRect(new Rect(0, 1.6f, 2, 2.4f), segments[2].DestinationRect);
            AssertRect(new Rect(2, 1.6f, 3, 2.4f), segments[3].DestinationRect);
        }

        [TestMethod]
        public void Image_brush_uses_stretch_math_for_background_images()
        {
            var brush = new ImageBrush { Stretch = Stretch.Uniform };

            AssertRect(new Rect(50, 0, 100, 100), brush.GetStretchedRect(new Rect(0, 0, 200, 100), 100, 100));

            brush.Stretch = Stretch.UniformToFill;

            AssertRect(new Rect(0, -50, 200, 200), brush.GetStretchedRect(new Rect(0, 0, 200, 100), 100, 100));

            brush.Stretch = Stretch.None;

            AssertRect(new Rect(50, 25, 100, 50), brush.GetStretchedRect(new Rect(0, 0, 200, 100), 100, 50));
        }

        [TestMethod]
        public void Brush_opacity_multiplies_alpha_and_premultiplies()
        {
            var color = Brush.ApplyOpacity(new Color(10, 20, 30, 200), 0.5f);

            // SpriteBatch's default AlphaBlend expects premultiplied colors.
            Assert.AreEqual(Color.FromNonPremultiplied(10, 20, 30, 100), color);
            Assert.AreEqual(100, color.A);
        }

        [TestMethod]
        public void Rounded_rect_fallback_creates_body_and_side_fill_rects()
        {
            var rects = RoundedRectRenderer.GetFillRects(new Rect(0, 0, 100, 40), new CornerRadius(8, 12, 6, 4)).ToArray();

            Assert.AreEqual(3, rects.Length);
            AssertRect(new Rect(8, 0, 80, 40), rects[0]);
            AssertRect(new Rect(0, 12, 8, 22), rects[1]);
            AssertRect(new Rect(88, 12, 12, 22), rects[2]);
        }

        [TestMethod]
        public void Shadow_renderer_creates_single_hard_shadow_when_blur_is_zero()
        {
            var shadow = new ShadowStyle
            {
                Color = new Color(0, 0, 0, 120),
                Offset = new Vector2(3, 4),
                Blur = 0,
                Spread = 2
            };

            var layers = ShadowRenderer.GetShadowLayers(new Rect(10, 20, 30, 40), shadow).ToArray();

            Assert.AreEqual(1, layers.Length);
            Assert.AreEqual(new Rect(11, 22, 34, 44), layers[0].Rect);
            Assert.AreEqual(new Color(0, 0, 0, 120), layers[0].Color);
        }

        [TestMethod]
        public void Shadow_renderer_layers_soft_shadow_and_supports_inset_geometry()
        {
            var shadow = new ShadowStyle
            {
                Color = new Color(0, 0, 0, 120),
                Blur = 6,
                Spread = 1
            };

            var layers = ShadowRenderer.GetShadowLayers(new Rect(0, 0, 20, 20), shadow).ToArray();

            Assert.IsTrue(layers.Length > 1);
            Assert.IsTrue(layers[0].Color.A > layers[^1].Color.A);

            shadow.Inset = true;
            shadow.Blur = 0;
            shadow.Spread = 2;

            var inset = ShadowRenderer.GetShadowLayers(new Rect(0, 0, 20, 20), shadow).Single();

            Assert.AreEqual(new Rect(2, 2, 16, 16), inset.Rect);
        }

        private static void AssertRect(Rect expected, Rect actual)
        {
            Assert.AreEqual(expected.Left, actual.Left, 0.001f);
            Assert.AreEqual(expected.Top, actual.Top, 0.001f);
            Assert.AreEqual(expected.Width, actual.Width, 0.001f);
            Assert.AreEqual(expected.Height, actual.Height, 0.001f);
        }
    }
}

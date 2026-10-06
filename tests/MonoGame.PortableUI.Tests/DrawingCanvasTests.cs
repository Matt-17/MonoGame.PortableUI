using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class DrawingCanvasTests
    {
        private sealed class Canvas : DrawingCanvas
        {
            public Action<DrawingContext>? Draw;
            protected override void OnRender(DrawingContext context) => Draw?.Invoke(context);
        }

        private sealed class BoxFont : MonoGame.PortableUI.Text.UIFont
        {
            public override float DefaultSize => 10;
            public override float GetLineHeight(float pixelSize) => pixelSize;
            public override Vector2 MeasureString(string text, float pixelSize) => new Vector2(text.Length * pixelSize / 2, pixelSize);
            public override Vector2 MeasureString(System.Text.StringBuilder text, float pixelSize) => new Vector2(text.Length * pixelSize / 2, pixelSize);
            public override void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
            public override void DrawString(SpriteBatch spriteBatch, System.Text.StringBuilder text, Vector2 position, Color color, float pixelSize, Vector2 scale) { }
        }

        private static DrawingContext Record(Action<DrawingContext> draw, Rect? rect = null, float opacity = 1f, float scale = 1f)
        {
            var canvas = new Canvas { Draw = draw };
            canvas.SetRenderState(opacity, new Vector2(scale, scale));
            return canvas.RenderInto(null, rect ?? new Rect(10, 20, 200, 100));
        }

        private static (float Min, float Max) Extent(DrawingContext context, Func<Vector3, float> axis, bool opaqueOnly)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in context.PendingVertices)
            {
                if (opaqueOnly && v.Color.A == 0)
                    continue;
                var value = axis(v.Position);
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }
            return (min, max);
        }

        [TestMethod]
        public void Line_is_a_band_of_its_thickness_offset_to_the_canvas_origin_with_a_soft_fringe()
        {
            var context = Record(dc => dc.DrawLine(new Vector2(0, 50), new Vector2(100, 50), Color.Red, 4));

            Assert.AreEqual(0, context.PendingVertices.Length % 3);
            var core = Extent(context, p => p.Y, true);
            Assert.AreEqual(70 - 1.5f, core.Min, 0.01f);
            Assert.AreEqual(70 + 1.5f, core.Max, 0.01f);
            var all = Extent(context, p => p.Y, false);
            Assert.AreEqual(70 - 2.5f, all.Min, 0.01f);
            Assert.AreEqual(70 + 2.5f, all.Max, 0.01f);
            var x = Extent(context, p => p.X, true);
            Assert.AreEqual(10, x.Min, 0.01f);
            Assert.AreEqual(110, x.Max, 0.01f);
        }

        [TestMethod]
        public void Square_cap_extends_the_line_by_half_its_thickness()
        {
            var context = Record(dc => dc.DrawLine(new Vector2(0, 0), new Vector2(100, 0), Color.Red, 10, LineCap.Square));
            var x = Extent(context, p => p.X, true);
            Assert.AreEqual(10 - 5, x.Min, 0.01f);
            Assert.AreEqual(110 + 5, x.Max, 0.01f);
        }

        [TestMethod]
        public void Arc_vertices_stay_on_the_ring_and_span_the_sweep()
        {
            var center = new Vector2(50, 50);
            var context = Record(dc => dc.DrawArc(center, 30, -MathHelper.PiOver2, MathHelper.TwoPi * 0.25f, Color.White, 6));

            var renderCenter = new Vector2(60, 70);
            foreach (var v in context.PendingVertices)
            {
                var d = Vector2.Distance(new Vector2(v.Position.X, v.Position.Y), renderCenter);
                if (v.Color.A > 0)
                    Assert.IsTrue(d >= 27.5f - 0.05f && d <= 32.5f + 0.05f, $"core vertex at {d}");
                else
                    Assert.IsTrue(d >= 26.5f - 0.6f && d <= 33.5f + 0.6f, $"fringe vertex at {d}");
            }
            // A quarter from 12 o'clock clockwise: x from the centre to the right, y from the top to the centre.
            var x = Extent(context, p => p.X, true);
            Assert.IsTrue(x.Min >= 60 - 3.1f && x.Max <= 60 + 33f);
            var y = Extent(context, p => p.Y, true);
            Assert.IsTrue(y.Min >= 70 - 33f && y.Max <= 70 + 3.1f);
        }

        [TestMethod]
        public void Opacity_and_scale_apply_and_colours_are_premultiplied()
        {
            var context = Record(dc => dc.FillRectangle(new Rect(0, 0, 10, 10), Color.White.WithAlpha(0.5f)), opacity: 0.5f, scale: 2f);

            var x = Extent(context, p => p.X, false);
            Assert.AreEqual(10 - 0.5f, x.Min, 0.01f);
            Assert.AreEqual(30 + 0.5f, x.Max, 0.01f);
            foreach (var v in context.PendingVertices)
            {
                if (v.Color.A == 0)
                    continue;
                Assert.AreEqual(64, v.Color.A, 1);
                Assert.AreEqual(v.Color.A, v.Color.R);
            }
        }

        [TestMethod]
        public void Transform_moves_and_scales_shapes_but_not_stroke_thickness()
        {
            var context = Record(dc =>
            {
                dc.Transform = Matrix.CreateScale(2) * Matrix.CreateTranslation(5, 5, 0);
                dc.DrawLine(new Vector2(0, 10), new Vector2(10, 10), Color.Red, 2);
            }, new Rect(0, 0, 100, 100));

            var y = Extent(context, p => p.Y, true);
            Assert.AreEqual(25 - 0.5f, y.Min, 0.01f);
            Assert.AreEqual(25 + 0.5f, y.Max, 0.01f);
            var x = Extent(context, p => p.X, true);
            Assert.AreEqual(5, x.Min, 0.01f);
            Assert.AreEqual(25, x.Max, 0.01f);
        }

        [TestMethod]
        public void Rotated_rectangle_corners_are_rotated_about_the_centre()
        {
            var context = Record(dc =>
            {
                dc.AntiAlias = false;
                dc.FillRectangle(new Vector2(50, 50), new Vector2(20, 10), MathHelper.PiOver2, Color.Red);
            }, new Rect(0, 0, 100, 100));

            Assert.AreEqual(6, context.PendingVertices.Length);
            var x = Extent(context, p => p.X, false);
            var y = Extent(context, p => p.Y, false);
            Assert.AreEqual(45, x.Min, 0.01f);
            Assert.AreEqual(55, x.Max, 0.01f);
            Assert.AreEqual(40, y.Min, 0.01f);
            Assert.AreEqual(60, y.Max, 0.01f);
        }

        [TestMethod]
        public void Filled_shapes_produce_whole_triangles_and_wide_pies_are_split()
        {
            var context = Record(dc =>
            {
                dc.FillCircle(new Vector2(50, 50), 20, Color.Blue);
                dc.FillPie(new Vector2(50, 50), 20, 0, MathHelper.Pi * 1.5f, Color.Green);
                dc.DrawCircle(new Vector2(50, 50), 25, Color.Black, 2);
                dc.FillPolygon(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(5, 8) }, Color.Red);
                dc.DrawPolyline(new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10) }, Color.Red, 3);
            });
            Assert.IsTrue(context.PendingVertices.Length > 100);
            Assert.AreEqual(0, context.PendingVertices.Length % 3);
            foreach (var v in context.PendingVertices)
                Assert.IsFalse(float.IsNaN(v.Position.X) || float.IsNaN(v.Position.Y));
        }

        [TestMethod]
        public void Degenerate_input_draws_nothing()
        {
            var context = Record(dc =>
            {
                dc.DrawLine(Vector2.One, Vector2.One, Color.Red, 2);
                dc.DrawArc(Vector2.Zero, 10, 0, 0, Color.Red);
                dc.FillRectangle(new Rect(0, 0, 0, 10), Color.Red);
                dc.FillCircle(Vector2.Zero, 10, Color.Transparent);
                dc.DrawPolyline(new[] { Vector2.Zero }, Color.Red);
            });
            Assert.AreEqual(0, context.PendingVertices.Length);
        }

        [TestMethod]
        public void Redrawing_the_same_picture_does_not_allocate()
        {
            Span<Vector2> plan = stackalloc Vector2[] { new Vector2(0, 0), new Vector2(40, 0), new Vector2(40, 30), new Vector2(0, 30) };
            var points = plan.ToArray();
            var canvas = new Canvas
            {
                Draw = dc =>
                {
                    dc.Transform = Matrix.CreateTranslation(3, 4, 0);
                    for (var i = 0; i < 50; i++)
                        dc.FillRectangle(new Vector2(i * 3, 20), new Vector2(8, 4), i * 0.1f, Color.Gray);
                    dc.DrawPolyline(points, Color.White, 3, closed: true);
                    dc.DrawArc(new Vector2(100, 50), 30, 0, 4f, Color.Orange, 6);
                }
            };
            canvas.RenderInto(null, new Rect(0, 0, 300, 200));
            var before = GC.GetAllocatedBytesForCurrentThread();
            canvas.RenderInto(null, new Rect(0, 0, 300, 200));
            Assert.AreEqual(before, GC.GetAllocatedBytesForCurrentThread());
        }

        [TestMethod]
        public void Render_event_runs_after_OnRender_and_text_is_a_sprite_draw()
        {
            var order = "";
            var canvas = new Canvas { Draw = _ => order += "o", Font = new BoxFont() };
            canvas.Render += (_, dc) =>
            {
                order += "e";
                dc.DrawText("A1", new Vector2(10, 10), Color.White, 12, new Vector2(0.5f, 0.5f));
            };
            var context = canvas.RenderInto(null, new Rect(0, 0, 50, 50));
            Assert.AreEqual("oe", order);
            Assert.AreEqual(1, context.SpriteDraws);
            Assert.AreEqual(50f, context.Size.Width);
            Assert.AreEqual(new Vector2(12, 12), context.MeasureText("AB", 12));
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;
using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     A control you draw on every frame: lines, arcs and rings, (rotated) rectangles, circles, polygons,
    ///     text at points and images through a <see cref="DrawingContext"/> - minimaps, floor plans,
    ///     gauges, brackets following a 3D object. Draw in <see cref="OnRender"/> (subclass) or the
    ///     <see cref="Render"/> event; coordinates are design pixels from the canvas's top-left corner.
    ///     <para>
    ///         The canvas has no size of its own (set Width/Height or stretch it). It draws only when asked:
    ///         call <see cref="Invalidate"/> whenever what it shows changes (pan, a gauge value), or the
    ///         picture stays - in <see cref="RenderMode.OnDemand"/> and inside cached layers too. Drawing
    ///         is clipped to the box unless <see cref="Control.InkOverflow"/> widens it.
    ///     </para>
    /// </summary>
    public class DrawingCanvas : Control
    {
        private readonly DrawingContext _context = new DrawingContext();
        private UIFont? _font;

        /// <summary>Raised after <see cref="OnRender"/> with the frame's drawing context.</summary>
        public event Action<DrawingCanvas, DrawingContext>? Render;

        /// <summary>Default font of <see cref="DrawingContext.DrawText"/>; null follows the theme's dynamic font,
        /// then <see cref="FontManager.DefaultDynamicFont"/>, then the default SpriteFont.</summary>
        public UIFont? Font
        {
            get => _font;
            set
            {
                _font = value;
                InvalidateLayout(false);
            }
        }

        /// <summary>Draws the canvas again on the next frame (its content changed).</summary>
        public void Invalidate() => RequestRedraw();

        /// <summary>Draw the canvas's content here. The context is valid only during the call.</summary>
        protected virtual void OnRender(DrawingContext context)
        {
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            base.OnDraw(spriteBatch, rect);
            RenderInto(spriteBatch, rect);
        }

        /// <summary>Runs the drawing code; a null batch records the geometry without a device (tests).</summary>
        internal DrawingContext RenderInto(SpriteBatch? spriteBatch, Rect rect)
        {
            _context.Begin(spriteBatch, rect, RenderScale, RenderOpacity);
            _context.SpriteFont = FontManager.DefaultFont;
            _context.DefaultDynamicFont = _font ?? ResolveTheme().Typography?.DynamicFont ?? FontManager.DefaultDynamicFont;
            try
            {
                OnRender(_context);
                Render?.Invoke(this, _context);
            }
            finally
            {
                if (spriteBatch != null)
                    _context.End();
            }
            return _context;
        }
    }
}

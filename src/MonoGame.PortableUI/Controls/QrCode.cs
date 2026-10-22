using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     A QR code of <see cref="Text"/>: dark modules on <see cref="Control.BackgroundBrush"/> (white by default), with a
    ///     light quiet zone of <see cref="QuietZone"/> modules inside the control so a camera finds its edge. Always square:
    ///     the smaller side of its box, centred; without a fixed Width or Height it measures 4 per module. Modules land on
    ///     whole device pixels where they are at least one pixel large, so the code stays crisp at any scale.
    /// </summary>
    public class QrCode : Control
    {
        private const float DefaultModuleSize = 4;

        private string _text = string.Empty;
        private QrErrorCorrection _errorCorrection = QrErrorCorrection.Medium;
        private int _quietZone = 4;
        private Color _moduleColor = Color.Black;
        private bool[,]? _modules;

        public QrCode()
        {
            IsFocusable = false;
            BackgroundBrush = new SolidColorBrush(Color.White);
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
        }

        /// <summary>The text encoded (UTF-8). Throws <see cref="ArgumentException"/> when it is too long for a QR code.</summary>
        public string Text
        {
            get { return _text; }
            set
            {
                value ??= string.Empty;
                if (_text == value)
                    return;
                var modules = QrEncoder.Encode(value, _errorCorrection);
                _text = value;
                _modules = modules;
                InvalidateLayout(true);
            }
        }

        /// <summary>How much damage the code survives (Medium by default); raised further when the size allows.</summary>
        public QrErrorCorrection ErrorCorrection
        {
            get { return _errorCorrection; }
            set
            {
                if (_errorCorrection == value)
                    return;
                _errorCorrection = value;
                _modules = null;
                InvalidateLayout(true);
            }
        }

        /// <summary>The light margin around the code, in modules (4, as the standard asks; 0 when the page around it is light).</summary>
        public int QuietZone
        {
            get { return _quietZone; }
            set
            {
                value = Math.Max(0, value);
                if (_quietZone == value)
                    return;
                _quietZone = value;
                InvalidateLayout(true);
            }
        }

        /// <summary>The colour of the dark modules.</summary>
        public Color ModuleColor
        {
            get { return _moduleColor; }
            set
            {
                if (_moduleColor == value)
                    return;
                _moduleColor = value;
                InvalidateLayout(false);
            }
        }

        /// <summary>Modules per side, the quiet zone included.</summary>
        public int ModuleCount => Modules.GetLength(0) + 2 * _quietZone;

        private bool[,] Modules => _modules ??= QrEncoder.Encode(_text, _errorCorrection);

        public override Size MeasureLayout()
        {
            if (IsGoneShown)
                return Size.Empty;

            var side = Width.IsFixed() && Height.IsFixed() ? Math.Min(Width, Height)
                : Width.IsFixed() ? Width
                : Height.IsFixed() ? Height
                : ModuleCount * DefaultModuleSize;
            return ApplyConstraints(new Size(side, side)) + Margin;
        }

        protected internal override void OnDraw(SpriteBatch spriteBatch, Rect rect)
        {
            var modules = Modules;
            var count = ModuleCount;
            // The rect is in device pixels: whole pixels per module where a module is at least one.
            var side = Math.Min(rect.Width, rect.Height);
            var module = side / count;
            if (module >= 1)
                module = MathF.Floor(module);
            var codeSide = module * count;
            var left = MathF.Round(rect.Left + (rect.Width - codeSide) / 2);
            var top = MathF.Round(rect.Top + (rect.Height - codeSide) / 2);

            DrawChrome(spriteBatch, new Rect(left, top, codeSide, codeSide));

            var color = _moduleColor * RenderOpacity;
            var size = modules.GetLength(0);
            var origin = new Vector2(left + _quietZone * module, top + _quietZone * module);
            for (var y = 0; y < size; y++)
            {
                // One rectangle per run of dark modules in a row.
                for (var x = 0; x < size;)
                {
                    if (!modules[y, x])
                    {
                        x++;
                        continue;
                    }
                    var start = x;
                    while (x < size && modules[y, x])
                        x++;
                    Primitives.Fill(spriteBatch, new Rect(origin.X + start * module, origin.Y + y * module, (x - start) * module, module), color);
                }
            }
        }
    }
}

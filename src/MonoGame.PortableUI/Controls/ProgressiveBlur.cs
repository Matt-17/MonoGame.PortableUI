using System;
using Microsoft.Xna.Framework;

namespace MonoGame.PortableUI.Controls
{
    /// <summary>
    ///     Blurs its own content along its top and bottom edge, strongest at the edge and fading to sharp at the
    ///     band's inner end - a list scrolling under a status bar or navigation bar. The content is drawn into a
    ///     texture each frame and only the two bands are blurred (a Gaussian whose radius follows the row; a core
    ///     shader, no package or install needed). At <see cref="RenderQuality.Low"/> the content is drawn sharp and
    ///     untinted. <see cref="TintColor"/>/<see cref="TintOpacity"/> tint the bands along the same gradient, or
    ///     along their own with <see cref="TopTintHeight"/>/<see cref="BottomTintHeight"/> and
    ///     <see cref="TintSolidHeight"/>.
    /// </summary>
    public class ProgressiveBlur : ContentControl
    {
        private float _topHeight;
        private float _bottomHeight;
        private float _maxRadius = 16;
        private Color _tintColor = Color.Black;
        private float _tintOpacity;
        private float _topTintHeight = float.NaN;
        private float _bottomTintHeight = float.NaN;
        private float _tintSolidHeight;

        public ProgressiveBlur()
        {
            IsFocusable = false;
        }

        /// <summary>Height of the blurred band at the top edge, in layout units (0: none).</summary>
        public float TopHeight
        {
            get => _topHeight;
            set => SetBlurValue(ref _topHeight, value);
        }

        /// <summary>Height of the blurred band at the bottom edge, in layout units (0: none).</summary>
        public float BottomHeight
        {
            get => _bottomHeight;
            set => SetBlurValue(ref _bottomHeight, value);
        }

        /// <summary>Blur radius at the very edge, in layout units; it falls linearly to 0 across each band.</summary>
        public float MaxRadius
        {
            get => _maxRadius;
            set => SetBlurValue(ref _maxRadius, value);
        }

        /// <summary>Colour the bands are tinted towards (straight alpha), following the same gradient as the blur.</summary>
        public Color TintColor
        {
            get => _tintColor;
            set
            {
                if (_tintColor == value)
                    return;
                _tintColor = value;
                InvalidateLayout(false);
            }
        }

        /// <summary>Strength of the tint at the very edge (0..1); it falls linearly to 0 across each band like the blur
        /// radius. 0 (the default) tints nothing.</summary>
        public float TintOpacity
        {
            get => _tintOpacity;
            set => SetBlurValue(ref _tintOpacity, Math.Min(1, value));
        }

        /// <summary>Height of the tint at the top edge, in layout units, independent of the blur band: full
        /// <see cref="TintOpacity"/> for <see cref="TintSolidHeight"/>, then an eased fade to 0. NaN (the default): the
        /// tint follows the blur band's gradient. Setting either edge gives the tint its own extent at both (an unset
        /// edge then has no tint).</summary>
        public float TopTintHeight
        {
            get => _topTintHeight;
            set => SetTintHeight(ref _topTintHeight, value);
        }

        /// <summary>Height of the tint at the bottom edge, like <see cref="TopTintHeight"/>.</summary>
        public float BottomTintHeight
        {
            get => _bottomTintHeight;
            set => SetTintHeight(ref _bottomTintHeight, value);
        }

        /// <summary>The part of <see cref="TopTintHeight"/>/<see cref="BottomTintHeight"/> next to the edge that keeps the
        /// full <see cref="TintOpacity"/> before the fade begins, in layout units (0: the fade starts at the edge).</summary>
        public float TintSolidHeight
        {
            get => _tintSolidHeight;
            set => SetBlurValue(ref _tintSolidHeight, value);
        }

        /// <summary>Whether the tint has its own extent (<see cref="TopTintHeight"/> or <see cref="BottomTintHeight"/> set).</summary>
        internal bool HasOwnTint => !float.IsNaN(_topTintHeight) || !float.IsNaN(_bottomTintHeight);

        /// <summary>The bands as drawn in the last frame (flights out of this blur are drawn with them).</summary>
        internal BlurBands? DrawnBands;

        private void SetTintHeight(ref float field, float value)
        {
            value = float.IsNaN(value) ? float.NaN : float.IsFinite(value) ? Math.Max(0, value) : 0;
            if (field.Equals(value))
                return;
            field = value;
            InvalidateLayout(false);
        }

        private void SetBlurValue(ref float field, float value)
        {
            value = float.IsFinite(value) ? Math.Max(0, value) : 0;
            if (field == value)
                return;
            field = value;
            InvalidateLayout(false);
        }
    }
}

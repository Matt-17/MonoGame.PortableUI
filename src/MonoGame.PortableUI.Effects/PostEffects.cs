using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Effects;

namespace MonoGame.PortableUI
{
    // The post effects themes and displays use. The core keeps the PostEffect base and the effect
    // lists; this package draws them (PostProcessManager) and installs itself when loaded.

    /// <summary>Direction of the dark lines drawn by <see cref="ScanlinePostEffect"/>.</summary>
    public enum ScanlineOrientation
    {
        /// <summary>Horizontal lines, as on a normally mounted CRT.</summary>
        Horizontal,

        /// <summary>Vertical lines, as on a CRT mounted on its side (portrait arcade monitors).</summary>
        Vertical
    }

    public sealed class ScanlinePostEffect : PostEffect
    {
        public ScanlinePostEffect() : base("scanlines")
        {
        }

        public float Spacing { get; set; } = 3;
        public float Strength { get; set; } = 0.18f;

        /// <summary>
        ///     Line direction. <see cref="ScanlineOrientation.Vertical"/> suits a display whose tube is turned
        ///     on its side, e.g. a 3:4 portrait arcade monitor: the beam still sweeps the tube's long side.
        /// </summary>
        public ScanlineOrientation Orientation { get; set; } = ScanlineOrientation.Horizontal;
    }

    /// <summary>
    ///     CRT screen curvature. It belongs to the display, not the look: it only applies as a display
    ///     effect (<see cref="ScreenEngineOptions.PostEffects"/>, <see cref="UISurface.PostEffects"/>);
    ///     in a theme's or ThemeIsland's effect list it is ignored. Pointer input follows the curve.
    /// </summary>
    public sealed class CrtBarrelPostEffect : PostEffect
    {
        public CrtBarrelPostEffect() : base("crt-barrel")
        {
        }

        public float Distortion { get; set; } = 0.08f;
        public float Vignette { get; set; } = 0.24f;

        /// <inheritdoc />
        public override bool IsDisplayOnly => true;

        private float ClampedDistortion => MathHelper.Clamp(Distortion, 0, 0.5f);

        /// <inheritdoc />
        public override PointF DisplayToUi(PointF point, Rect screen)
            => ClampedDistortion > 0 && screen.Width > 0 && screen.Height > 0 ? PostProcessManager.InverseBarrel(point, screen, ClampedDistortion) : point;

        /// <inheritdoc />
        public override PointF UiToDisplay(PointF point, Rect screen)
            => ClampedDistortion > 0 && screen.Width > 0 && screen.Height > 0 ? PostProcessManager.ForwardBarrel(point, screen, ClampedDistortion) : point;
    }

    public sealed class VignettePostEffect : PostEffect
    {
        public VignettePostEffect() : base("vignette")
        {
        }

        public float Strength { get; set; } = 0.2f;
    }

    public sealed class FilmGrainPostEffect : PostEffect
    {
        public FilmGrainPostEffect() : base("film-grain")
        {
        }

        public float Strength { get; set; } = 0.04f;
    }

    public sealed class BloomPostEffect : PostEffect
    {
        public BloomPostEffect() : base("bloom")
        {
        }

        public float Strength { get; set; } = 0.25f;
        public float Threshold { get; set; } = 0.72f;
    }

    public sealed class DotMatrixPostEffect : PostEffect
    {
        public DotMatrixPostEffect() : base("dot-matrix")
        {
        }

        public float CellSize { get; set; } = 3;
        public float Strength { get; set; } = 0.18f;
    }
}

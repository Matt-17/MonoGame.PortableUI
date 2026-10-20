namespace MonoGame.PortableUI.Controls
{
    /// <summary>How a <see cref="ProgressIndicator"/> animates.</summary>
    public enum ProgressIndicatorStyle
    {
        /// <summary>Squares bouncing in a column (the original PortableUI spinner).</summary>
        Squares,

        /// <summary>Material's circular indicator: an arc that rotates while it grows and shrinks.</summary>
        CircularArc
    }

    /// <summary>Axis a <see cref="ProgressIndicator"/>'s circular arc spins around.</summary>
    public enum ProgressIndicatorAxis
    {
        /// <summary>In the screen plane (Material).</summary>
        Z,

        /// <summary>Also flips around the horizontal axis.</summary>
        X,

        /// <summary>Also flips around the vertical axis.</summary>
        Y
    }
}

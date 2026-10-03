namespace MonoGame.PortableUI.Controls
{
    /// <summary>How a control's drawing is cached (<see cref="Control.CacheMode"/>).</summary>
    public enum CacheMode
    {
        /// <summary>Drawn every frame (default).</summary>
        None,

        /// <summary>The control and its subtree are drawn into a texture that is reused until something inside changes.</summary>
        Bitmap
    }
}

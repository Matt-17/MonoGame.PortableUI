using System;
using Microsoft.Xna.Framework;

using MonoGame.PortableUI.Text;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>FontStashSharp fonts for the demos, loaded through TitleContainer (files on desktop, assets on Android).</summary>
    public static class DemoFonts
    {
        private static bool _loaded;
        private static FontStashUIFont? _selawik;

        /// <summary>Selawik (SIL OFL 1.1, Microsoft's open Segoe UI substitute), or null when the file is missing.</summary>
        public static FontStashUIFont? Selawik
        {
            get
            {
                if (_loaded)
                    return _selawik;
                _loaded = true;
                try
                {
                    using var stream = TitleContainer.OpenStream("Content/Fonts/Selawik-Regular.ttf");
                    _selawik = FontStashUIFont.FromStreams(16, stream);
                }
                catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException or ArgumentException)
                {
                    _selawik = null;
                }
                return _selawik;
            }
        }
    }
}

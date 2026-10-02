using System;
using System.IO;
using MonoGame.PortableUI.FontStashSharp;

namespace MonoGame.PortableUI.Demo
{
    /// <summary>FontStashSharp fonts for the demo, loaded from the OFL TTFs copied next to the executable.</summary>
    internal static class DemoFonts
    {
        private static bool _loaded;
        private static FontStashUIFont? _atkinson;

        /// <summary>Atkinson Hyperlegible (SIL OFL 1.1), or null when the file is missing (e.g. Android assets).</summary>
        public static FontStashUIFont? Atkinson
        {
            get
            {
                if (_loaded)
                    return _atkinson;
                _loaded = true;
                var path = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "AtkinsonHyperlegible-Regular.ttf");
                if (File.Exists(path))
                    _atkinson = FontStashUIFont.FromFiles(16, path);
                return _atkinson;
            }
        }
    }
}

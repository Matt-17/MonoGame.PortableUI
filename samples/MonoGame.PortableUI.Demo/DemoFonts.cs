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

        // Theme font name -> bundled TTF (see docs/FONTS.md). "default" is the default theme's font.
        // Scale: the SpriteFonts of two pixel fonts are baked at another size than their "-14" names say.
        private static readonly (string Name, string File, float Scale)[] ThemeFonts =
        {
            ("anton", "Anton-Regular.ttf", 1),
            ("atkinsonhyperlegible", "AtkinsonHyperlegible-Regular.ttf", 1),
            ("barlowcondensed", "BarlowCondensed-SemiBold.ttf", 1),
            ("cinzel", "Cinzel-SemiBold.ttf", 1),
            ("default", "Selawik-Regular.ttf", 1),
            ("fredoka", "Fredoka-SemiBold.ttf", 1),
            ("ibmplexmono", "IBMPlexMono-Regular.ttf", 1),
            ("jersey10", "Jersey10-Regular.ttf", 1),
            ("orbitron", "Orbitron-Variable.ttf", 1),
            ("pressstart2p", "PressStart2P-Regular.ttf", 11f / 14),
            ("px437ibmvga8x16", "Px437_IBM_VGA_8x16.ttf", 1),
            ("roboto", "Roboto-Variable.ttf", 1),
            ("selawik", "Selawik-Regular.ttf", 1),
            ("silkscreen", "Silkscreen-Regular.ttf", 1),
            ("vt323", "VT323-Regular.ttf", 16f / 14)
        };

        /// <summary>
        ///     Registers the bundled TTFs under the themes' font names, so every theme draws with a runtime font
        ///     (sharp at any size; pixel fonts on their pixel grid) instead of its baked SpriteFont. Loaded lazily.
        /// </summary>
        public static void RegisterThemeFonts()
        {
            foreach (var (name, file, scale) in ThemeFonts)
                FontManager.RegisterDynamicFontFile(name, "Content/Fonts/" + file, sizeScale: scale);
        }

        /// <summary>Theme TTF files the Android head ships as assets.</summary>
        public static System.Collections.Generic.IEnumerable<string> ThemeFontFiles
        {
            get
            {
                foreach (var (_, file, _) in ThemeFonts)
                    yield return file;
            }
        }

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

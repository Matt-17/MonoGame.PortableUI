using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Exceptions;

namespace MonoGame.PortableUI
{
    /// <summary>
    /// Fonts are loaded by naming convention:
    /// * {name}-{style}-{size}.spritefont
    /// * everything lower case
    /// * name is used as identifier to load font
    /// * sizes are all even numbers from 2 to 64
    /// * style one of: regular, bold, italic or bolditalic
    /// * e.g. arial-bold-12.spritefont
    /// all Fonts have to be inside 'Fonts' folder
    /// </summary>
    public class FontManager
    {
        private const int DefaultSize = 14;

        private static Dictionary<string, SpriteFont>? Fonts { get; set; }
        private static readonly HashSet<string> RegisteredFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Game? FontGame { get; set; }
        private static string? ContentRoot { get; set; }
        private static bool CanProbeContentRoot { get; set; }

        public static SpriteFont? DefaultFont { get; set; }

        public static void LoadFonts(Game game, params string[] fontList)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (Fonts == null)
                Fonts = new Dictionary<string, SpriteFont>();
            FontGame = game;
            ContentRoot = ResolveContentRoot(game.Content.RootDirectory, AppContext.BaseDirectory);
            CanProbeContentRoot = Directory.Exists(ContentRoot);

            foreach (var font in fontList)
            {
                if (!string.IsNullOrWhiteSpace(font))
                    RegisteredFonts.Add(font);
            }

            EnsureDefaultFont();
        }

        internal static string ResolveContentRoot(string rootDirectory, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                return Path.GetFullPath(baseDirectory);

            if (Path.IsPathFullyQualified(rootDirectory))
                return Path.GetFullPath(rootDirectory);

            return Path.GetFullPath(Path.Combine(baseDirectory, rootDirectory));
        }

        internal static bool ContentAssetExists(string contentRoot, string assetName)
        {
            var relativePath = assetName
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var assetPath = Path.Combine(contentRoot, $"{relativePath}.xnb");
            return File.Exists(assetPath);
        }

        public static SpriteFont GetFont(string? font = null, FontStyle style = FontStyle.Regular, int size = DefaultSize)
        {
            if (font == null)
            {
                EnsureDefaultFont();
                if (DefaultFont == null)
                    throw new DefaultFontMissingException();
                return DefaultFont;
            }

            if (TryLoadFont(font, style, size, out var spriteFont))
                return spriteFont;

            throw new FontMissingException(CreateFontKey(font, style, size));
        }

        internal static string CreateFontKey(string font, FontStyle style, int size)
        {
            return $"{font}-{style.ToString().ToLowerInvariant()}-{size}";
        }

        private static void EnsureDefaultFont()
        {
            if (DefaultFont != null)
                return;

            foreach (var font in RegisteredFonts)
            {
                if (TryLoadFont(font, FontStyle.Regular, DefaultSize, out var spriteFont))
                {
                    DefaultFont = spriteFont;
                    return;
                }
            }
        }

        private static bool TryLoadFont(string font, FontStyle style, int size, out SpriteFont spriteFont)
        {
            if (Fonts == null)
                Fonts = new Dictionary<string, SpriteFont>();

            var fontKey = CreateFontKey(font, style, size);
            if (Fonts.TryGetValue(fontKey, out var cachedFont))
            {
                spriteFont = cachedFont;
                return true;
            }

            spriteFont = null!;
            if (FontGame == null)
                return false;

            var assetName = $"Fonts/{fontKey}";
            if (CanProbeContentRoot && ContentRoot != null && !ContentAssetExists(ContentRoot, assetName))
                return false;

            try
            {
                spriteFont = FontGame.Content.Load<SpriteFont>(assetName);
                Fonts[fontKey] = spriteFont;
                return true;
            }
            catch when (!CanProbeContentRoot)
            {
                // Some platforms do not expose content as files, so keep the legacy fallback there.
                return false;
            }
        }
    }
}

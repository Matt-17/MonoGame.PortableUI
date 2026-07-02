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

        private const int MaxFontSize = 64;

        private const int MinFontSize = 2;

        private static Dictionary<string, SpriteFont>? Fonts { get; set; }

        public static SpriteFont? DefaultFont { get; set; }

        public static void LoadFonts(Game game, params string[] fontList)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            if (Fonts == null)
                Fonts = new Dictionary<string, SpriteFont>();
            var fonts = Fonts;
            var contentRoot = ResolveContentRoot(game.Content.RootDirectory, AppContext.BaseDirectory);
            var canProbeContentRoot = Directory.Exists(contentRoot);

            foreach (var font in fontList)
            {
                for (int size = MinFontSize; size < MaxFontSize; size += 2)
                {
                    foreach (var style in Enum.GetValues(typeof(FontStyle)))
                    {
                        var styleName = style.ToString()!.ToLowerInvariant();
                        var formattableString = $@"{font}-{styleName}-{size}";
                        var assetName = $"Fonts/{formattableString}";

                        if (canProbeContentRoot && !ContentAssetExists(contentRoot, assetName))
                            continue;

                        try
                        {
                            var spriteFont = game.Content.Load<SpriteFont>(assetName);
                            if (DefaultFont == null)
                                DefaultFont = spriteFont;
                            fonts[$"{formattableString}"] = spriteFont;
                        }
                        catch when (!canProbeContentRoot)
                        {
                            // Some platforms do not expose content as files, so keep the legacy probing fallback there.
                        }
                    }
                }
            }
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
                if (DefaultFont == null)
                    throw new DefaultFontMissingException();
                return DefaultFont;
            }

            try
            {
                if (Fonts == null)
                    throw new FontMissingException($"{font}-{style.ToString().ToLower()}-{size}");

                return Fonts[$"{font}-{style.ToString().ToLower()}-{size}"];
            }
            catch
            {
                throw new FontMissingException($"{font}-{style.ToString().ToLower()}-{size}");
            }
        }
    }
}

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
        // Baked pixel size per loaded SpriteFont, so TextBlock can scale glyphs to its TextSize.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SpriteFont, object> FontSizes = new();
        private static readonly HashSet<string> RegisteredFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> WarnedMissingFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Game? FontGame { get; set; }
        private static string? ContentRoot { get; set; }
        private static bool CanProbeContentRoot { get; set; }

        public static SpriteFont? DefaultFont { get; set; }

        /// <summary>
        ///     The pixel size a font was baked at (from its <c>name-style-size</c> asset), so callers
        ///     can scale glyphs to a requested size. Falls back to <see cref="DefaultSize"/> for fonts
        ///     that were not loaded through this manager.
        /// </summary>
        public static int GetBakedSize(SpriteFont? font)
        {
            if (font != null && FontSizes.TryGetValue(font, out var boxed) && boxed is int size)
                return size;
            return DefaultSize;
        }

        /// <summary>
        /// Clears all cached fonts and the associated <see cref="Game"/>. The cached
        /// <see cref="SpriteFont"/>s are tied to a specific <see cref="GraphicsDevice"/>, so this must
        /// run whenever the host game/graphics device is recreated (e.g. an Android activity restart),
        /// otherwise stale fonts referencing a disposed device would be handed out.
        /// </summary>
        public static void Reset()
        {
            Fonts = null;
            DefaultFont = null;
            FontSizes.Clear();
            FontGame = null;
            ContentRoot = null;
            CanProbeContentRoot = false;
            RegisteredFonts.Clear();
            WarnedMissingFonts.Clear();
        }

        public static void LoadFonts(Game game, params string[] fontList)
        {
            if (game == null)
                throw new ArgumentNullException(nameof(game));

            // A new Game instance means a new content pipeline / GraphicsDevice — drop the caches
            // bound to the previous one so we never serve fonts backed by a disposed device.
            if (!ReferenceEquals(FontGame, game))
                Reset();

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

        /// <summary>Loads a font without throwing; returns false when the asset is not built/registered.</summary>
        public static bool TryGetFont(string? font, out SpriteFont? spriteFont, FontStyle style = FontStyle.Regular, int size = DefaultSize)
        {
            if (font == null)
            {
                EnsureDefaultFont();
                spriteFont = DefaultFont;
                return spriteFont != null;
            }

            if (TryLoadFont(font, style, size, out var loaded))
            {
                spriteFont = loaded;
                return true;
            }

            spriteFont = null;
            return false;
        }

        /// <summary>
        ///     Loads a font, falling back to <see cref="DefaultFont"/> when it is not built —
        ///     themes render with correct colors/shapes and the default font instead of failing.
        ///     Logs one warning per missing font asset.
        /// </summary>
        public static SpriteFont? GetFontOrDefault(string? font = null, FontStyle style = FontStyle.Regular, int size = DefaultSize)
        {
            if (TryGetFont(font, out var spriteFont, style, size))
                return spriteFont;

            var fontKey = CreateFontKey(font!, style, size);
            if (WarnedMissingFonts.Add(fontKey))
                System.Diagnostics.Trace.TraceWarning($"MonoGame.PortableUI: font asset 'Fonts/{fontKey}' is not built; falling back to the default font. See the MonoGame.PortableUI.Themes ThemeContent/README.md for font setup.");

            EnsureDefaultFont();
            return DefaultFont;
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
                FontSizes.AddOrUpdate(spriteFont, size);
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

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

        // The same fonts by (name, style, size), and the ones known to be missing: controls look their font up on every
        // draw, and building the string key ("name-regular-14") was three strings per lookup, every frame.
        private static readonly Dictionary<(string Font, FontStyle Style, int Size), SpriteFont> FontsByKey = new();
        private static readonly HashSet<(string Font, FontStyle Style, int Size)> MissingFonts = new();
        private static readonly HashSet<(string Font, FontStyle Style, int Size)> FallbackFonts = new();
        // Baked pixel size per loaded SpriteFont, so TextBlock can scale glyphs to its TextSize.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SpriteFont, object> FontSizes = new();
        private static readonly HashSet<string> RegisteredFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> WarnedMissingFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Game? FontGame { get; set; }
        private static string? ContentRoot { get; set; }
        private static bool CanProbeContentRoot { get; set; }

        private static SpriteFont? _defaultFont;
        private static Text.UIFont? _defaultDynamicFont;
        private static long _dynamicFontVersion;

        /// <summary>
        ///     A runtime-rasterizing font (e.g. FontStashSharp) every text control uses instead of its
        ///     SpriteFont, unless it has its own <c>FontOverride</c> or <c>DynamicFont</c>.
        ///     Null (the default) keeps the SpriteFont path. Running screens pick a change up on their
        ///     next update.
        /// </summary>
        public static Text.UIFont? DefaultDynamicFont
        {
            get => _defaultDynamicFont;
            set
            {
                if (ReferenceEquals(_defaultDynamicFont, value))
                    return;
                _defaultDynamicFont = value;
                System.Threading.Interlocked.Increment(ref _dynamicFontVersion);
            }
        }

        internal static long DynamicFontVersion => System.Threading.Interlocked.Read(ref _dynamicFontVersion);

        internal static void NotifyDynamicFontChanged() => System.Threading.Interlocked.Increment(ref _dynamicFontVersion);

        public static SpriteFont? DefaultFont
        {
            get => _defaultFont;
            set
            {
                EnsureFallbackCharacter(value);
                _defaultFont = value;
            }
        }

        /// <summary>
        ///     Gives a font without a <see cref="SpriteFont.DefaultCharacter"/> a fallback glyph
        ///     ('?' when the font contains it), so measuring or drawing text with characters the font
        ///     does not cover — e.g. 'ä' typed into a TextBox with an ASCII-only font — renders a
        ///     placeholder instead of throwing <see cref="ArgumentException"/>. Fonts that already
        ///     define a default character are left untouched.
        /// </summary>
        public static void EnsureFallbackCharacter(SpriteFont? font)
        {
            if (font == null || font.DefaultCharacter.HasValue)
                return;

            var characters = font.Characters;
            if (characters.Count == 0)
                return;

            font.DefaultCharacter = characters.Contains('?') ? '?'
                : characters.Contains(' ') ? ' '
                : characters[0];
        }

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
            FontsByKey.Clear();
            MissingFonts.Clear();
            FallbackFonts.Clear();
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

            // A missing font is asked for every frame: after the first warning, fall back without building its key again.
            if (font != null && !FallbackFonts.Add((font, style, size)))
            {
                EnsureDefaultFont();
                return DefaultFont;
            }
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
            if (FontsByKey.TryGetValue((font, style, size), out var known))
            {
                spriteFont = known;
                return true;
            }
            spriteFont = null!;
            if (FontGame != null && MissingFonts.Contains((font, style, size)))
                return false;

            if (Fonts == null)
                Fonts = new Dictionary<string, SpriteFont>();

            var fontKey = CreateFontKey(font, style, size);
            if (Fonts.TryGetValue(fontKey, out var cachedFont))
            {
                spriteFont = cachedFont;
                FontsByKey[(font, style, size)] = cachedFont;
                return true;
            }

            if (FontGame == null)
                return false;

            var assetName = $"Fonts/{fontKey}";
            if (CanProbeContentRoot && ContentRoot != null && !ContentAssetExists(ContentRoot, assetName))
            {
                MissingFonts.Add((font, style, size));
                return false;
            }

            try
            {
                spriteFont = FontGame.Content.Load<SpriteFont>(assetName);
                EnsureFallbackCharacter(spriteFont);
                Fonts[fontKey] = spriteFont;
                FontsByKey[(font, style, size)] = spriteFont;
                FontSizes.AddOrUpdate(spriteFont, size);
                return true;
            }
            catch when (!CanProbeContentRoot)
            {
                // Some platforms do not expose content as files, so keep the legacy fallback there.
                MissingFonts.Add((font, style, size));
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// </summary>
    public class FontManager
    {
        private const int DefaultSize = 14;

        private const int MaxFontSize = 64;

        private const int MinFontSize = 2;

        private static Dictionary<string, SpriteFont> Fonts { get; set; }
        
        public static SpriteFont DefaultFont { get; set; }

        public static void LoadFonts(Game game, params string[] fontList)
        {
            if (Fonts == null)
                Fonts = new Dictionary<string, SpriteFont>();
            foreach (var font in fontList)
            {
                for (int size = MinFontSize; size < MaxFontSize; size+=2)
                {
                    foreach (var style in Enum.GetValues(typeof(FontStyle)))
                    {
                        try
                        {
                            var formattableString = $@"{font}-{style.ToString().ToLower()}-{size}";
                            var spriteFont = game.Content.Load<SpriteFont>($"Fonts/{formattableString}");
                            if (DefaultFont == null)
                                DefaultFont = spriteFont;
                            Fonts[$"{formattableString}"] = spriteFont;
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
        }


        public static SpriteFont GetFont(string font = null, FontStyle style = FontStyle.Regular, int size = DefaultSize)
        {
            if (font == null)
            {
                if (DefaultFont == null)
                    throw new DefaultFontMissingException();
                return DefaultFont;
            }

            try
            {
                return Fonts[$"{font}-{style.ToString().ToLower()}-{size}"];
            }
            catch 
            {
                throw new FontMissingException($"{font}-{style.ToString().ToLower()}-{size}");
            }
        }
    }

    public enum FontStyle
    {
        Regular,
        Bold,
        Italic,
        BoldItalic
    }
}

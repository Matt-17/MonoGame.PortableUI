using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>
    ///     WCAG-style relative-luminance contrast audit over every registered theme palette.
    ///     Guards against unreadable combinations like light-gray text on gray buttons.
    /// </summary>
    [TestClass]
    public class ThemeContrastTests
    {
        private const double MinimumContrast = 3.0;

        [TestMethod]
        public void Every_theme_palette_has_readable_core_combinations()
        {
            var failures = new List<string>();
            foreach (var definition in ThemeRegistry.Themes)
            {
                var palette = definition.Palette;
                var theme = definition.CreateTheme();

                // Translucent/glass surfaces composite over the backdrop; the flat palette color
                // is not what ends up on screen, so those can only be judged visually.
                if (palette.SurfaceBrush == null)
                {
                    Check(failures, definition.Id, "Text on Surface", palette.Text, palette.Surface);
                    Check(failures, definition.Id, "MutedText on Surface", palette.MutedText, palette.Surface, 2.4);
                }

                // Selected rows/blocks are large highlight areas; 2.5 is acceptable there.
                Check(failures, definition.Id, "SelectionText on Selection", palette.SelectionText, palette.Selection, 2.5);

                var buttonFace = theme.ButtonBackgroundBrush is MonoGame.PortableUI.Media.SolidColorBrush solid
                    ? solid.Color
                    : palette.SurfaceBrush == null ? palette.Surface : (Color?)null;
                if (buttonFace is { } face)
                    Check(failures, definition.Id, "ButtonText on button face", theme.ButtonTextColor, face);
            }

            Assert.AreEqual(0, failures.Count, "Contrast failures:\n" + string.Join("\n", failures));
        }

        private static void Check(List<string> failures, string id, string what, Color foreground, Color background, double minimum = MinimumContrast)
        {
            // Translucent backgrounds/foregrounds cannot be judged without the composite result.
            if (background.A < 200 || foreground.A < 200)
                return;

            var ratio = ContrastRatio(foreground, background);
            if (ratio < minimum)
                failures.Add($"{id}: {what} = {ratio:0.00} (< {minimum:0.0}) fg={foreground} bg={background}");
        }

        internal static double ContrastRatio(Color a, Color b)
        {
            var la = RelativeLuminance(a);
            var lb = RelativeLuminance(b);
            var lighter = Math.Max(la, lb);
            var darker = Math.Min(la, lb);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double RelativeLuminance(Color color)
        {
            static double Channel(byte value)
            {
                var c = value / 255.0;
                return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }
    }
}

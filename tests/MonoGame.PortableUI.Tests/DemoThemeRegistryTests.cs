using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Demo;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class DemoThemeRegistryTests
    {
        private static readonly string[] ExpectedThemeIds =
        {
            "c64",
            "gameboy",
            "dos",
            "amiga",
            "terminal",
            "studio"
        };

        [TestMethod]
        public void Registry_contains_all_demo_presets()
        {
            var actualIds = DemoThemeRegistry.Presets.Select(preset => preset.Id).ToArray();

            CollectionAssert.AreEquivalent(ExpectedThemeIds, actualIds);
            Assert.AreEqual(ExpectedThemeIds.Length, DemoThemeRegistry.Presets.Count);
        }

        [TestMethod]
        public void Startup_theme_prefers_command_line_over_environment()
        {
            var split = DemoThemeRegistry.ResolveStartupTheme(new[] { "--theme", "dos" }, "gameboy");
            var equals = DemoThemeRegistry.ResolveStartupTheme(new[] { "--theme=terminal" }, "gameboy");

            Assert.AreEqual("dos", split.Id);
            Assert.AreEqual("terminal", equals.Id);
        }

        [TestMethod]
        public void Startup_theme_uses_environment_when_arguments_do_not_specify_theme()
        {
            var preset = DemoThemeRegistry.ResolveStartupTheme(new[] { "--windowed" }, "amiga");

            Assert.AreEqual("amiga", preset.Id);
        }

        [TestMethod]
        public void Unknown_theme_ids_fall_back_to_c64()
        {
            Assert.AreEqual("c64", DemoThemeRegistry.Resolve("missing").Id);
            Assert.AreEqual("c64", DemoThemeRegistry.ResolveStartupTheme(new[] { "--theme", "missing" }, "dos").Id);
            Assert.AreEqual("c64", DemoThemeRegistry.ResolveStartupTheme(new string[0], "missing").Id);
        }

        [TestMethod]
        public void Theme_argument_parser_supports_split_and_equals_forms()
        {
            Assert.IsTrue(DemoThemeRegistry.TryParseThemeArgument(new[] { "--theme", "gameboy" }, out var split));
            Assert.AreEqual("gameboy", split);

            Assert.IsTrue(DemoThemeRegistry.TryParseThemeArgument(new[] { "--theme=studio" }, out var equals));
            Assert.AreEqual("studio", equals);

            Assert.IsFalse(DemoThemeRegistry.TryParseThemeArgument(new[] { "--other" }, out var missing));
            Assert.IsNull(missing);
        }

        [TestMethod]
        public void Presets_have_theme_and_font_metadata()
        {
            foreach (var preset in DemoThemeRegistry.Presets)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(preset.Id));
                Assert.IsFalse(string.IsNullOrWhiteSpace(preset.DisplayName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(preset.FontName));
                Assert.IsNotNull(preset.Palette);
                Assert.IsNotNull(preset.CreateTheme);
                Assert.IsNotNull(preset.CreateTheme());
            }
        }
    }
}

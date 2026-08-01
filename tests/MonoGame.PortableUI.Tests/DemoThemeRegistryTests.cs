using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Demo;
using MonoGame.PortableUI.Media;

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
            "studio",
            "aurora"
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

        [TestMethod]
        public void Aurora_theme_uses_gradient_brushes()
        {
            var preset = DemoThemeRegistry.Resolve("aurora");
            var theme = preset.CreateTheme();

            AssertGradient(preset.Palette.BackgroundBrush, GradientDirection.DiagonalDown);
            AssertGradient(preset.Palette.SurfaceBrush, GradientDirection.Vertical);
            AssertGradient(preset.Palette.SelectionBrush, GradientDirection.Horizontal);
            AssertGradient(theme.ButtonBackgroundBrush, GradientDirection.DiagonalDown);
            AssertGradient(theme.TabSelectedHeaderBackgroundBrush, GradientDirection.Horizontal);
            AssertGradient(theme.TextBoxBackgroundBrush, GradientDirection.Vertical);
        }

        [TestMethod]
        public void Launch_settings_include_profile_for_each_demo_preset()
        {
            var path = Path.Combine(
                FindRepositoryRoot(),
                "samples",
                "MonoGame.PortableUI.Demo",
                "Properties",
                "launchSettings.json");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var profiles = document.RootElement.GetProperty("profiles");

            foreach (var preset in DemoThemeRegistry.Presets)
            {
                var expectedArgs = "--theme " + preset.Id;
                var hasProfile = profiles.EnumerateObject().Any(profile =>
                    profile.Value.TryGetProperty("commandLineArgs", out var args) &&
                    string.Equals(args.GetString(), expectedArgs, StringComparison.OrdinalIgnoreCase));

                Assert.IsTrue(hasProfile, $"Missing launch profile for theme '{preset.Id}'.");
            }
        }

        private static void AssertGradient(Brush? brush, GradientDirection direction)
        {
            Assert.IsInstanceOfType(brush, typeof(GradientBrush));
            Assert.AreEqual(direction, ((GradientBrush)brush!).Direction);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "MonoGame.PortableUI.slnx")))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the MonoGame.PortableUI repository root.");
        }
    }
}

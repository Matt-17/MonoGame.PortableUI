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
        private static readonly string[] ExpectedThemeIds = ThemeRegistry.Themes.Select(theme => theme.Id).ToArray();

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
        public void Unknown_theme_ids_fall_back_to_default()
        {
            Assert.AreEqual("default", DemoThemeRegistry.Resolve("missing").Id);
            Assert.AreEqual("default", DemoThemeRegistry.ResolveStartupTheme(new[] { "--theme", "missing" }, "dos").Id);
            Assert.AreEqual("default", DemoThemeRegistry.ResolveStartupTheme(new string[0], "missing").Id);
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
        public void Run_options_parse_screenshot_arguments()
        {
            var split = DemoRunOptions.Parse(new[] { "--theme", "dos", "--screenshot", "docs/themes", "--screenshot-screen", "main" });
            var equals = DemoRunOptions.Parse(new[] { "--theme=glass", "--screenshot=artifacts/themes", "--screenshot-screen=gallery" });

            Assert.AreEqual("dos", split.InitialThemePreset.Id);
            Assert.AreEqual("docs/themes", split.ScreenshotDirectory);
            Assert.AreEqual("main", split.ScreenshotScreen);
            Assert.IsTrue(split.IsScreenshotMode);

            Assert.AreEqual("glass", equals.InitialThemePreset.Id);
            Assert.AreEqual("artifacts/themes", equals.ScreenshotDirectory);
            Assert.AreEqual("gallery", equals.ScreenshotScreen);
            Assert.IsTrue(equals.IsScreenshotMode);
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
        public void Demo_font_names_are_registered_in_content_manifest()
        {
            var manifestPath = Path.Combine(
                FindRepositoryRoot(),
                "samples",
                "MonoGame.PortableUI.Demo",
                "Content",
                "Content.mgcb");
            var manifest = File.ReadAllText(manifestPath);

            foreach (var fontName in DemoThemeRegistry.FontNames)
                StringAssert.Contains(manifest, $"Fonts\\{fontName}-regular-14.spritefont");
        }

        [TestMethod]
        public void Glass_theme_uses_translucent_frosted_brushes()
        {
            var preset = DemoThemeRegistry.Resolve("glass");
            var theme = preset.CreateTheme();

            Assert.IsInstanceOfType(preset.Palette.BackgroundBrush, typeof(GlassBackdropBrush));
            AssertFrosted(preset.Palette.SurfaceBrush);
            AssertFrosted(preset.Palette.SurfaceAltBrush);
            AssertFrosted(preset.Palette.FieldFrameBrush);
            AssertFrosted(theme.ButtonBackgroundBrush);
            AssertFrosted(theme.ButtonHoverBrush);
            AssertFrosted(theme.ButtonPressedBrush);
            AssertFrosted(theme.TextBoxBackgroundBrush);
            Assert.IsTrue(((FrostedGlassBrush)preset.Palette.SurfaceBrush!).TintColor.A < byte.MaxValue);
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
        public void Launch_settings_theme_profiles_reference_registered_theme_ids()
        {
            var path = Path.Combine(
                FindRepositoryRoot(),
                "samples",
                "MonoGame.PortableUI.Demo",
                "Properties",
                "launchSettings.json");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var profiles = document.RootElement.GetProperty("profiles");
            var themedProfiles = 0;

            foreach (var profile in profiles.EnumerateObject())
            {
                if (!profile.Value.TryGetProperty("commandLineArgs", out var args))
                    continue;

                Assert.IsTrue(DemoThemeRegistry.TryParseThemeArgument(args.GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries), out var themeId));
                Assert.IsNotNull(themeId, $"Profile '{profile.Name}' has an empty --theme argument.");
                var resolved = DemoThemeRegistry.Resolve(themeId);

                Assert.AreEqual(themeId, resolved.Id, true, $"Profile '{profile.Name}' references an unknown theme id.");
                themedProfiles++;
            }

            Assert.IsTrue(themedProfiles > 0, "Expected at least one themed launch profile.");
        }

        private static void AssertFrosted(Brush? brush)
        {
            Assert.IsInstanceOfType(brush, typeof(FrostedGlassBrush));
            Assert.IsTrue(((FrostedGlassBrush)brush!).BlurRadius > 0);
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

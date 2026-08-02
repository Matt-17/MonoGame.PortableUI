using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Demo;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class VisualRegressionTests
    {
        [TestMethod]
        public void Theme_gallery_screenshots_exist_for_registered_demo_presets()
        {
            var directory = Path.Combine(FindRepositoryRoot(), "docs", "themes");

            foreach (var preset in DemoThemeRegistry.Presets)
            {
                var path = Path.Combine(directory, $"{preset.Id}.png");
                Assert.IsTrue(File.Exists(path), $"Missing theme screenshot: {preset.Id}");
                Assert.IsTrue(new FileInfo(path).Length > 1024, $"Theme screenshot is unexpectedly small: {preset.Id}");
            }

            var pngCount = Directory.GetFiles(directory, "*.png").Length;
            Assert.AreEqual(DemoThemeRegistry.Presets.Select(preset => preset.Id).Distinct().Count(), pngCount);
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

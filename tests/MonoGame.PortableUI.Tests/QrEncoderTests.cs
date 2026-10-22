using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonoGame.PortableUI.Media;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class QrEncoderTests
    {
        private static readonly string[] Samples =
        {
            "",
            "Hello, world!",
            "https://www.nayuki.io/",
            "Grüße an Bord ☀ – Ümläute & Emoji 🍹",
            new string('x', 300),
            new string('7', 1200),
        };

        [TestMethod]
        public void Picks_the_smallest_version_that_holds_the_text()
        {
            Assert.AreEqual(21, QrEncoder.Encode("Hello, world!", QrErrorCorrection.Low).GetLength(0));
            // 17 bytes fill version 1 at Low; 18 need version 2.
            Assert.AreEqual(21, QrEncoder.Encode(new string('a', 17), QrErrorCorrection.Low).GetLength(0));
            Assert.AreEqual(25, QrEncoder.Encode(new string('a', 18), QrErrorCorrection.Low).GetLength(0));
            Assert.AreEqual(177, QrEncoder.Encode(new string('a', 2953), QrErrorCorrection.Low).GetLength(0));
            Assert.ThrowsExactly<ArgumentException>(() => QrEncoder.Encode(new string('a', 2954), QrErrorCorrection.Low));
        }

        [TestMethod]
        public void Has_finder_and_timing_patterns()
        {
            foreach (var text in Samples)
            {
                var modules = QrEncoder.Encode(text, QrErrorCorrection.Medium);
                var size = modules.GetLength(0);
                foreach (var (fx, fy) in new[] { (3, 3), (size - 4, 3), (3, size - 4) })
                {
                    for (var dy = -3; dy <= 3; dy++)
                    {
                        for (var dx = -3; dx <= 3; dx++)
                            Assert.AreEqual(Math.Max(Math.Abs(dx), Math.Abs(dy)) != 2, modules[fy + dy, fx + dx], text);
                    }
                }
                for (var i = 8; i < size - 8; i++)
                {
                    Assert.AreEqual(i % 2 == 0, modules[6, i]);
                    Assert.AreEqual(i % 2 == 0, modules[i, 6]);
                }
                // The dark module.
                Assert.IsTrue(modules[size - 8, 8]);
            }
        }

        /// <summary>With QR_DUMP_DIR set: writes the samples as PGM images, to read them back with a real decoder.</summary>
        [TestMethod]
        public void Dumps_samples_for_a_decoder()
        {
            var directory = Environment.GetEnvironmentVariable("QR_DUMP_DIR");
            if (string.IsNullOrEmpty(directory))
                return;
            Directory.CreateDirectory(directory);
            for (var i = 0; i < Samples.Length; i++)
            {
                foreach (QrErrorCorrection level in Enum.GetValues(typeof(QrErrorCorrection)))
                {
                    var modules = QrEncoder.Encode(Samples[i], level);
                    var size = modules.GetLength(0);
                    const int scale = 6, quiet = 4;
                    var side = (size + 2 * quiet) * scale;
                    var image = new StringBuilder($"P2\n{side} {side}\n255\n");
                    for (var y = 0; y < side; y++)
                    {
                        for (var x = 0; x < side; x++)
                        {
                            int my = y / scale - quiet, mx = x / scale - quiet;
                            var dark = my >= 0 && my < size && mx >= 0 && mx < size && modules[my, mx];
                            image.Append(dark ? "0 " : "255 ");
                        }
                        image.Append('\n');
                    }
                    File.WriteAllText(Path.Combine(directory, $"{i}-{level}.pgm"), image.ToString());
                    File.WriteAllText(Path.Combine(directory, $"{i}-{level}.txt"), Samples[i], new UTF8Encoding(false));
                }
            }
        }
    }
}

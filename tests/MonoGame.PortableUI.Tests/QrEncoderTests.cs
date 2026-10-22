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

        // An AIDA boarding ticket: version 1, error correction L, numeric mode, mask 2.
        private static readonly string[] TicketRows =
        {
            "111111100100101111111", "100000101001001000001", "101110100100001011101", "101110101001001011101",
            "101110100011101011101", "100000101110101000001", "111111101010101111111", "000000000011100000000",
            "111110111100110101010", "111001011010100100001", "111110101101000000001", "000010011100000100101",
            "001010100011000001001", "000000001011111011000", "111111101100101100010", "100000100101111001000",
            "101110101010100010010", "101110101000100111100", "101110101111010000000", "100000101000000111110",
            "111111101111010100100",
        };

        private static string[] Rows(bool[,] modules)
        {
            var size = modules.GetLength(0);
            var rows = new string[size];
            for (var y = 0; y < size; y++)
            {
                var row = new StringBuilder(size);
                for (var x = 0; x < size; x++)
                    row.Append(modules[y, x] ? '1' : '0');
                rows[y] = row.ToString();
            }
            return rows;
        }

        [TestMethod]
        public void Reproduces_a_printed_numeric_code_module_for_module()
        {
            // The printer's mask is not the lowest penalty by the standard's rule (that is mask 3 without the quiet zone,
            // 0 with it, for this code), so it is given.
            var modules = QrEncoder.Encode("17065574226572334", QrErrorCorrection.Low, QrMode.Numeric, boostErrorCorrection: false, mask: 2);
            CollectionAssert.AreEqual(TicketRows, Rows(modules));
            CollectionAssert.AreEqual(TicketRows, Rows(QrEncoder.Encode("17065574226572334", QrErrorCorrection.Low, QrMode.Auto, false, 1, 2)));
            // Boosted, the same digits take a stronger correction and look different.
            CollectionAssert.AreNotEqual(TicketRows, Rows(QrEncoder.Encode("17065574226572334", QrErrorCorrection.Low, QrMode.Numeric, mask: 2)));
        }

        [TestMethod]
        public void Auto_mode_takes_the_smallest_mode()
        {
            // 41 digits fill version 1 at Low in numeric mode; 25 alphanumeric characters; 17 bytes.
            Assert.AreEqual(21, QrEncoder.Encode(new string('7', 41), QrErrorCorrection.Low, boostErrorCorrection: false).GetLength(0));
            Assert.AreEqual(25, QrEncoder.Encode(new string('7', 42), QrErrorCorrection.Low, boostErrorCorrection: false).GetLength(0));
            Assert.AreEqual(21, QrEncoder.Encode(new string('A', 25), QrErrorCorrection.Low, boostErrorCorrection: false).GetLength(0));
            Assert.AreEqual(25, QrEncoder.Encode(new string('A', 26), QrErrorCorrection.Low, boostErrorCorrection: false).GetLength(0));
            Assert.AreEqual(29, QrEncoder.Encode(new string('7', 41), QrErrorCorrection.Low, QrMode.Byte, false).GetLength(0));
            Assert.AreEqual(177, QrEncoder.Encode(new string('7', 7089), QrErrorCorrection.Low).GetLength(0));
            Assert.ThrowsExactly<ArgumentException>(() => QrEncoder.Encode(new string('7', 7090), QrErrorCorrection.Low));
        }

        [TestMethod]
        public void A_fixed_mode_rejects_text_it_cannot_hold()
        {
            Assert.ThrowsExactly<ArgumentException>(() => QrEncoder.Encode("12a", QrErrorCorrection.Low, QrMode.Numeric));
            Assert.ThrowsExactly<ArgumentException>(() => QrEncoder.Encode("abc", QrErrorCorrection.Low, QrMode.Alphanumeric));
        }

        [TestMethod]
        public void A_fixed_version_is_kept_or_rejected()
        {
            Assert.AreEqual(57, QrEncoder.Encode("Hello", QrErrorCorrection.Medium, version: 10).GetLength(0));
            Assert.ThrowsExactly<ArgumentException>(() => QrEncoder.Encode(new string('a', 18), QrErrorCorrection.Low, version: 1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => QrEncoder.Encode("x", QrErrorCorrection.Low, version: 41));
        }

        [TestMethod]
        public void Each_fixed_mask_gives_its_own_picture_and_one_of_them_is_the_automatic_one()
        {
            var automatic = string.Join("", Rows(QrEncoder.Encode("Hello, world!", QrErrorCorrection.Medium)));
            var matches = 0;
            var pictures = new System.Collections.Generic.HashSet<string>();
            for (var mask = 0; mask < 8; mask++)
            {
                var picture = string.Join("", Rows(QrEncoder.Encode("Hello, world!", QrErrorCorrection.Medium, mask: mask)));
                pictures.Add(picture);
                if (picture == automatic)
                    matches++;
            }
            Assert.AreEqual(8, pictures.Count);
            Assert.AreEqual(1, matches);
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

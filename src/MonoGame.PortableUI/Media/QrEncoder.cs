// QR Code generator, after Project Nayuki's "QR Code generator library" (https://www.nayuki.io/page/qr-code-generator-library),
// reduced to byte mode. Original licence:
//
// Copyright (c) Project Nayuki. (MIT License)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// - The above copyright notice and this permission notice shall be included in
//   all copies or substantial portions of the Software.
// - The Software is provided "as is", without warranty of any kind, express or
//   implied, including but not limited to the warranties of merchantability,
//   fitness for a particular purpose and noninfringement. In no event shall the
//   authors or copyright holders be liable for any claim, damages or other
//   liability, whether in an action of contract, tort or otherwise, arising from,
//   out of or in connection with the Software or the use or other dealings in the
//   Software.

using System;
using System.Collections.Generic;
using System.Text;

namespace MonoGame.PortableUI.Media
{
    /// <summary>How much of a QR code may be damaged or covered and still read: about 7, 15, 25 or 30 %.</summary>
    public enum QrErrorCorrection
    {
        Low,
        Medium,
        Quartile,
        High,
    }

    /// <summary>
    ///     Encodes text (UTF-8, byte mode) into the modules of a QR code: the smallest version 1-40 that holds it, the
    ///     error correction raised as far as that version allows, the mask with the lowest penalty.
    /// </summary>
    internal static class QrEncoder
    {
        // Per error correction level (Low, Medium, Quartile, High) and version; index 0 unused.
        private static readonly sbyte[,] EccCodewordsPerBlock =
        {
            { -1, 7, 10, 15, 20, 26, 18, 20, 24, 30, 18, 20, 24, 26, 30, 22, 24, 28, 30, 28, 28, 28, 28, 30, 30, 26, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28 },
            { -1, 13, 22, 18, 26, 18, 24, 18, 22, 20, 24, 28, 26, 24, 20, 30, 24, 28, 28, 26, 30, 28, 30, 30, 30, 30, 28, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
            { -1, 17, 28, 22, 16, 22, 28, 26, 26, 24, 28, 24, 28, 22, 24, 24, 30, 28, 28, 26, 28, 30, 24, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 },
        };

        private static readonly sbyte[,] ErrorCorrectionBlocks =
        {
            { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
            { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
            { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8, 8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
            { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 },
        };

        // The two format bits of each level, in the order of the enum.
        private static readonly int[] FormatBits = { 1, 0, 3, 2 };

        private const int PenaltyN1 = 3;
        private const int PenaltyN2 = 3;
        private const int PenaltyN3 = 40;
        private const int PenaltyN4 = 10;

        /// <summary>The modules, [y, x], true = dark. Throws when the text does not fit version 40.</summary>
        public static bool[,] Encode(string text, QrErrorCorrection errorCorrection)
        {
            var data = Encoding.UTF8.GetBytes(text ?? string.Empty);

            // The smallest version that holds the data.
            int version, usedBits = 0;
            for (version = 1; ; version++)
            {
                var countBits = version <= 9 ? 8 : 16;
                usedBits = 4 + countBits + data.Length * 8;
                if (data.Length < 1 << countBits && usedBits <= DataCodewords(version, errorCorrection) * 8)
                    break;
                if (version >= 40)
                    throw new ArgumentException("The text is too long for a QR code.", nameof(text));
            }
            // A stronger correction where it still fits.
            for (var level = QrErrorCorrection.Medium; level <= QrErrorCorrection.High; level++)
            {
                if (level > errorCorrection && usedBits <= DataCodewords(version, level) * 8)
                    errorCorrection = level;
            }

            // Mode (byte), count, data, terminator, padding.
            var bits = new List<bool>();
            AppendBits(bits, 0x4, 4);
            AppendBits(bits, data.Length, version <= 9 ? 8 : 16);
            foreach (var b in data)
                AppendBits(bits, b, 8);
            var capacity = DataCodewords(version, errorCorrection) * 8;
            AppendBits(bits, 0, Math.Min(4, capacity - bits.Count));
            AppendBits(bits, 0, (8 - bits.Count % 8) % 8);
            for (var pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11)
                AppendBits(bits, pad, 8);
            var codewords = new byte[bits.Count / 8];
            for (var i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                    codewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));
            }

            var symbol = new Symbol(version, errorCorrection);
            symbol.DrawFunctionPatterns();
            symbol.DrawCodewords(AddEccAndInterleave(codewords, version, errorCorrection));
            symbol.ChooseMask();
            return symbol.Modules;
        }

        private static void AppendBits(List<bool> bits, int value, int length)
        {
            for (var i = length - 1; i >= 0; i--)
                bits.Add(((value >> i) & 1) != 0);
        }

        private static int RawDataModules(int version)
        {
            var result = (16 * version + 128) * version + 64;
            if (version >= 2)
            {
                var alignments = version / 7 + 2;
                result -= (25 * alignments - 10) * alignments - 55;
                if (version >= 7)
                    result -= 36;
            }
            return result;
        }

        private static int DataCodewords(int version, QrErrorCorrection level)
            => RawDataModules(version) / 8 - EccCodewordsPerBlock[(int)level, version] * ErrorCorrectionBlocks[(int)level, version];

        private static byte[] AddEccAndInterleave(byte[] data, int version, QrErrorCorrection level)
        {
            var blockCount = ErrorCorrectionBlocks[(int)level, version];
            var eccLength = EccCodewordsPerBlock[(int)level, version];
            var rawCodewords = RawDataModules(version) / 8;
            var shortBlocks = blockCount - rawCodewords % blockCount;
            var shortBlockLength = rawCodewords / blockCount;

            var divisor = ReedSolomonDivisor(eccLength);
            var blocks = new byte[blockCount][];
            for (int i = 0, k = 0; i < blockCount; i++)
            {
                var length = shortBlockLength - eccLength + (i < shortBlocks ? 0 : 1);
                var block = new byte[shortBlockLength + 1];
                Array.Copy(data, k, block, 0, length);
                k += length;
                var ecc = ReedSolomonRemainder(data, k - length, length, divisor);
                // Short blocks keep a gap before their correction bytes, skipped when interleaving.
                Array.Copy(ecc, 0, block, shortBlockLength + 1 - eccLength, eccLength);
                blocks[i] = block;
            }

            var result = new byte[rawCodewords];
            var n = 0;
            for (var i = 0; i < shortBlockLength + 1; i++)
            {
                for (var j = 0; j < blockCount; j++)
                {
                    if (i != shortBlockLength - eccLength || j >= shortBlocks)
                        result[n++] = blocks[j][i];
                }
            }
            return result;
        }

        private static byte[] ReedSolomonDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;
            var root = 1;
            for (var i = 0; i < degree; i++)
            {
                for (var j = 0; j < degree; j++)
                {
                    result[j] = Multiply(result[j], root);
                    if (j + 1 < degree)
                        result[j] ^= result[j + 1];
                }
                root = Multiply(root, 0x02);
            }
            return result;
        }

        private static byte[] ReedSolomonRemainder(byte[] data, int offset, int length, byte[] divisor)
        {
            var result = new byte[divisor.Length];
            for (var n = 0; n < length; n++)
            {
                var factor = data[offset + n] ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[^1] = 0;
                for (var i = 0; i < result.Length; i++)
                    result[i] ^= Multiply(divisor[i], factor);
            }
            return result;
        }

        private static byte Multiply(int x, int y)
        {
            var z = 0;
            for (var i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return (byte)z;
        }

        private sealed class Symbol
        {
            private readonly int _version;
            private readonly QrErrorCorrection _level;
            private readonly int _size;
            private readonly bool[,] _isFunction;

            public Symbol(int version, QrErrorCorrection level)
            {
                _version = version;
                _level = level;
                _size = version * 4 + 17;
                Modules = new bool[_size, _size];
                _isFunction = new bool[_size, _size];
            }

            public bool[,] Modules { get; }

            public void DrawFunctionPatterns()
            {
                for (var i = 0; i < _size; i++)
                {
                    SetFunction(6, i, i % 2 == 0);
                    SetFunction(i, 6, i % 2 == 0);
                }
                DrawFinder(3, 3);
                DrawFinder(_size - 4, 3);
                DrawFinder(3, _size - 4);

                var positions = AlignmentPositions();
                var count = positions.Length;
                for (var i = 0; i < count; i++)
                {
                    for (var j = 0; j < count; j++)
                    {
                        // Not over the finder patterns.
                        if (!(i == 0 && j == 0 || i == 0 && j == count - 1 || i == count - 1 && j == 0))
                            DrawAlignment(positions[i], positions[j]);
                    }
                }

                DrawFormatBits(0);
                DrawVersion();
            }

            /// <summary>The data in the zigzag of two-module columns, from the bottom right.</summary>
            public void DrawCodewords(byte[] data)
            {
                var i = 0;
                for (var right = _size - 1; right >= 1; right -= 2)
                {
                    if (right == 6)
                        right = 5;
                    for (var vertical = 0; vertical < _size; vertical++)
                    {
                        for (var j = 0; j < 2; j++)
                        {
                            var x = right - j;
                            var upward = ((right + 1) & 2) == 0;
                            var y = upward ? _size - 1 - vertical : vertical;
                            if (!_isFunction[y, x] && i < data.Length * 8)
                            {
                                Modules[y, x] = ((data[i >> 3] >> (7 - (i & 7))) & 1) != 0;
                                i++;
                            }
                        }
                    }
                }
            }

            public void ChooseMask()
            {
                var best = 0;
                var lowest = int.MaxValue;
                for (var mask = 0; mask < 8; mask++)
                {
                    ApplyMask(mask);
                    DrawFormatBits(mask);
                    var penalty = Penalty();
                    if (penalty < lowest)
                    {
                        best = mask;
                        lowest = penalty;
                    }
                    // Masking twice undoes it.
                    ApplyMask(mask);
                }
                ApplyMask(best);
                DrawFormatBits(best);
            }

            private void SetFunction(int x, int y, bool dark)
            {
                Modules[y, x] = dark;
                _isFunction[y, x] = true;
            }

            private void DrawFinder(int x, int y)
            {
                for (var dy = -4; dy <= 4; dy++)
                {
                    for (var dx = -4; dx <= 4; dx++)
                    {
                        var distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        int xx = x + dx, yy = y + dy;
                        if (xx >= 0 && xx < _size && yy >= 0 && yy < _size)
                            SetFunction(xx, yy, distance != 2 && distance != 4);
                    }
                }
            }

            private void DrawAlignment(int x, int y)
            {
                for (var dy = -2; dy <= 2; dy++)
                {
                    for (var dx = -2; dx <= 2; dx++)
                        SetFunction(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
                }
            }

            private int[] AlignmentPositions()
            {
                if (_version == 1)
                    return Array.Empty<int>();
                var count = _version / 7 + 2;
                var step = (_version * 8 + count * 3 + 5) / (count * 4 - 4) * 2;
                var result = new int[count];
                result[0] = 6;
                for (int i = count - 1, position = _size - 7; i >= 1; i--, position -= step)
                    result[i] = position;
                return result;
            }

            private void DrawFormatBits(int mask)
            {
                var data = FormatBits[(int)_level] << 3 | mask;
                var remainder = data;
                for (var i = 0; i < 10; i++)
                    remainder = (remainder << 1) ^ ((remainder >> 9) * 0x537);
                var bits = (data << 10 | remainder) ^ 0x5412;

                for (var i = 0; i <= 5; i++)
                    SetFunction(8, i, Bit(bits, i));
                SetFunction(8, 7, Bit(bits, 6));
                SetFunction(8, 8, Bit(bits, 7));
                SetFunction(7, 8, Bit(bits, 8));
                for (var i = 9; i < 15; i++)
                    SetFunction(14 - i, 8, Bit(bits, i));

                for (var i = 0; i < 8; i++)
                    SetFunction(_size - 1 - i, 8, Bit(bits, i));
                for (var i = 8; i < 15; i++)
                    SetFunction(8, _size - 15 + i, Bit(bits, i));
                // The dark module.
                SetFunction(8, _size - 8, true);
            }

            private void DrawVersion()
            {
                if (_version < 7)
                    return;
                var remainder = _version;
                for (var i = 0; i < 12; i++)
                    remainder = (remainder << 1) ^ ((remainder >> 11) * 0x1F25);
                var bits = _version << 12 | remainder;
                for (var i = 0; i < 18; i++)
                {
                    var bit = Bit(bits, i);
                    var a = _size - 11 + i % 3;
                    var b = i / 3;
                    SetFunction(a, b, bit);
                    SetFunction(b, a, bit);
                }
            }

            private void ApplyMask(int mask)
            {
                for (var y = 0; y < _size; y++)
                {
                    for (var x = 0; x < _size; x++)
                    {
                        var invert = mask switch
                        {
                            0 => (x + y) % 2 == 0,
                            1 => y % 2 == 0,
                            2 => x % 3 == 0,
                            3 => (x + y) % 3 == 0,
                            4 => (x / 3 + y / 2) % 2 == 0,
                            5 => x * y % 2 + x * y % 3 == 0,
                            6 => (x * y % 2 + x * y % 3) % 2 == 0,
                            _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                        };
                        if (invert && !_isFunction[y, x])
                            Modules[y, x] = !Modules[y, x];
                    }
                }
            }

            private int Penalty()
            {
                var result = 0;
                var history = new int[7];
                for (var pass = 0; pass < 2; pass++)
                {
                    // Rows, then columns: runs of one colour and finder-like patterns.
                    for (var a = 0; a < _size; a++)
                    {
                        var runColor = false;
                        var run = 0;
                        Array.Clear(history, 0, history.Length);
                        for (var b = 0; b < _size; b++)
                        {
                            var dark = pass == 0 ? Modules[a, b] : Modules[b, a];
                            if (dark == runColor)
                            {
                                run++;
                                if (run == 5)
                                    result += PenaltyN1;
                                else if (run > 5)
                                    result++;
                            }
                            else
                            {
                                AddHistory(run, history);
                                if (!runColor)
                                    result += CountFinderPatterns(history) * PenaltyN3;
                                runColor = dark;
                                run = 1;
                            }
                        }
                        result += TerminateAndCount(runColor, run, history) * PenaltyN3;
                    }
                }

                for (var y = 0; y < _size - 1; y++)
                {
                    for (var x = 0; x < _size - 1; x++)
                    {
                        var color = Modules[y, x];
                        if (color == Modules[y, x + 1] && color == Modules[y + 1, x] && color == Modules[y + 1, x + 1])
                            result += PenaltyN2;
                    }
                }

                var darkCount = 0;
                foreach (var dark in Modules)
                {
                    if (dark)
                        darkCount++;
                }
                var total = _size * _size;
                var k = (Math.Abs(darkCount * 20 - total * 10) + total - 1) / total - 1;
                return result + k * PenaltyN4;
            }

            private int CountFinderPatterns(int[] history)
            {
                var n = history[1];
                var core = n > 0 && history[2] == n && history[3] == n * 3 && history[4] == n && history[5] == n;
                return (core && history[0] >= n * 4 && history[6] >= n ? 1 : 0) + (core && history[6] >= n * 4 && history[0] >= n ? 1 : 0);
            }

            private int TerminateAndCount(bool runColor, int run, int[] history)
            {
                if (runColor)
                {
                    AddHistory(run, history);
                    run = 0;
                }
                run += _size;
                AddHistory(run, history);
                return CountFinderPatterns(history);
            }

            private void AddHistory(int run, int[] history)
            {
                // The light border before the first run.
                if (history[0] == 0)
                    run += _size;
                Array.Copy(history, 0, history, 1, history.Length - 1);
                history[0] = run;
            }

            private static bool Bit(int value, int index) => ((value >> index) & 1) != 0;
        }
    }
}

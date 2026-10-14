using System;

namespace MonoGame.PortableUI.Audio
{
    /// <summary>The character of the synthesized default sounds.</summary>
    public enum UiSynthStyle
    {
        /// <summary>Soft sine clicks and blips with a little body (modern, glass, flat themes).</summary>
        Soft,
        /// <summary>Square-wave bleeps (8-bit, terminal, retro desktop themes).</summary>
        Retro
    }

    /// <summary>
    ///     Renders the built-in UI sounds as 16-bit mono PCM, so an app has sound without shipping audio
    ///     files. Short (6-100 ms), quiet and dry, made for UI: they sit under game audio.
    /// </summary>
    public static class UiSoundSynth
    {
        public const int SampleRate = 44100;

        /// <summary>The sound for <paramref name="sound"/> as little-endian 16-bit mono PCM at <see cref="SampleRate"/>.</summary>
        public static byte[] Render(UiSound sound, UiSynthStyle style = UiSynthStyle.Soft)
        {
            var samples = new float[(int)(SampleRate * 0.12)];
            var length = 0;
            var retro = style == UiSynthStyle.Retro;
            switch (sound)
            {
                case UiSound.Click:
                    length = Tone(samples, 0, 0.028, 1700, 1150, 0.55, retro);
                    Noise(samples, 0, 0.004, 0.25, 1);
                    break;
                case UiSound.Hover:
                    length = Tone(samples, 0, 0.018, 2600, 2600, 0.3, retro);
                    break;
                case UiSound.FocusMove:
                    length = Tone(samples, 0, 0.026, 1500, 1650, 0.4, retro);
                    break;
                case UiSound.Select:
                    length = Tone(samples, 0, 0.032, 1250, 1650, 0.45, retro);
                    break;
                case UiSound.ToggleOn:
                    Tone(samples, 0, 0.032, 880, 880, 0.45, retro);
                    length = Tone(samples, 0.036, 0.04, 1320, 1320, 0.45, retro);
                    break;
                case UiSound.ToggleOff:
                    Tone(samples, 0, 0.032, 1320, 1320, 0.4, retro);
                    length = Tone(samples, 0.036, 0.04, 880, 880, 0.4, retro);
                    break;
                case UiSound.Open:
                    length = Tone(samples, 0, 0.085, 520, 1150, 0.4, retro, attack: 0.012);
                    break;
                case UiSound.Close:
                    length = Tone(samples, 0, 0.07, 1050, 480, 0.35, retro, attack: 0.006);
                    break;
                case UiSound.Back:
                    Tone(samples, 0, 0.035, 760, 760, 0.4, retro);
                    length = Tone(samples, 0.04, 0.045, 500, 500, 0.4, retro);
                    break;
                case UiSound.Error:
                    Tone(samples, 0, 0.06, 196, 196, 0.45, square: true);
                    length = Tone(samples, 0.075, 0.07, 185, 185, 0.45, square: true);
                    break;
                case UiSound.Tick:
                    length = Tone(samples, 0, 0.008, 3400, 3000, 0.35, retro);
                    break;
                case UiSound.Type:
                    length = Tone(samples, 0, 0.012, 2900, 2500, 0.18, retro);
                    Noise(samples, 0, 0.01, 0.22, 7);
                    break;
            }

            var bytes = new byte[length * 2];
            for (var i = 0; i < length; i++)
            {
                var value = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[i * 2] = (byte)value;
                bytes[i * 2 + 1] = (byte)(value >> 8);
            }

            return bytes;
        }

        // Adds a tone gliding from `from` to `to` Hz with a quick attack and an exponential tail; returns
        // the sample index where it ends.
        private static int Tone(float[] samples, double start, double seconds, double from, double to, double amplitude, bool square, double attack = 0.002)
        {
            var first = (int)(start * SampleRate);
            var count = (int)(seconds * SampleRate);
            var phase = 0.0;
            for (var i = 0; i < count && first + i < samples.Length; i++)
            {
                var t = i / (double)SampleRate;
                var progress = i / (double)count;
                var frequency = from + (to - from) * progress;
                phase += 2 * Math.PI * frequency / SampleRate;
                var envelope = Math.Min(1, t / attack) * Math.Exp(-5 * progress) * (1 - progress);
                double wave;
                if (square)
                    wave = (Math.Sin(phase) >= 0 ? 1 : -1) * 0.45;
                else
                    // A touch of the octave gives a sine some body without harshness.
                    wave = Math.Sin(phase) + 0.18 * Math.Sin(2 * phase);
                samples[first + i] += (float)(wave * envelope * amplitude);
            }

            return Math.Min(samples.Length, first + count);
        }

        private static void Noise(float[] samples, double start, double seconds, double amplitude, uint seed)
        {
            var first = (int)(start * SampleRate);
            var count = (int)(seconds * SampleRate);
            var x = 0x12345u * seed + 1;
            var previous = 0.0;
            for (var i = 0; i < count && first + i < samples.Length; i++)
            {
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                var white = (x & 0xFFFF) / 32768.0 - 1;
                // One-pole low-pass: a soft "tk" instead of hiss.
                previous += (white - previous) * 0.45;
                var envelope = 1 - i / (double)count;
                samples[first + i] += (float)(previous * envelope * envelope * amplitude);
            }
        }
    }
}

using System;

namespace MonoGame.PortableUI.Audio
{
    /// <summary>The character of the synthesized UI sounds (<see cref="UiSoundSet.Style"/>).</summary>
    public enum UiSynthStyle
    {
        /// <summary>Soft sine clicks and blips with a little body (modern and flat themes).</summary>
        Soft,
        /// <summary>Square-wave bleeps (8-bit and pixel-art themes).</summary>
        Retro,
        /// <summary>Bright, bell-like pings with a short shimmer (glass and translucent themes).</summary>
        Glass,
        /// <summary>Clacky key switches and relays (terminal and text-mode themes).</summary>
        Mechanical
    }

    /// <summary>
    ///     Renders the built-in UI sounds as 16-bit mono PCM, so an app has sound without shipping audio
    ///     files. Short (6-180 ms), quiet and dry, made for UI: they sit under game audio.
    /// </summary>
    public static class UiSoundSynth
    {
        public const int SampleRate = 44100;

        private enum Wave
        {
            Sine,
            Square,
            Bell
        }

        /// <summary>The sound for <paramref name="sound"/> as little-endian 16-bit mono PCM at <see cref="SampleRate"/>.</summary>
        public static byte[] Render(UiSound sound, UiSynthStyle style = UiSynthStyle.Soft)
        {
            var samples = new float[(int)(SampleRate * 0.2)];
            var length = style == UiSynthStyle.Mechanical ? RenderMechanical(samples, sound) : RenderTonal(samples, sound, style);

            var bytes = new byte[length * 2];
            for (var i = 0; i < length; i++)
            {
                var value = (short)(Math.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[i * 2] = (byte)value;
                bytes[i * 2 + 1] = (byte)(value >> 8);
            }

            return bytes;
        }

        private static int RenderTonal(float[] s, UiSound sound, UiSynthStyle style)
        {
            var wave = style switch
            {
                UiSynthStyle.Retro => Wave.Square,
                UiSynthStyle.Glass => Wave.Bell,
                _ => Wave.Sine
            };
            // Glass: higher and longer, the bell's partials ring out.
            var f = style == UiSynthStyle.Glass ? 1.35 : 1.0;
            var d = style == UiSynthStyle.Glass ? 1.6 : 1.0;
            var decay = style == UiSynthStyle.Glass ? 3.5 : 5.0;
            switch (sound)
            {
                case UiSound.Click:
                {
                    var end = Tone(s, 0, 0.028 * d, 1700 * f, 1150 * f, 0.55, wave, decay);
                    if (style != UiSynthStyle.Glass)
                        Noise(s, 0, 0.004, 0.25, 1);
                    return end;
                }
                case UiSound.Hover:
                    return Tone(s, 0, 0.018 * d, 2600 * f, 2600 * f, 0.3, wave, decay);
                case UiSound.FocusMove:
                    return Tone(s, 0, 0.026 * d, 1500 * f, 1650 * f, 0.4, wave, decay);
                case UiSound.Select:
                    return Tone(s, 0, 0.032 * d, 1250 * f, 1650 * f, 0.45, wave, decay);
                case UiSound.ToggleOn:
                    Tone(s, 0, 0.032 * d, 880 * f, 880 * f, 0.45, wave, decay);
                    return Tone(s, 0.036 * d, 0.04 * d, 1320 * f, 1320 * f, 0.45, wave, decay);
                case UiSound.ToggleOff:
                    Tone(s, 0, 0.032 * d, 1320 * f, 1320 * f, 0.4, wave, decay);
                    return Tone(s, 0.036 * d, 0.04 * d, 880 * f, 880 * f, 0.4, wave, decay);
                case UiSound.Open:
                    return Tone(s, 0, 0.085 * d, 520 * f, 1150 * f, 0.4, wave, decay, attack: 0.012);
                case UiSound.Close:
                    return Tone(s, 0, 0.07 * d, 1050 * f, 480 * f, 0.35, wave, decay, attack: 0.006);
                case UiSound.Back:
                    Tone(s, 0, 0.035 * d, 760 * f, 760 * f, 0.4, wave, decay);
                    return Tone(s, 0.04 * d, 0.045 * d, 500 * f, 500 * f, 0.4, wave, decay);
                case UiSound.Error:
                    Tone(s, 0, 0.06, 196, 196, 0.45, Wave.Square, 5);
                    return Tone(s, 0.075, 0.07, 185, 185, 0.45, Wave.Square, 5);
                case UiSound.Tick:
                    return Tone(s, 0, 0.008 * d, 3400 * f, 3000 * f, 0.35, wave, decay);
                case UiSound.Type:
                {
                    var end = Tone(s, 0, 0.012 * d, 2900 * f, 2500 * f, 0.18, wave, decay);
                    Noise(s, 0, 0.01, 0.22, 7);
                    return end;
                }
                default:
                    return 0;
            }
        }

        // Key switches, relays and a little thock: noise transients over a low body.
        private static int RenderMechanical(float[] s, UiSound sound)
        {
            switch (sound)
            {
                case UiSound.Click:
                    // Press and release of a key switch.
                    Clack(s, 0, 0.6, 3);
                    return Clack(s, 0.045, 0.35, 5);
                case UiSound.Hover:
                    return Clack(s, 0, 0.18, 11, body: false);
                case UiSound.FocusMove:
                    return Clack(s, 0, 0.3, 13, body: false);
                case UiSound.Select:
                    return Clack(s, 0, 0.45, 17);
                case UiSound.ToggleOn:
                    Clack(s, 0, 0.45, 19);
                    return Tone(s, 0.02, 0.05, 1400, 1400, 0.2, Wave.Square, 6);
                case UiSound.ToggleOff:
                    Clack(s, 0, 0.45, 23);
                    return Tone(s, 0.02, 0.05, 700, 700, 0.2, Wave.Square, 6);
                case UiSound.Open:
                    // A relay pulling in.
                    Clack(s, 0, 0.4, 29);
                    return Tone(s, 0.01, 0.07, 120, 160, 0.35, Wave.Square, 4);
                case UiSound.Close:
                    Clack(s, 0, 0.35, 31);
                    return Tone(s, 0.01, 0.06, 160, 110, 0.3, Wave.Square, 4);
                case UiSound.Back:
                    Clack(s, 0, 0.4, 37);
                    return Clack(s, 0.06, 0.3, 41);
                case UiSound.Error:
                    Tone(s, 0, 0.06, 196, 196, 0.45, Wave.Square, 5);
                    return Tone(s, 0.075, 0.07, 185, 185, 0.45, Wave.Square, 5);
                case UiSound.Tick:
                    return Clack(s, 0, 0.3, 43, body: false);
                case UiSound.Type:
                    return Clack(s, 0, 0.35, 47);
                default:
                    return 0;
            }
        }

        private static int Clack(float[] s, double start, double amplitude, uint seed, bool body = true)
        {
            var end = Noise(s, start, 0.012, amplitude, seed, smoothing: 0.7);
            if (body)
                end = Math.Max(end, Tone(s, start, 0.03, 210, 150, amplitude * 0.5, Wave.Sine, 6));
            return end;
        }

        // Adds a tone gliding from `from` to `to` Hz with a quick attack and an exponential tail; returns
        // the sample index where it ends.
        private static int Tone(float[] samples, double start, double seconds, double from, double to, double amplitude, Wave wave, double decay, double attack = 0.002)
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
                var envelope = Math.Min(1, t / attack) * Math.Exp(-decay * progress) * (1 - progress);
                var value = wave switch
                {
                    Wave.Square => (Math.Sin(phase) >= 0 ? 1 : -1) * 0.45,
                    // Inharmonic partials of a small bell; the upper ones fade first.
                    Wave.Bell => Math.Sin(phase) + 0.35 * Math.Sin(2.76 * phase) * (1 - progress) + 0.15 * Math.Sin(5.4 * phase) * (1 - progress) * (1 - progress),
                    // A touch of the octave gives a sine some body without harshness.
                    _ => Math.Sin(phase) + 0.18 * Math.Sin(2 * phase)
                };
                samples[first + i] += (float)(value * envelope * amplitude);
            }

            return Math.Min(samples.Length, first + count);
        }

        private static int Noise(float[] samples, double start, double seconds, double amplitude, uint seed, double smoothing = 0.45)
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
                previous += (white - previous) * smoothing;
                var envelope = 1 - i / (double)count;
                samples[first + i] += (float)(previous * envelope * envelope * amplitude);
            }

            return Math.Min(samples.Length, first + count);
        }
    }
}

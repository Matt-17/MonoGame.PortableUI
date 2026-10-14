using System;
using MonoGame.PortableUI.Audio;

namespace MonoGame.PortableUI.Haptics
{
    /// <summary>
    ///     How a theme feels (<see cref="PortableTheme.Haptics"/>): the pulse for each UI event, an overall
    ///     <see cref="Intensity"/> and, for gamepad rumble, a <see cref="Length"/> factor. Android plays
    ///     its own system pulses, so there only the choice of pulse and the strength count. Immutable:
    ///     build variations with <see cref="With(UiSound, UiHaptic)"/> and <see cref="With(float?, float?)"/>.
    /// </summary>
    public sealed class UiHapticSet
    {
        private readonly UiHaptic[] _map;

        private UiHapticSet(UiHaptic[] map, float intensity, float length)
        {
            _map = map;
            Intensity = Math.Clamp(intensity, 0, 1);
            Length = Math.Clamp(length, 0.25f, 4);
        }

        /// <summary>Clicks and toggles click, steps, focus moves and selections tick, popups and back tap,
        /// errors buzz twice; hover, close and typing stay still.</summary>
        public static UiHapticSet Default { get; } = new UiHapticSet(DefaultMap(), 1, 1);

        /// <summary>Light and short: everything one step softer (glass and translucent themes).</summary>
        public static UiHapticSet Subtle { get; } = Default
            .With(UiSound.Click, UiHaptic.Tap)
            .With(UiSound.ToggleOn, UiHaptic.Tap)
            .With(UiSound.ToggleOff, UiHaptic.Tap)
            .With(UiSound.Open, UiHaptic.Tick)
            .With(UiSound.Back, UiHaptic.Tick)
            .With(intensity: 0.7f, length: 0.8f);

        /// <summary>Firm and a little long, like an old console pad: toggles and opening thump (retro and pixel-art themes).</summary>
        public static UiHapticSet Punchy { get; } = Default
            .With(UiSound.ToggleOn, UiHaptic.Heavy)
            .With(UiSound.ToggleOff, UiHaptic.Heavy)
            .With(UiSound.Open, UiHaptic.Click)
            .With(UiSound.Back, UiHaptic.Click)
            .With(length: 1.3f);

        /// <summary>Every key press felt: focus moves and selections tap, typing ticks (terminal themes).</summary>
        public static UiHapticSet Clacky { get; } = Default
            .With(UiSound.FocusMove, UiHaptic.Tap)
            .With(UiSound.Select, UiHaptic.Tap)
            .With(UiSound.Type, UiHaptic.Tick)
            .With(length: 0.9f);

        /// <summary>No haptics at all.</summary>
        public static UiHapticSet None { get; } = new UiHapticSet(new UiHaptic[UiSoundService.Count], 0, 1);

        /// <summary>Strength factor 0..1 for the set.</summary>
        public float Intensity { get; }

        /// <summary>Length factor for gamepad rumble pulses (1 = default; Android ignores it).</summary>
        public float Length { get; }

        public UiHaptic Get(UiSound sound) => _map[(int)sound];

        /// <summary>A copy that plays <paramref name="haptic"/> for <paramref name="sound"/>.</summary>
        public UiHapticSet With(UiSound sound, UiHaptic haptic)
        {
            var map = (UiHaptic[])_map.Clone();
            map[(int)sound] = haptic;
            return new UiHapticSet(map, Intensity, Length);
        }

        /// <summary>A copy with another strength and rumble length.</summary>
        public UiHapticSet With(float? intensity = null, float? length = null)
            => new UiHapticSet(_map, intensity ?? Intensity, length ?? Length);

        private static UiHaptic[] DefaultMap()
        {
            var map = new UiHaptic[UiSoundService.Count];
            map[(int)UiSound.Click] = UiHaptic.Click;
            map[(int)UiSound.ToggleOn] = UiHaptic.Click;
            map[(int)UiSound.ToggleOff] = UiHaptic.Click;
            map[(int)UiSound.Select] = UiHaptic.Tick;
            map[(int)UiSound.FocusMove] = UiHaptic.Tick;
            map[(int)UiSound.Tick] = UiHaptic.Tick;
            map[(int)UiSound.Open] = UiHaptic.Tap;
            map[(int)UiSound.Back] = UiHaptic.Tap;
            map[(int)UiSound.Error] = UiHaptic.Double;
            // Hover has no device to buzz; close follows a choice; the system keyboard has its own typing feedback.
            return map;
        }
    }
}

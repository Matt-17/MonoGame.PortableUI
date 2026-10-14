using System;
using System.Collections.Generic;
using MonoGame.PortableUI.Audio;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Haptics
{
    /// <summary>The strength and shape of a haptic pulse, from the faintest to an error buzz.</summary>
    public enum UiHaptic
    {
        None,
        /// <summary>A faint detent (slider step, focus move, list selection).</summary>
        Tick,
        /// <summary>A light tap (opening a popup, back).</summary>
        Tap,
        /// <summary>A key press (button, toggle).</summary>
        Click,
        /// <summary>A firm thump.</summary>
        Heavy,
        /// <summary>Two quick pulses (a rejected input).</summary>
        Double
    }

    /// <summary>The kind of input the user last used; haptics answer on that device.</summary>
    public enum UiInputDevice
    {
        None,
        Mouse,
        Touch,
        Keyboard,
        GamePad
    }

    /// <summary>
    ///     A haptic backend: <see cref="GamePadRumbleHaptics"/> (gamepad motors), <c>AndroidHaptics</c> (the
    ///     phone's haptic feedback). Called on the game thread.
    /// </summary>
    public interface IUiHapticPlayer
    {
        /// <summary>Whether this backend answers input from <paramref name="device"/> (a phone vibrates
        /// for touch, a gamepad rumbles for gamepad input).</summary>
        bool Supports(UiInputDevice device);

        /// <param name="haptic">The pulse.</param>
        /// <param name="intensity">0..1 (backends without strength control ignore it).</param>
        /// <param name="length">Length factor of the pulse (1 = default; system patterns ignore it).</param>
        void Play(UiHaptic haptic, float intensity, float length);

        /// <summary>Every engine update: backends that time their own pulses (rumble) stop them here.</summary>
        void Update(TimeSpan now);

        /// <summary>True while a pulse this backend times itself is running: the engine keeps updating.</summary>
        bool IsActive { get; }
    }

    /// <summary>
    ///     Haptic feedback for the same events as <see cref="UiSoundService"/> (<c>engine.Haptics</c>),
    ///     independent of sound: on the device the user is using - the phone for touch, the gamepad for
    ///     gamepad input, nothing for mouse and keyboard. Add backends to <see cref="Players"/>.
    ///     <c>control.Sounds.MuteHaptics</c> silences a subtree. Which pulse an event gets comes from the theme
    ///     of its control (<see cref="PortableTheme.Haptics"/>) unless the engine overrides it.
    /// </summary>
    public sealed class UiHapticService
    {
        // Engine-level choices per event; null = the theme's set decides.
        private readonly UiHaptic?[] _overrides = new UiHaptic?[UiSoundService.Count];
        private readonly TimeSpan[] _lastPlayed = new TimeSpan[(int)UiHaptic.Double + 1];
        private float _intensity = 1;

        private readonly ScreenEngine? _engine;

        internal UiHapticService(ScreenEngine? engine = null)
        {
            _engine = engine;
            for (var i = 0; i < _lastPlayed.Length; i++)
                _lastPlayed[i] = TimeSpan.MinValue;
        }

        /// <summary>The backends; each answers the input devices it <see cref="IUiHapticPlayer.Supports"/>.</summary>
        public IList<IUiHapticPlayer> Players { get; } = new List<IUiHapticPlayer>();

        public bool IsEnabled { get; set; } = true;

        /// <summary>Overall strength 0..1.</summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Math.Clamp(value, 0, 1);
        }

        /// <summary>The pulse for an event in this engine: an engine choice (<see cref="SetHaptic"/>), else the
        /// app theme's <see cref="PortableTheme.Haptics"/> set. Controls follow their own theme (islands).</summary>
        public UiHaptic GetHaptic(UiSound sound) => _overrides[(int)sound] ?? ThemeSet(null).Get(sound);

        /// <summary>Overrides the pulse of an event for every theme of this engine (<see cref="ClearHaptic"/> undoes it).</summary>
        public void SetHaptic(UiSound sound, UiHaptic haptic) => _overrides[(int)sound] = haptic;

        /// <summary>Lets the theme decide the pulse of <paramref name="sound"/> again.</summary>
        public void ClearHaptic(UiSound sound) => _overrides[(int)sound] = null;

        private UiHapticSet ThemeSet(Control? source)
        {
            var theme = source != null ? source.ResolveTheme() : _engine?.Options.Theme ?? PortableTheme.ResolveCurrent();
            return theme.ResolveHaptics();
        }

        /// <summary>Fires the pulse of <paramref name="sound"/> for <paramref name="source"/> on <paramref name="device"/>.</summary>
        public bool Play(UiSound sound, Control? source, UiInputDevice device)
        {
            if (!IsEnabled || _intensity <= 0 || Players.Count == 0 || device == UiInputDevice.None)
                return false;

            var set = ThemeSet(source);
            var haptic = _overrides[(int)sound] ?? set.Get(sound);
            var intensity = _intensity * set.Intensity;
            if (haptic == UiHaptic.None || intensity <= 0)
                return false;

            for (var element = (FrameworkElement?)source; element != null; element = element.Parent)
            {
                if (element is Control { SoundOverrides: { MuteHaptics: true } })
                    return false;
            }

            // A detent every frame of a fast drag would blur into a buzz: at most one pulse per 40 ms.
            var now = Common.ScreenSystem.TotalTime;
            var last = _lastPlayed[(int)haptic];
            if (last != TimeSpan.MinValue && now >= last && now - last < TimeSpan.FromMilliseconds(40))
                return false;

            var played = false;
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                if (!player.Supports(device))
                    continue;
                player.Play(haptic, intensity, set.Length);
                played = true;
            }

            if (played)
            {
                _lastPlayed[(int)haptic] = now;
                _engine?.RequestRedraw();
            }
            return played;
        }

        internal void Update(TimeSpan now)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                player.Update(now);
                // A rumble must stop on time even when nothing else asks for frames (OnDemand).
                if (player.IsActive)
                    _engine?.RequestRedraw();
            }
        }
    }
}

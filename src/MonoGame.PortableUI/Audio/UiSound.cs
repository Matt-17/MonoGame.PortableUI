using System;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;

namespace MonoGame.PortableUI.Audio
{
    /// <summary>The semantic UI sound events controls raise (the player decides what each sounds like).</summary>
    public enum UiSound
    {
        /// <summary>A button (or any control with a Click handler) was activated.</summary>
        Click,
        /// <summary>The mouse moved onto something clickable.</summary>
        Hover,
        /// <summary>Keyboard or gamepad moved the focus.</summary>
        FocusMove,
        /// <summary>An item was selected from the keyboard (list, tabs).</summary>
        Select,
        ToggleOn,
        ToggleOff,
        /// <summary>A popup, drop-down, menu or dialog opened.</summary>
        Open,
        /// <summary>A popup, drop-down, menu or dialog closed.</summary>
        Close,
        /// <summary>Escape / gamepad B asked to go back.</summary>
        Back,
        /// <summary>An input was rejected (a full text box); apps play it for their own errors.</summary>
        Error,
        /// <summary>A slider moved one step.</summary>
        Tick,
        /// <summary>Text was typed or deleted.</summary>
        Type
    }

    /// <summary>
    ///     Plays UI sounds - the backend of <see cref="UiSoundService"/>. <see cref="SoundEffectUiSoundPlayer"/>
    ///     plays MonoGame <c>SoundEffect</c>s; implement it for another audio engine (FMOD, Wwise) or to
    ///     route UI sounds into a game's own mixer. Called on the game thread; must not allocate per call.
    /// </summary>
    public interface IUiSoundPlayer
    {
        /// <param name="sound">The event to sound.</param>
        /// <param name="volume">0..1, already mixed (master, per sound, per control).</param>
        /// <param name="pitch">-1..1 in octaves, like <c>SoundEffectInstance.Pitch</c>.</param>
        /// <param name="pan">-1 (left) .. 1 (right).</param>
        void Play(UiSound sound, float volume, float pitch, float pan);
    }

    /// <summary>
    ///     Per-control sound settings (<c>control.Sounds</c>); they apply to the control and everything inside
    ///     it, the nearest setting winning for <see cref="Map"/>, volumes multiplying.
    /// </summary>
    public sealed class UiSoundOverrides
    {
        private const int None = -1;
        private const int Silent = -2;
        private readonly int[] _map = CreateMap();

        /// <summary>Silences this control and its subtree.</summary>
        public bool IsMuted { get; set; }

        /// <summary>No haptic feedback (vibration, rumble) from this control and its subtree; sounds still play.</summary>
        public bool MuteHaptics { get; set; }

        /// <summary>Volume factor for this subtree (default 1).</summary>
        public float Volume { get; set; } = 1;

        /// <summary>Pitch shift in octaves for this subtree (e.g. 0.2 for a higher "small" button).</summary>
        public float Pitch { get; set; }

        /// <summary>Plays <paramref name="to"/> instead of <paramref name="from"/> here; null silences that event only.</summary>
        public UiSoundOverrides Map(UiSound from, UiSound? to)
        {
            _map[(int)from] = to.HasValue ? (int)to.Value : Silent;
            return this;
        }

        /// <summary>Removes a <see cref="Map"/> entry.</summary>
        public UiSoundOverrides Unmap(UiSound from)
        {
            _map[(int)from] = None;
            return this;
        }

        /// <summary>True when this level remaps <paramref name="sound"/>; <paramref name="mapped"/> null = silent.</summary>
        internal bool TryMap(UiSound sound, out UiSound? mapped)
        {
            var value = _map[(int)sound];
            mapped = value >= 0 ? (UiSound)value : null;
            return value != None;
        }

        private static int[] CreateMap()
        {
            var map = new int[UiSoundService.Count];
            for (var i = 0; i < map.Length; i++)
                map[i] = None;
            return map;
        }
    }

    /// <summary>
    ///     The UI sounds of one <see cref="ScreenEngine"/> (<c>engine.Sounds</c>). Controls raise events
    ///     (click, hover, focus, toggles, popups, back, slider ticks, typing); this mixes them (master and
    ///     per-sound volume, per-control <see cref="UiSoundOverrides"/>), rate-limits repeats and hands
    ///     them to the <see cref="Player"/>. Without a player nothing plays.
    /// </summary>
    public sealed class UiSoundService
    {
        internal const int Count = (int)UiSound.Type + 1;

        private readonly float[] _volumes = new float[Count];
        private readonly TimeSpan[] _minIntervals = new TimeSpan[Count];
        private readonly TimeSpan[] _lastPlayed = new TimeSpan[Count];
        private TimeSpan _lastActivation = TimeSpan.MinValue;
        private float _masterVolume = 1;
        private uint _random = 0x9E3779B9;

        private readonly ScreenEngine? _engine;

        internal UiSoundService(ScreenEngine? engine = null)
        {
            _engine = engine;
            for (var i = 0; i < Count; i++)
            {
                _volumes[i] = 0.8f;
                _minIntervals[i] = TimeSpan.FromMilliseconds(30);
                _lastPlayed[i] = TimeSpan.MinValue;
            }

            SetVolume(UiSound.Hover, 0.35f);
            SetVolume(UiSound.FocusMove, 0.5f);
            SetVolume(UiSound.Tick, 0.45f);
            SetVolume(UiSound.Type, 0.4f);
            SetMinInterval(UiSound.Hover, TimeSpan.FromMilliseconds(60));
            SetMinInterval(UiSound.Tick, TimeSpan.FromMilliseconds(35));
            SetMinInterval(UiSound.Type, TimeSpan.FromMilliseconds(25));
            SetMinInterval(UiSound.FocusMove, TimeSpan.FromMilliseconds(40));
        }

        /// <summary>A fixed backend for every sound of this engine; it wins over <see cref="UseThemeSounds"/>.
        /// <see cref="SoundEffectUiSoundPlayer.CreateDefault"/> gives a built-in synthesized set.</summary>
        public IUiSoundPlayer? Player { get; set; }

        /// <summary>
        ///     Plays each sound in the set of the theme that styles its control (<see cref="PortableTheme.Sounds"/>;
        ///     a <c>ThemeIsland</c> sounds like its own theme) when no <see cref="Player"/> is set. The players
        ///     are synthesized once per set and shared. Off by default.
        /// </summary>
        public bool UseThemeSounds { get; set; }

        /// <summary>Master volume 0..1 for all UI sounds of this engine.</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Math.Clamp(value, 0, 1);
        }

        public bool IsMuted { get; set; }

        /// <summary>Volume 0..1 of one event in the mix (defaults: hover, focus, tick and typing quieter).</summary>
        public float GetVolume(UiSound sound) => _volumes[(int)sound];

        public void SetVolume(UiSound sound, float volume) => _volumes[(int)sound] = Math.Clamp(volume, 0, 1);

        /// <summary>The same event plays at most once per interval (hover sweeps, slider drags, key repeat).</summary>
        public TimeSpan GetMinInterval(UiSound sound) => _minIntervals[(int)sound];

        public void SetMinInterval(UiSound sound, TimeSpan interval) => _minIntervals[(int)sound] = interval < TimeSpan.Zero ? TimeSpan.Zero : interval;

        /// <summary>
        ///     Plays <paramref name="sound"/> for <paramref name="source"/> (its <c>Sounds</c> overrides and those
        ///     of its ancestors apply; its position pans slightly). True when it was handed to the player.
        /// </summary>
        public bool Play(UiSound sound, Control? source = null)
        {
            if (IsMuted || _masterVolume <= 0)
                return false;

            var volume = 1f;
            var pitch = 0f;
            var player = Player;
            if (player == null)
            {
                if (!UseThemeSounds)
                    return false;
                var theme = source != null ? source.ResolveTheme() : _engine?.Options.Theme ?? PortableTheme.ResolveCurrent();
                var set = theme.ResolveSounds();
                player = UiSoundSet.GetPlayer(set);
                if (player == null)
                    return false;
                volume = set.Volume;
                pitch = set.Pitch;
            }

            var mapped = false;
            for (var element = (FrameworkElement?)source; element != null; element = element.Parent)
            {
                if (element is not Control { SoundOverrides: { } overrides })
                    continue;
                if (overrides.IsMuted)
                    return false;
                if (!mapped && overrides.TryMap(sound, out var to))
                {
                    if (to == null)
                        return false;
                    sound = to.Value;
                    mapped = true;
                }

                volume *= overrides.Volume;
                pitch += overrides.Pitch;
            }

            var now = ScreenSystem.TotalTime;
            var index = (int)sound;
            if (_lastPlayed[index] != TimeSpan.MinValue && now - _lastPlayed[index] < _minIntervals[index] && now >= _lastPlayed[index])
                return false;
            // Choosing a menu item or a drop-down entry closes the popup: the choice is the sound.
            if (sound == UiSound.Close && _lastActivation != TimeSpan.MinValue && now - _lastActivation < TimeSpan.FromMilliseconds(80) && now >= _lastActivation)
                return false;

            volume *= _volumes[index] * _masterVolume;
            if (volume <= 0)
                return false;

            _lastPlayed[index] = now;
            if (sound is UiSound.Click or UiSound.ToggleOn or UiSound.ToggleOff or UiSound.Select)
                _lastActivation = now;

            // Rapid repeats (typing, ticks, hover) vary a little, so they do not sound like a machine gun.
            if (sound is UiSound.Type or UiSound.Tick or UiSound.Hover)
                pitch += (NextRandom() - 0.5f) * 0.08f;

            player.Play(sound, Math.Min(1, volume), Math.Clamp(pitch, -1, 1), PanOf(source));
            return true;
        }

        // A gentle pan by the control's horizontal position on its screen.
        private static float PanOf(Control? source)
        {
            if (source?.Screen is not { } screen)
                return 0;
            var width = screen.ScreenRect.Width;
            if (width <= 0)
                return 0;
            var rect = source.ClippingRect;
            var x = (rect.Left + rect.Width / 2 - screen.ScreenRect.Left) / width;
            return Math.Clamp((x * 2 - 1) * 0.25f, -0.25f, 0.25f);
        }

        private float NextRandom()
        {
            // xorshift32: no allocation, good enough for pitch jitter.
            var x = _random;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _random = x;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }
    }
}

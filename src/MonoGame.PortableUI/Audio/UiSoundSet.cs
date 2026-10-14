using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace MonoGame.PortableUI.Audio
{
    /// <summary>
    ///     What a theme sounds like (<see cref="PortableTheme.Sounds"/>): a synthesized <see cref="Style"/>,
    ///     shifted in <see cref="Pitch"/> and scaled in <see cref="Volume"/>, optionally with your own
    ///     <see cref="SoundEffect"/> for single events. Played when <see cref="UiSoundService.UseThemeSounds"/>
    ///     is on; the engine builds one player per set and shares it. Immutable: build variations with
    ///     <see cref="With"/> / <see cref="WithEffect"/>.
    /// </summary>
    public sealed class UiSoundSet
    {
        private readonly Dictionary<UiSound, SoundEffect>? _effects;

        public UiSoundSet(UiSynthStyle style, float pitch = 0, float volume = 1)
            : this(style, pitch, volume, null)
        {
        }

        private UiSoundSet(UiSynthStyle style, float pitch, float volume, Dictionary<UiSound, SoundEffect>? effects)
        {
            Style = style;
            Pitch = Math.Clamp(pitch, -1, 1);
            Volume = Math.Clamp(volume, 0, 1);
            _effects = effects;
        }

        /// <summary>Soft sine clicks: the default for modern, flat and desktop themes.</summary>
        public static UiSoundSet Soft { get; } = new UiSoundSet(UiSynthStyle.Soft);

        /// <summary>Square-wave bleeps: the default for pixel-art themes (<see cref="PortableTheme.PixelMotion"/>).</summary>
        public static UiSoundSet Retro { get; } = new UiSoundSet(UiSynthStyle.Retro, volume: 0.7f);

        /// <summary>Bell-like pings for glass themes.</summary>
        public static UiSoundSet Glass { get; } = new UiSoundSet(UiSynthStyle.Glass, volume: 0.8f);

        /// <summary>Key switches and relays for terminal themes.</summary>
        public static UiSoundSet Mechanical { get; } = new UiSoundSet(UiSynthStyle.Mechanical);

        public UiSynthStyle Style { get; }

        /// <summary>Pitch shift in octaves (-1..1) for every sound of the set.</summary>
        public float Pitch { get; }

        /// <summary>Volume factor 0..1 for the set (sets differ in loudness).</summary>
        public float Volume { get; }

        /// <summary>Your own effect for <paramref name="sound"/>, or null for the synthesized one.</summary>
        public SoundEffect? GetEffect(UiSound sound) => _effects != null && _effects.TryGetValue(sound, out var effect) ? effect : null;

        /// <summary>A copy with another pitch and volume.</summary>
        public UiSoundSet With(float? pitch = null, float? volume = null)
            => new UiSoundSet(Style, pitch ?? Pitch, volume ?? Volume, _effects);

        /// <summary>A copy that plays <paramref name="effect"/> for <paramref name="sound"/> (the caller owns it).</summary>
        public UiSoundSet WithEffect(UiSound sound, SoundEffect effect)
        {
            var effects = _effects != null ? new Dictionary<UiSound, SoundEffect>(_effects) : new Dictionary<UiSound, SoundEffect>();
            effects[sound] = effect ?? throw new ArgumentNullException(nameof(effect));
            return new UiSoundSet(Style, Pitch, Volume, effects);
        }

        // One player per set, shared by every engine (audio is not tied to a graphics device).
        private static readonly Dictionary<UiSoundSet, IUiSoundPlayer?> Players = new Dictionary<UiSoundSet, IUiSoundPlayer?>();
        private static readonly object PlayersLock = new object();

        /// <summary>Builds players for sets (tests swap in a recorder).</summary>
        internal static Func<UiSoundSet, IUiSoundPlayer?> PlayerFactory = CreatePlayer;

        /// <summary>The shared player of <paramref name="set"/>; null without an audio device.</summary>
        internal static IUiSoundPlayer? GetPlayer(UiSoundSet set)
        {
            lock (PlayersLock)
            {
                if (!Players.TryGetValue(set, out var player))
                {
                    player = PlayerFactory(set);
                    Players[set] = player;
                }

                return player;
            }
        }

        internal static void ClearPlayers()
        {
            lock (PlayersLock)
            {
                foreach (var player in Players.Values)
                    (player as IDisposable)?.Dispose();
                Players.Clear();
            }
        }

        private static IUiSoundPlayer? CreatePlayer(UiSoundSet set)
        {
            var player = SoundEffectUiSoundPlayer.CreateDefault(set.Style);
            if (player == null || set._effects == null)
                return player;
            foreach (var pair in set._effects)
                player[pair.Key] = pair.Value;
            return player;
        }
    }
}

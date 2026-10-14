using System;
using Microsoft.Xna.Framework.Audio;

namespace MonoGame.PortableUI.Audio
{
    /// <summary>
    ///     Plays UI sounds through MonoGame <see cref="SoundEffect"/>s. Each sound gets a few pre-created
    ///     voices that are reused round-robin, so playing allocates nothing and quick repeats overlap
    ///     instead of cutting each other off.
    ///     <code>engine.Sounds.Player = SoundEffectUiSoundPlayer.CreateDefault();          // built-in set
    /// player[UiSound.Click] = Content.Load&lt;SoundEffect&gt;("ui/click");               // or your own</code>
    /// </summary>
    public sealed class SoundEffectUiSoundPlayer : IUiSoundPlayer, IDisposable
    {
        private readonly SoundEffect?[] _effects = new SoundEffect?[UiSoundService.Count];
        private readonly SoundEffectInstance[]?[] _voices = new SoundEffectInstance[UiSoundService.Count][];
        private readonly int[] _next = new int[UiSoundService.Count];
        private readonly bool[] _owned = new bool[UiSoundService.Count];

        /// <param name="voicesPerSound">How many copies of one sound can sound at once.</param>
        public SoundEffectUiSoundPlayer(int voicesPerSound = 3)
        {
            VoicesPerSound = Math.Max(1, voicesPerSound);
        }

        public int VoicesPerSound { get; }

        /// <summary>The effect for <paramref name="sound"/> (null = silent). The caller keeps owning effects it sets.</summary>
        public SoundEffect? this[UiSound sound]
        {
            get => _effects[(int)sound];
            set => Set(sound, value, owned: false);
        }

        /// <summary>
        ///     A player with the built-in synthesized sounds (<see cref="UiSoundSynth"/>) in <paramref name="style"/>;
        ///     it owns and disposes them. Needs an audio device: returns null where none is available
        ///     (headless servers, CI).
        /// </summary>
        public static SoundEffectUiSoundPlayer? CreateDefault(UiSynthStyle style = UiSynthStyle.Soft, int voicesPerSound = 3)
        {
            var player = new SoundEffectUiSoundPlayer(voicesPerSound);
            try
            {
                for (var i = 0; i < UiSoundService.Count; i++)
                {
                    var sound = (UiSound)i;
                    var effect = new SoundEffect(UiSoundSynth.Render(sound, style), UiSoundSynth.SampleRate, AudioChannels.Mono);
                    player.Set(sound, effect, owned: true);
                }
            }
            catch (Exception exception) when (exception is NoAudioHardwareException or InvalidOperationException or TypeInitializationException or DllNotFoundException)
            {
                player.Dispose();
                return null;
            }

            return player;
        }

        public void Play(UiSound sound, float volume, float pitch, float pan)
        {
            var index = (int)sound;
            var voices = _voices[index];
            if (voices == null)
                return;

            var voice = voices[_next[index]];
            _next[index] = (_next[index] + 1) % voices.Length;
            if (voice.State != SoundState.Stopped)
                voice.Stop();
            voice.Volume = Math.Clamp(volume, 0, 1);
            voice.Pitch = Math.Clamp(pitch, -1, 1);
            voice.Pan = Math.Clamp(pan, -1, 1);
            voice.Play();
        }

        private void Set(UiSound sound, SoundEffect? effect, bool owned)
        {
            var index = (int)sound;
            ReleaseVoices(index);
            if (_owned[index])
                _effects[index]?.Dispose();
            _effects[index] = effect;
            _owned[index] = owned && effect != null;
            _next[index] = 0;
            if (effect == null)
                return;

            var voices = new SoundEffectInstance[VoicesPerSound];
            for (var i = 0; i < voices.Length; i++)
                voices[i] = effect.CreateInstance();
            _voices[index] = voices;
        }

        private void ReleaseVoices(int index)
        {
            var voices = _voices[index];
            if (voices == null)
                return;
            foreach (var voice in voices)
                voice.Dispose();
            _voices[index] = null;
        }

        public void Dispose()
        {
            for (var i = 0; i < UiSoundService.Count; i++)
                Set((UiSound)i, null, owned: false);
        }
    }
}

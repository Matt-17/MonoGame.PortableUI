using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Audio;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;
using MonoGame.PortableUI.Themes;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ThemeSoundTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class RecordingPlayer : IUiSoundPlayer
        {
            public RecordingPlayer(UiSoundSet set) => Set = set;

            public UiSoundSet Set { get; }
            public readonly List<(UiSound Sound, float Volume, float Pitch)> Played = new List<(UiSound, float, float)>();

            public void Play(UiSound sound, float volume, float pitch, float pan) => Played.Add((sound, volume, pitch));
        }

        private readonly Dictionary<UiSoundSet, RecordingPlayer> _players = new Dictionary<UiSoundSet, RecordingPlayer>();
        private Func<UiSoundSet, IUiSoundPlayer?>? _previousFactory;
        private Game? _game;

        [TestInitialize]
        public void Setup()
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(10);
            UiSoundSet.ClearPlayers();
            _previousFactory = UiSoundSet.PlayerFactory;
            UiSoundSet.PlayerFactory = set => _players[set] = new RecordingPlayer(set);
        }

        [TestCleanup]
        public void Cleanup()
        {
            UiSoundSet.PlayerFactory = _previousFactory!;
            UiSoundSet.ClearPlayers();
            _game?.Dispose();
        }

        private ScreenEngine Create(Control content, PortableTheme theme)
        {
            _game = new Game();
            var engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false, Theme = theme });
            engine.SetScreenSize(400, 300);
            var screen = new TestScreen { InputSource = new VirtualInputSource(), Content = content };
            engine.NavigateToScreen(screen);
            screen.Update();
            return engine;
        }

        [TestMethod]
        public void A_theme_without_a_set_derives_one()
        {
            Assert.AreSame(UiSoundSet.Soft, new PortableTheme().ResolveSounds());
            Assert.AreSame(UiSoundSet.Retro, new PortableTheme { PixelMotion = true }.ResolveSounds());
            var own = UiSoundSet.Glass.With(pitch: 0.2f);
            Assert.AreSame(own, new PortableTheme { PixelMotion = true, Sounds = own }.ResolveSounds());
        }

        [TestMethod]
        public void Theme_sounds_play_only_when_enabled_and_follow_the_controls_theme()
        {
            var plain = new Button();
            var islandButton = new Button();
            var island = new ThemeIsland { Theme = new PortableTheme { Sounds = UiSoundSet.Mechanical }, Content = islandButton };
            var panel = new StackPanel();
            panel.AddChild(plain);
            panel.AddChild(island);
            var engine = Create(panel, new PortableTheme { Sounds = UiSoundSet.Glass });

            plain.OnClick();
            Assert.AreEqual(0, _players.Count, "off by default");

            engine.Sounds.UseThemeSounds = true;
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(100);
            plain.OnClick();
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(100);
            islandButton.OnClick();

            Assert.AreEqual(1, _players[UiSoundSet.Glass].Played.Count);
            Assert.AreEqual(1, _players[UiSoundSet.Mechanical].Played.Count, "an island sounds like its theme");
            Assert.AreEqual(engine.Sounds.GetVolume(UiSound.Click) * UiSoundSet.Glass.Volume, _players[UiSoundSet.Glass].Played[0].Volume, 0.0001f, "the set's volume mixes in");
        }

        [TestMethod]
        public void A_fixed_player_wins_over_theme_sounds()
        {
            var button = new Button();
            var engine = Create(button, new PortableTheme());
            var fixedPlayer = new RecordingPlayer(UiSoundSet.Soft);
            engine.Sounds.UseThemeSounds = true;
            engine.Sounds.Player = fixedPlayer;
            button.OnClick();
            Assert.AreEqual(1, fixedPlayer.Played.Count);
            Assert.AreEqual(0, _players.Count);
        }

        [TestMethod]
        public void A_sets_pitch_shifts_every_sound()
        {
            var button = new Button();
            var set = UiSoundSet.Soft.With(pitch: 0.3f);
            var engine = Create(button, new PortableTheme { Sounds = set });
            engine.Sounds.UseThemeSounds = true;
            button.OnClick();
            Assert.AreEqual(0.3f, _players[set].Played[0].Pitch, 0.0001f);
        }

        [TestMethod]
        public void Catalog_themes_sound_like_their_era()
        {
            var byId = PortableThemes.All.ToDictionary(t => t.Id, t => t.CreateTheme().ResolveSounds());
            Console.WriteLine(string.Join(Environment.NewLine, byId.OrderBy(p => p.Value.Style).ThenBy(p => p.Key).Select(p => $"{p.Value.Style,-11} {p.Key}")));

            foreach (var definition in PortableThemes.All)
            {
                var sounds = byId[definition.Id];
                switch (definition.Metadata.Era)
                {
                    case ThemeEra.Glass:
                        Assert.AreEqual(UiSynthStyle.Glass, sounds.Style, definition.Id);
                        break;
                    case ThemeEra.Terminal:
                        Assert.AreEqual(UiSynthStyle.Mechanical, sounds.Style, definition.Id);
                        break;
                    case ThemeEra.Retro:
                        Assert.AreEqual(UiSynthStyle.Retro, sounds.Style, definition.Id);
                        break;
                }

                if (definition.CreateTheme().PixelMotion && definition.Metadata.Era != ThemeEra.Terminal)
                    Assert.AreEqual(UiSynthStyle.Retro, sounds.Style, $"{definition.Id}: pixel-art themes bleep");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Audio;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class UiSoundTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class TestControl : Control
        {
        }

        private sealed class RecordingPlayer : IUiSoundPlayer
        {
            public readonly List<(UiSound Sound, float Volume, float Pitch, float Pan)> Played = new List<(UiSound, float, float, float)>();

            public void Play(UiSound sound, float volume, float pitch, float pan) => Played.Add((sound, volume, pitch, pan));

            public UiSound[] Sounds()
            {
                var sounds = new UiSound[Played.Count];
                for (var i = 0; i < sounds.Length; i++)
                    sounds[i] = Played[i].Sound;
                return sounds;
            }
        }

        private Game? _game;

        [TestCleanup]
        public void Cleanup() => _game?.Dispose();

        private (ScreenEngine Engine, TestScreen Screen, VirtualInputSource Source, RecordingPlayer Player) Create(Control content)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(10);
            _game = new Game();
            var engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var player = new RecordingPlayer();
            engine.Sounds.Player = player;
            var screen = new TestScreen();
            var source = new VirtualInputSource();
            screen.InputSource = source;
            screen.Content = content;
            engine.NavigateToScreen(screen);
            screen.Update();
            return (engine, screen, source, player);
        }

        private static void Later(double milliseconds) => ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(milliseconds);

        [TestMethod]
        public void A_button_clicks_and_a_plain_control_only_with_a_click_handler()
        {
            var button = new Button { Content = new TextBlock { Text = "OK" } };
            var plain = new TestControl();
            var panel = new StackPanel();
            panel.AddChild(button);
            panel.AddChild(plain);
            var (_, _, _, player) = Create(panel);

            button.OnClick();
            Later(100);
            plain.OnClick();
            Assert.AreEqual(1, player.Played.Count, "no handler, no sound");

            plain.Click += (_, _) => { };
            Later(100);
            plain.OnClick();
            CollectionAssert.AreEqual(new[] { UiSound.Click, UiSound.Click }, player.Sounds());
        }

        [TestMethod]
        public void Toggles_sound_their_new_state()
        {
            var check = new CheckBox { Text = "Music" };
            var toggle = new ToggleSwitch();
            var panel = new StackPanel();
            panel.AddChild(check);
            panel.AddChild(toggle);
            var (_, _, _, player) = Create(panel);

            check.OnClick();
            Later(100);
            check.OnClick();
            Later(100);
            toggle.OnClick();
            CollectionAssert.AreEqual(new[] { UiSound.ToggleOn, UiSound.ToggleOff, UiSound.ToggleOn }, player.Sounds());
        }

        [TestMethod]
        public void Repeats_within_the_interval_are_dropped()
        {
            var button = new Button();
            var (engine, _, _, player) = Create(button);
            button.OnClick();
            Later(10);
            button.OnClick();
            Assert.AreEqual(1, player.Played.Count);
            Later(engine.Sounds.GetMinInterval(UiSound.Click).TotalMilliseconds);
            button.OnClick();
            Assert.AreEqual(2, player.Played.Count);
        }

        [TestMethod]
        public void Overrides_mute_remap_and_mix_down_the_tree()
        {
            var button = new Button();
            var panel = new StackPanel();
            panel.AddChild(button);
            var (engine, _, _, player) = Create(panel);
            engine.Sounds.MasterVolume = 0.5f;
            engine.Sounds.SetVolume(UiSound.Click, 1);

            panel.Sounds.Volume = 0.5f;
            button.Sounds.Volume = 0.5f;
            button.OnClick();
            Assert.AreEqual(0.125f, player.Played[0].Volume, 0.0001f, "master x panel x button");

            panel.Sounds.Map(UiSound.Click, UiSound.Select);
            Later(100);
            button.OnClick();
            Assert.AreEqual(UiSound.Select, player.Played[1].Sound, "an ancestor remaps");

            button.Sounds.Map(UiSound.Click, UiSound.Back);
            Later(100);
            button.OnClick();
            Assert.AreEqual(UiSound.Back, player.Played[2].Sound, "the nearest map wins");

            button.Sounds.Map(UiSound.Click, null);
            Later(100);
            button.OnClick();
            Assert.AreEqual(3, player.Played.Count, "mapped to silence");

            button.Sounds.Unmap(UiSound.Click);
            panel.Sounds.IsMuted = true;
            Later(100);
            button.OnClick();
            Assert.AreEqual(3, player.Played.Count, "a muted ancestor silences the subtree");

            panel.Sounds.IsMuted = false;
            engine.Sounds.IsMuted = true;
            Later(100);
            button.OnClick();
            Assert.AreEqual(3, player.Played.Count, "engine mute");
        }

        [TestMethod]
        public void Choosing_from_a_popup_does_not_also_play_close()
        {
            var button = new Button();
            var (engine, screen, _, player) = Create(button);
            screen.ShowFlyOut(new Rect(10, 10, 10, 10), new TextBlock { Text = "menu" }, removeOnRelease: false);
            Assert.AreEqual(UiSound.Open, player.Played[^1].Sound);

            Later(100);
            screen.ClearFlyOut();
            Assert.AreEqual(UiSound.Close, player.Played[^1].Sound, "dismissed: close");

            Later(100);
            screen.ShowFlyOut(new Rect(10, 10, 10, 10), new TextBlock { Text = "menu" }, removeOnRelease: false);
            Later(100);
            button.OnClick();
            Later(16);
            screen.ClearFlyOut();
            Assert.AreEqual(UiSound.Click, player.Played[^1].Sound, "the choice is the sound");
            Assert.IsNotNull(engine);
        }

        [TestMethod]
        public void Hover_sounds_only_when_the_pointer_moves_onto_something_clickable()
        {
            var button = new Button { Width = 100, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var (_, screen, source, player) = Create(button);

            source.SetPointer(new PointF(300, 200));
            screen.Update();
            Later(100);
            source.SetPointer(new PointF(20, 20));
            screen.Update();
            CollectionAssert.AreEqual(new[] { UiSound.Hover }, player.Sounds());

            // Leave, then let the control come back under the resting pointer: no sound.
            Later(100);
            button.Margin = new Thickness(200, 0, 0, 0);
            screen.Update();
            Later(100);
            button.Margin = new Thickness(0);
            screen.Update();
            screen.Update();
            Assert.AreEqual(1, player.Played.Count);
        }

        [TestMethod]
        public void Keyboard_focus_moves_slider_steps_and_typing_sound()
        {
            var first = new Button { Content = new TextBlock { Text = "A" } };
            var slider = new Slider { Width = 200, SmallChange = 5 };
            var box = new TextBox { Width = 200, MaxLength = 2 };
            var panel = new StackPanel();
            panel.AddChild(first);
            panel.AddChild(slider);
            panel.AddChild(box);
            var (_, screen, _, player) = Create(panel);

            screen.FocusNextTabStop();
            Assert.AreEqual(UiSound.FocusMove, player.Played[^1].Sound);

            Later(100);
            slider.OnKeyPressed(KeyboardCommand.CursorRight, KeyboardModifiers.None);
            Assert.AreEqual(UiSound.Tick, player.Played[^1].Sound);

            Later(100);
            box.HandleKeyPressed(box, new KeyEventArgs('a'));
            Assert.AreEqual(UiSound.Type, player.Played[^1].Sound);
            Later(100);
            box.HandleKeyPressed(box, new KeyEventArgs('b'));
            Later(100);
            box.HandleKeyPressed(box, new KeyEventArgs('c'));
            Assert.AreEqual(UiSound.Error, player.Played[^1].Sound, "full: the character is rejected");
        }

        [TestMethod]
        public void Playing_allocates_nothing()
        {
            var button = new Button();
            var panel = new StackPanel();
            panel.AddChild(button);
            var (_, _, _, player) = Create(panel);
            panel.Sounds.Volume = 0.8f;
            player.Played.Capacity = 1000;
            button.OnClick();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 200; i++)
            {
                Later(100);
                button.OnClick();
            }
            Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [TestMethod]
        public void The_synth_renders_short_quiet_sounds_for_every_event()
        {
            foreach (UiSynthStyle style in new[] { UiSynthStyle.Soft, UiSynthStyle.Retro, UiSynthStyle.Glass, UiSynthStyle.Mechanical })
            {
                for (var i = 0; i <= (int)UiSound.Type; i++)
                {
                    var pcm = UiSoundSynth.Render((UiSound)i, style);
                    var samples = pcm.Length / 2;
                    Assert.IsTrue(samples > UiSoundSynth.SampleRate * 0.005, $"{(UiSound)i} {style}: not empty");
                    Assert.IsTrue(samples <= UiSoundSynth.SampleRate * 0.2, $"{(UiSound)i} {style}: short");
                    var peak = 0;
                    for (var s = 0; s < samples; s++)
                        peak = Math.Max(peak, Math.Abs((int)(short)(pcm[s * 2] | pcm[s * 2 + 1] << 8)));
                    Assert.IsTrue(peak > 1000 && peak < short.MaxValue, $"{(UiSound)i} {style}: audible, not clipping ({peak})");
                }
            }
        }

        [TestMethod]
        public void The_mouse_clicks_on_the_press_and_a_finger_on_the_release()
        {
            var button = new Button { Width = 100, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var clicks = 0;
            button.Click += (_, _) => clicks++;
            var (_, screen, source, player) = Create(button);

            source.SetPointer(new PointF(20, 20));
            screen.Update();
            Later(100);
            var beforePress = player.Played.Count;
            source.SetPointer(new PointF(20, 20), leftDown: true);
            screen.Update();
            Assert.AreEqual(beforePress + 1, player.Played.Count, "the press clicks");
            Assert.AreEqual(UiSound.Click, player.Played[^1].Sound);
            Assert.AreEqual(0, clicks, "the action waits for the release");

            Later(120);
            source.SetPointer(new PointF(20, 20));
            screen.Update();
            Assert.AreEqual(1, clicks);
            Assert.AreEqual(beforePress + 1, player.Played.Count, "no second click on the release");

            // Keyboard activation (no press) still sounds.
            Later(200);
            button.OnClick();
            Assert.AreEqual(beforePress + 2, player.Played.Count);
        }

        [TestMethod]
        public void A_check_box_sounds_its_coming_state_on_the_mouse_press()
        {
            var check = new CheckBox { Text = "Music", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var (_, screen, source, player) = Create(check);
            source.SetPointer(new PointF(8, 8));
            screen.Update();
            Later(100);
            source.SetPointer(new PointF(8, 8), leftDown: true);
            screen.Update();
            Assert.AreEqual(UiSound.ToggleOn, player.Played[^1].Sound, "about to turn on");
            Later(100);
            source.SetPointer(new PointF(8, 8));
            screen.Update();
            Assert.IsTrue(check.IsChecked);
            Assert.AreEqual(UiSound.ToggleOn, player.Played[^1].Sound);
        }
    }
}

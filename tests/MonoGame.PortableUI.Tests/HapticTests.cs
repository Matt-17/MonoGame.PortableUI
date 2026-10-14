using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.PortableUI.Audio;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Haptics;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class HapticTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class RecordingHaptics : IUiHapticPlayer
        {
            private readonly UiInputDevice _device;

            public RecordingHaptics(UiInputDevice device) => _device = device;

            public readonly List<UiHaptic> Played = new List<UiHaptic>();
            public bool Supports(UiInputDevice device) => device == _device;
            public void Play(UiHaptic haptic, float intensity) => Played.Add(haptic);
            public void Update(TimeSpan now)
            {
            }

            public bool IsActive => false;
        }

        private sealed class RecordingSound : IUiSoundPlayer
        {
            public int Count;
            public void Play(UiSound sound, float volume, float pitch, float pan) => Count++;
        }

        private Game? _game;

        [TestCleanup]
        public void Cleanup() => _game?.Dispose();

        private (ScreenEngine Engine, TestScreen Screen, VirtualInputSource Source, RecordingHaptics Touch, RecordingHaptics Pad) Create(Control content)
        {
            ScreenSystem.TotalTime = TimeSpan.FromSeconds(10);
            _game = new Game();
            var engine = ScreenEngine.Initialize(_game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(400, 300);
            var touch = new RecordingHaptics(UiInputDevice.Touch);
            var pad = new RecordingHaptics(UiInputDevice.GamePad);
            engine.Haptics.Players.Add(touch);
            engine.Haptics.Players.Add(pad);
            var source = new VirtualInputSource();
            var screen = new TestScreen { InputSource = source, Content = content };
            engine.NavigateToScreen(screen);
            screen.Update();
            return (engine, screen, source, touch, pad);
        }

        private static void Later(double milliseconds) => ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(milliseconds);

        [TestMethod]
        public void Haptics_answer_on_the_device_in_use()
        {
            var button = new Button();
            var (engine, _, _, touch, pad) = Create(button);

            engine.LastInputDevice = UiInputDevice.Mouse;
            button.OnClick();
            engine.LastInputDevice = UiInputDevice.Touch;
            Later(100);
            button.OnClick();
            engine.LastInputDevice = UiInputDevice.GamePad;
            Later(100);
            button.OnClick();

            CollectionAssert.AreEqual(new[] { UiHaptic.Click }, touch.Played, "the phone for the tap");
            CollectionAssert.AreEqual(new[] { UiHaptic.Click }, pad.Played, "the pad for the A press; nothing for the mouse");
        }

        [TestMethod]
        public void Events_map_to_pulses_and_haptics_are_muted_separately_from_sound()
        {
            var button = new Button();
            var panel = new StackPanel();
            panel.AddChild(button);
            var (engine, _, _, touch, _) = Create(panel);
            var sound = new RecordingSound();
            engine.Sounds.Player = sound;
            engine.LastInputDevice = UiInputDevice.Touch;

            engine.PlayFeedback(UiSound.Error, button);
            Later(100);
            engine.PlayFeedback(UiSound.Hover, button);
            Later(100);
            engine.PlayFeedback(UiSound.Tick, button);
            CollectionAssert.AreEqual(new[] { UiHaptic.Double, UiHaptic.Tick }, touch.Played, "hover does not buzz");

            panel.Sounds.MuteHaptics = true;
            Later(100);
            var soundsBefore = sound.Count;
            button.OnClick();
            Assert.AreEqual(2, touch.Played.Count, "muted subtree: no pulse");
            Assert.AreEqual(soundsBefore + 1, sound.Count, "but the sound still plays");

            panel.Sounds.MuteHaptics = false;
            engine.Sounds.IsMuted = true;
            Later(100);
            button.OnClick();
            Assert.AreEqual(3, touch.Played.Count, "sound off, vibration on");
        }

        [TestMethod]
        public void Pulses_of_one_kind_are_rate_limited()
        {
            var slider = new Slider();
            var (engine, _, _, _, pad) = Create(slider);
            engine.LastInputDevice = UiInputDevice.GamePad;
            engine.PlayFeedback(UiSound.Tick, slider);
            Later(10);
            engine.PlayFeedback(UiSound.Tick, slider);
            Later(50);
            engine.PlayFeedback(UiSound.Tick, slider);
            Assert.AreEqual(2, pad.Played.Count);
        }

        [TestMethod]
        public void The_last_input_device_follows_mouse_keyboard_and_gamepad()
        {
            var (engine, screen, source, _, _) = Create(new Button());
            source.SetPointer(new PointF(50, 50));
            screen.Update();
            Assert.AreEqual(UiInputDevice.Mouse, engine.LastInputDevice);

            source.SetKeyboardState(new KeyboardState(Keys.Down));
            screen.Update();
            source.SetKeyboardState(new KeyboardState());
            Assert.AreEqual(UiInputDevice.Keyboard, engine.LastInputDevice);

            source.SetGamePadState(new GamePadState(new GamePadThumbSticks(), new GamePadTriggers(), new GamePadButtons(Buttons.A), new GamePadDPad()));
            screen.Update();
            Assert.AreEqual(UiInputDevice.GamePad, engine.LastInputDevice);
        }

        [TestMethod]
        public void A_rumble_pulse_runs_its_pattern_and_stops()
        {
            var calls = new List<(double At, float Left, float Right)>();
            var rumble = new GamePadRumbleHaptics();
            var now = TimeSpan.FromSeconds(10);
            ScreenSystem.TotalTime = now;
            rumble.SetVibration = (_, left, right) => calls.Add(((ScreenSystem.TotalTime - now).TotalMilliseconds, left, right));

            rumble.Play(UiHaptic.Double, 1);
            Assert.IsTrue(rumble.IsActive);
            for (var t = 0.0; t <= 200; t += 5)
            {
                ScreenSystem.TotalTime = now + TimeSpan.FromMilliseconds(t);
                rumble.Update(ScreenSystem.TotalTime);
            }

            Assert.IsFalse(rumble.IsActive);
            Assert.AreEqual(4, calls.Count, string.Join(", ", calls));
            Assert.IsTrue(calls[0].Right > 0 && calls[1].Right == 0 && calls[2].Right > 0 && calls[3].Right == 0, "on, off, on, off");
            Assert.AreEqual(45, calls[1].At, 5);
            Assert.AreEqual(95, calls[2].At, 5);
        }
    }
}

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>
    ///     A click pressed and released between two frames (a quick click during a slow frame) never showed in a
    ///     poll of the buttons' state; <see cref="DeviceInputSource"/> now also reports the presses SDL saw.
    /// </summary>
    [TestClass]
    [DoNotParallelize] // pushes events into SDL's queue for the whole process
    public class QuickClickTests
    {
        private const uint ButtonDown = 0x401, ButtonUp = 0x402;

        [DllImport("SDL2", EntryPoint = "SDL_PushEvent", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PushEvent(byte[] sdlEvent);

        /// <summary>An SDL_MouseButtonEvent in SDL_Event's 56 bytes: type, timestamp, windowID, which, button, state, clicks.</summary>
        private static byte[] ButtonEvent(uint type, byte button)
        {
            var data = new byte[56];
            BitConverter.GetBytes(type).CopyTo(data, 0);
            data[16] = button;
            data[17] = (byte)(type == ButtonDown ? 1 : 0);
            data[18] = 1;
            return data;
        }

        [TestMethod]
        public void Presses_are_counted_per_button_and_taken_once()
        {
            SdlMouseClicks.Discard();
            SdlMouseClicks.Record(SdlMouseClicks.Left);
            SdlMouseClicks.Record(SdlMouseClicks.Left);
            SdlMouseClicks.Record(SdlMouseClicks.Right);
            SdlMouseClicks.Record(7); // extra mouse buttons are ignored
            Assert.AreEqual(2, SdlMouseClicks.Take(SdlMouseClicks.Left));
            Assert.AreEqual(0, SdlMouseClicks.Take(SdlMouseClicks.Left));
            Assert.AreEqual(1, SdlMouseClicks.Take(SdlMouseClicks.Right));
            Assert.AreEqual(0, SdlMouseClicks.Take(SdlMouseClicks.Middle));
        }

        [TestMethod]
        public void A_click_within_one_slow_frame_reads_as_pressed_for_one_frame()
        {
            using var game = new Game(); // starts SDL
            Assert.IsTrue(SdlMouseClicks.TryInstall(), "the watch must run in the SDL that delivers the events");
            var source = DeviceInputSource.Instance;
            var frame = TimeSpan.FromHours(1);
            ScreenSystem.TotalTime = frame;
            _ = source.PressedMouseButtons;
            if (source.PressedMouseButtons.Count > 0)
                Assert.Inconclusive("A real mouse button is held.");

            // Between this frame's poll and the next one: a quick click, down and up again.
            Assert.AreEqual(1, PushEvent(ButtonEvent(ButtonDown, SdlMouseClicks.Right)));
            Assert.AreEqual(1, PushEvent(ButtonEvent(ButtonUp, SdlMouseClicks.Right)));

            ScreenSystem.TotalTime = frame += TimeSpan.FromMilliseconds(16);
            CollectionAssert.AreEqual(new[] { MouseButton.Right }, new System.Collections.Generic.List<MouseButton>(source.PressedMouseButtons));
            // Every screen reading in the same frame sees it; the next frame sees it released.
            CollectionAssert.AreEqual(new[] { MouseButton.Right }, new System.Collections.Generic.List<MouseButton>(source.PressedMouseButtons));
            ScreenSystem.TotalTime = frame += TimeSpan.FromMilliseconds(16);
            Assert.AreEqual(0, source.PressedMouseButtons.Count);

            // Presses while nobody reads the device (inactive window) are dropped.
            PushEvent(ButtonEvent(ButtonDown, SdlMouseClicks.Right));
            source.DiscardClicks();
            ScreenSystem.TotalTime = frame += TimeSpan.FromMilliseconds(16);
            Assert.AreEqual(0, source.PressedMouseButtons.Count);
        }
    }
}

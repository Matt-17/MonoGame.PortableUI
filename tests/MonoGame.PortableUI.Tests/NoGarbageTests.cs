using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    /// <summary>
    ///     A game runs for hours: a UI that makes garbage every frame brings garbage collections and their stutter. The
    ///     update path (input routing, hover, tooltips, timers, layout) must allocate nothing per frame, with the pointer
    ///     resting or moving over controls. Drawing needs a graphics device and is measured by the demo
    ///     (--benchmark-garbage).
    /// </summary>
    [TestClass]
    public class NoGarbageTests
    {
        [TestMethod]
        public void Update_with_a_moving_pointer_allocates_nothing()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(640, 480);
            var source = new VirtualInputSource();
            var screen = new TestScreen { InputSource = source };
            screen.Content = BuildContent();
            engine.NavigateToScreen(screen);
            screen.InvalidateLayout(true);

            var angle = 0f;
            void Frame(bool moving)
            {
                if (moving)
                {
                    angle += .05f;
                    source.SetPointer(new PointF(320 + MathF.Cos(angle) * 300, 240 + MathF.Sin(angle * 1.3f) * 220));
                }
                screen.Update();
            }

            // Warm up as a running game does: hover every control once, and let the JIT optimise in the background.
            for (var round = 0; round < 3; round++)
            {
                for (var i = 0; i < 400; i++)
                    Frame(true);
                Thread.Sleep(150);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 300; i++)
                Frame(false);
            var resting = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 600; i++)
                Frame(true);
            var moving = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, resting, "bytes allocated by 300 updates with the pointer resting");
            Assert.AreEqual(0, moving, "bytes allocated by 600 updates with the pointer moving over controls");
        }

        private static Control BuildContent()
        {
            var grid = new Grid();
            for (var i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
            }
            Control[] controls =
            [
                new Button { Text = "Play" },
                new Button { Text = "Options", ToolTip = "Settings" },
                new CheckBox { Text = "Sound", IsChecked = true },
                new TextBlock { Text = "Score 000120" },
                new Slider { Minimum = 0, Maximum = 100, Value = 40 },
                new ProgressBar { Minimum = 0, Maximum = 100, Value = 60 },
                new TextBox { Text = "Player" },
                new ToggleButton { Text = "Toggle" },
                new Border { Content = new TextBlock { Text = "Framed" } },
                new StackPanel { Children = { new Button { Text = "A" }, new Button { Text = "B" } } },
                new ListBox { Items = { "One", "Two", "Three", "Four", "Five" } },
                new RadioButton { Text = "Easy" },
            ];
            for (var i = 0; i < controls.Length; i++)
            {
                Grid.SetRow(controls[i], i / 4);
                Grid.SetColumn(controls[i], i % 4);
                controls[i].Margin = new Thickness(6);
                grid.Children.Add(controls[i]);
            }
            return grid;
        }

        private sealed class TestScreen : Screen
        {
        }
    }
}

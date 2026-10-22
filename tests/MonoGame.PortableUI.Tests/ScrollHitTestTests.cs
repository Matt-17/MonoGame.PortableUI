using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Input;
using MonoGame.PortableUI.Input;

namespace MonoGame.PortableUI.Tests
{
    [TestClass]
    public class ScrollHitTestTests
    {
        private sealed class TestScreen : Screen
        {
        }

        private sealed class TouchSource : IInputSource
        {
            public TouchCollection Current = new TouchCollection(Array.Empty<TouchLocation>());
            public PointF MousePosition => new PointF(-1, -1);
            public IReadOnlyCollection<MouseButton> PressedMouseButtons => Array.Empty<MouseButton>();
            public int ScrollWheelValue => 0;
            public KeyboardState KeyboardState => default;
            public TouchCollection Touches => Current;
            public void Set(TouchLocationState state, Vector2 position) => Current = new TouchCollection(new[] { new TouchLocation(1, state, position) });
            public void Clear() => Current = new TouchCollection(Array.Empty<TouchLocation>());
        }

        [TestMethod]
        public void A_tap_below_a_horizontal_viewer_reaches_the_button_under_the_finger()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(392, 800);
            var chipClicks = 0;
            var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            for (var i = 0; i < 12; i++)
            {
                var chip = new Button { Text = "Chip " + i, Height = 34, MinHeight = 34 };
                chip.Click += (_, _) => chipClicks++;
                chips.AddChild(chip);
            }
            var chipRow = new ScrollViewer { ScrollOrientation = Orientation.Horizontal, ScrollDirections = ScrollDirections.Horizontal, Margin = new Thickness(-16, 0), Content = chips };
            var filterClicks = 0;
            var filter = new Button { Text = "Filter", Height = 34, HorizontalAlignment = HorizontalAlignment.Right };
            filter.Click += (_, _) => filterClicks++;
            var grid = new Grid();
            grid.AddChild(filter);
            var page = new StackPanel { Spacing = 10, Margin = new Thickness(16, 0) };
            page.AddChild(new Border { Height = 100 });
            page.AddChild(chipRow);
            page.AddChild(grid);
            page.AddChild(new Border { Height = 1200 });
            var source = new TouchSource();
            var screen = new TestScreen { Content = new ScrollViewer { Content = page }, InputSource = source };
            engine.NavigateToScreen(screen);
            screen.Update();
            screen.Update();

            var target = filter.ClippingRect;
            Assert.IsTrue(target.Top >= chipRow.ClippingRect.Bottom, $"layout: {chipRow.ClippingRect} / {target}");
            var at = new Vector2(target.Left + target.Width / 2, target.Top + target.Height / 2);
            source.Set(TouchLocationState.Pressed, at);
            screen.Update();
            source.Set(TouchLocationState.Released, at);
            screen.Update();
            source.Clear();
            screen.Update();

            Assert.AreEqual(0, chipClicks, $"chips {chipRow.ClippingRect}, first chip {chips.GetVisualChild(0).ClippingRect}");
            Assert.AreEqual(1, filterClicks);
        }

        private int _chipClicks, _filterClicks;
        private TestScreen? _screen;
        private Button? _filter;
        private ScrollViewer? _chipRow;

        private Control BuildPage(float layoutScaleUnused)
        {
            var chips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            for (var i = 0; i < 12; i++)
            {
                var chip = new Button { Text = "Chip " + i, Height = 34, MinHeight = 34 };
                chip.Click += (_, _) =>
                {
                    _chipClicks++;
                    _screen!.Content = BuildPage(0);
                };
                chips.AddChild(chip);
            }
            _chipRow = new ScrollViewer { ScrollOrientation = Orientation.Horizontal, ScrollDirections = ScrollDirections.Horizontal, StateKey = "chips", Margin = new Thickness(-16, 0), Content = chips };
            _filter = new Button { Text = "Filter", Height = 34, HorizontalAlignment = HorizontalAlignment.Right };
            _filter.Click += (_, _) => _filterClicks++;
            var grid = new Grid();
            grid.AddChild(_filter);
            var page = new StackPanel { Spacing = 10, Margin = new Thickness(16, 0) };
            page.AddChild(new Border { Height = 100 });
            page.AddChild(_chipRow);
            page.AddChild(grid);
            page.AddChild(new Border { Height = 1200 });
            return new ProgressiveBlur { TopHeight = 40, BottomHeight = 40, Content = new ScrollViewer { StateKey = "page", Content = page } };
        }

        private static void Step(ScreenEngine engine)
        {
            ScreenSystem.TotalTime += TimeSpan.FromMilliseconds(16);
            engine.Update(new GameTime(ScreenSystem.TotalTime, TimeSpan.FromMilliseconds(16)));
        }

        private static void Tap(TouchSource source, ScreenEngine screen, Vector2 at)
        {
            source.Set(TouchLocationState.Pressed, at);
            Step(screen);
            source.Set(TouchLocationState.Moved, at);
            Step(screen);
            source.Set(TouchLocationState.Released, at);
            Step(screen);
            source.Clear();
            Step(screen);
            Step(screen);
        }

        [TestMethod]
        public void After_a_chip_rebuilt_the_page_a_tap_below_the_chips_reaches_the_button()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(392, 870);
            var source = new TouchSource();
            _screen = new TestScreen { InputSource = source };
            _screen.Content = BuildPage(0);
            engine.NavigateToScreen(_screen);
            Step(engine);
            Step(engine);

            var chip = (Control)((StackPanel)_chipRow!.Content!).GetVisualChild(1);
            var c = chip.ClippingRect;
            Tap(source, engine, new Vector2((c.Left + c.Width / 2), (c.Top + c.Height / 2)));
            Assert.AreEqual(1, _chipClicks, $"chip tap {c} scale {engine.RenderScale}");

            var f = _filter!.ClippingRect;
            Tap(source, engine, new Vector2((f.Left + f.Width / 2), (f.Top + f.Height / 2)));
            Assert.AreEqual(1, _chipClicks, $"chips {_chipRow.ClippingRect} filter {f}");
            Assert.AreEqual(1, _filterClicks);
        }

        [TestMethod]
        public void A_scrim_tap_that_closes_a_sheet_never_reaches_the_page_below()
        {
            using var game = new Game();
            var engine = ScreenEngine.Initialize(game, new ScreenEngineOptions { AddComponentToGame = false });
            engine.SetScreenSize(392, 870);
            var source = new TouchSource();
            var pageTouches = 0;
            var list = new ScrollViewer { ScrollBarVisibility = ScrollBarVisibility.AutoHide, Content = new Border { Height = 3000 } };
            list.TouchDown += (_, _) => pageTouches++;
            list.TouchUp += (_, _) => pageTouches++;
            var page = new TestScreen { Content = list, InputSource = source };
            engine.NavigateToScreen(page);
            Step(engine);

            var modal = engine.ShowModal(new Border { Height = 200 }, new ModalOptions { Placement = ModalPlacement.BottomSheet });
            modal.InputSource = source;
            for (var i = 0; i < 40; i++)
                Step(engine);

            source.Set(TouchLocationState.Pressed, new Vector2(200, 100));
            Step(engine);
            Assert.IsFalse(modal.IsOpen, "the scrim tap closes the sheet");
            foreach (var y in new[] { 110f, 140f, 180f })
            {
                source.Set(TouchLocationState.Moved, new Vector2(200, y));
                Step(engine);
            }
            source.Set(TouchLocationState.Released, new Vector2(200, 180));
            Step(engine);
            source.Clear();
            for (var i = 0; i < 40; i++)
                Step(engine);

            Assert.AreEqual(0, pageTouches);
            Assert.AreEqual(0, list.Offset.Y);
            Assert.AreEqual(0f, list.ScrollBarOpacity, "no scroll bar shown");

            source.Set(TouchLocationState.Pressed, new Vector2(200, 300));
            Step(engine);
            source.Set(TouchLocationState.Released, new Vector2(200, 300));
            Step(engine);
            source.Clear();
            Step(engine);
            Assert.AreEqual(2, pageTouches, "the next gesture is the page's");
        }
    }
}

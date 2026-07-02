using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MonoGame.PortableUI.Common;
using MonoGame.PortableUI.Controls;
using MonoGame.PortableUI.Controls.Events;
using MonoGame.PortableUI.Controls.Input;

namespace MonoGame.PortableUI.Benchmarks
{
    [MemoryDiagnoser]
    public class PortableUiBenchmarks
    {
        private Grid _stressGrid = new Grid();
        private ScrollViewer _scrollList = new ScrollViewer();
        private Rect _viewport;

        [GlobalSetup]
        public void Setup()
        {
            _viewport = new Rect(0, 0, 1200, 800);
            _stressGrid = CreateStressGrid(25, 20);
            _stressGrid.UpdateLayout(_viewport);
            _scrollList = CreateScrollList(500);
            _scrollList.UpdateLayout(_viewport);
        }

        [Benchmark]
        public Rect GridLayout500Controls()
        {
            _stressGrid.UpdateLayout(_viewport);
            return _stressGrid.BoundingRect;
        }

        [Benchmark]
        public Size ScrollListLayout500Controls()
        {
            _scrollList.UpdateLayout(_viewport);
            return _scrollList.Extent;
        }

        [Benchmark]
        public int VisualTreeFlatten500Controls()
        {
            return VisualTreeHelper.GetVisualTreeAsList(_stressGrid, false).Count();
        }

        [Benchmark]
        public int HitTraversal500Controls()
        {
            var handled = 0;
            var args = new MouseEventArgs(new PointF(600, 400), new List<MouseButton> { MouseButton.Left });
            VisualTreeHelper.IterateVisualTree(
                _stressGrid,
                args,
                (control, eventArgs) => control.BoundingRect.Contains(eventArgs.Position),
                (control, eventArgs) =>
                {
                    handled++;
                    eventArgs.Handled = true;
                },
                null);
            return handled;
        }

        [Benchmark]
        public int MissTraversal500Controls()
        {
            var handled = 0;
            var args = new MouseEventArgs(new PointF(-10, -10), new List<MouseButton> { MouseButton.Left });
            VisualTreeHelper.IterateVisualTree(
                _stressGrid,
                args,
                (control, eventArgs) => control.BoundingRect.Contains(eventArgs.Position),
                (control, eventArgs) => handled++,
                null);
            return handled;
        }

        private static Grid CreateStressGrid(int rows, int columns)
        {
            var grid = new Grid();
            for (var row = 0; row < rows; row++)
                grid.RowDefinitions.Add(new RowDefinition());
            for (var column = 0; column < columns; column++)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    grid.AddChild(new FixedSizeControl(new Size(48, 24)), row, column);
                }
            }

            return grid;
        }

        private static ScrollViewer CreateScrollList(int controlCount)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            for (var i = 0; i < controlCount; i++)
                stack.AddChild(new FixedSizeControl(new Size(240, 24)));

            return new ScrollViewer
            {
                Content = stack,
                ScrollOrientation = Orientation.Vertical
            };
        }

        private sealed class FixedSizeControl : Control
        {
            private readonly Size _size;

            public FixedSizeControl(Size size)
            {
                _size = size;
            }

            public override Size MeasureLayout()
            {
                return _size;
            }
        }
    }
}

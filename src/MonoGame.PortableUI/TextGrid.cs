using System;

using MonoGame.PortableUI.Common;

namespace MonoGame.PortableUI
{
    /// <summary>
    ///     A text-mode display: the screen is a grid of <see cref="Columns"/> × <see cref="Rows"/>
    ///     character cells and the pointer only exists per cell — like the DOS mouse driver (INT 33h),
    ///     which reported text-mode coordinates in whole cells. Set it on the display
    ///     (<see cref="ScreenEngineOptions.TextGrid"/>, <see cref="UISurface.TextGrid"/>); each
    ///     engine/surface has its own (80×25 DOS stations next to a 40×25 C64 in one process).
    /// </summary>
    /// <remarks>
    ///     The cell size is the screen's layout size divided by the grid, so it follows any logical
    ///     size and tolerates non-square display scaling. Recommended logical sizes: 640×400 for
    ///     80×25 with 8×16 cells (CGA/EGA style), 720×400 for 80×25 with 9×16 VGA cells, 320×200 for
    ///     a 40×25 C64 screen — each shown stretched to a 4:3 monitor.
    /// </remarks>
    public readonly struct TextGrid : IEquatable<TextGrid>
    {
        public TextGrid(int columns, int rows)
        {
            if (columns <= 0)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows <= 0)
                throw new ArgumentOutOfRangeException(nameof(rows));
            Columns = columns;
            Rows = rows;
        }

        public int Columns { get; }

        public int Rows { get; }

        /// <summary>80×25, the DOS text mode.</summary>
        public static TextGrid Dos => new(80, 25);

        /// <summary>40×25, the C64 screen.</summary>
        public static TextGrid C64 => new(40, 25);

        /// <summary>The cell (column, row) under <paramref name="point"/>, clamped to the grid.</summary>
        public (int Column, int Row) CellAt(PointF point, Rect area)
        {
            var column = (int)Math.Floor((point.X - area.Left) / CellWidth(area));
            var row = (int)Math.Floor((point.Y - area.Top) / CellHeight(area));
            return (Math.Clamp(column, 0, Columns - 1), Math.Clamp(row, 0, Rows - 1));
        }

        /// <summary>The rectangle of a cell within <paramref name="area"/>.</summary>
        public Rect CellRect(int column, int row, Rect area)
        {
            var width = CellWidth(area);
            var height = CellHeight(area);
            return new Rect(area.Left + column * width, area.Top + row * height, width, height);
        }

        /// <summary>The centre of the cell under <paramref name="point"/>: where a text-mode pointer acts.</summary>
        public PointF Snap(PointF point, Rect area)
        {
            var (column, row) = CellAt(point, area);
            var cell = CellRect(column, row, area);
            return new PointF(cell.Left + cell.Width / 2, cell.Top + cell.Height / 2);
        }

        public float CellWidth(Rect area) => area.Width / Columns;

        public float CellHeight(Rect area) => area.Height / Rows;

        public bool Equals(TextGrid other) => Columns == other.Columns && Rows == other.Rows;

        public override bool Equals(object? obj) => obj is TextGrid other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Columns, Rows);

        public static bool operator ==(TextGrid left, TextGrid right) => left.Equals(right);

        public static bool operator !=(TextGrid left, TextGrid right) => !left.Equals(right);

        public override string ToString() => $"{Columns}x{Rows}";
    }
}

using GoEngine.Core;

namespace GoEngine.Tests.Support;

internal static class Diagram
{
    public static PositionSnapshot Parse(params string[] rows)
    {
        int size = rows.Length;
        PointState[] coloring = new PointState[size * size];
        for (int y = 0; y < size; y++)
        {
            if (rows[y].Length != size)
            {
                throw new ArgumentException($"Row {y} has {rows[y].Length} points, expected {size}.", nameof(rows));
            }

            for (int x = 0; x < size; x++)
            {
                // Row index is Y, column index is X: the same p.Y * size + p.X layout as PositionSnapshot.
                coloring[y * size + x] = rows[y][x] switch
                {
                    '.' => PointState.Empty,
                    'X' => PointState.Black,
                    'O' => PointState.White,
                    char c => throw new ArgumentException($"Unknown point '{c}' at ({x}, {y}).", nameof(rows)),
                };
            }
        }

        return new PositionSnapshot(size, coloring, Color.Black, 0, 0, 0);
    }
}

namespace GoEngine.Core;

/// <summary>Turn alternation and the <see cref="PointState"/> of a player's stones.</summary>
public static class ColorExtensions
{
    /// <summary>The other player, who moves after <paramref name="color"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The color is undefined.</exception>
    public static Color Opposite(this Color color)
    {
        return color switch
        {
            Color.Black => Color.White,
            Color.White => Color.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Undefined color."),
        };
    }

    /// <summary>The state of a point that holds a stone of <paramref name="color"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The color is undefined.</exception>
    public static PointState ToPointState(this Color color)
    {
        return color switch
        {
            Color.Black => PointState.Black,
            Color.White => PointState.White,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, "Undefined color."),
        };
    }
}

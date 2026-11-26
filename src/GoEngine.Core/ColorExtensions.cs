namespace GoEngine.Core;

/// <summary>
/// Turn alternation and conversion of a <see cref="Color"/> to the
/// <see cref="PointState"/> of its stones.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Returns the color of the other player, i.e. whose turn follows a move by
    /// <paramref name="color"/>.
    /// </summary>
    /// <param name="color">A defined player color.</param>
    /// <returns>
    /// <see cref="Color.White"/> for <see cref="Color.Black"/>, and
    /// <see cref="Color.Black"/> for <see cref="Color.White"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="color"/> is not a defined <see cref="Color"/> value.
    /// </exception>
    public static Color Opposite(this Color color)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the content of a point occupied by a stone of <paramref name="color"/>.
    /// </summary>
    /// <param name="color">A defined player color.</param>
    /// <returns>
    /// <see cref="PointState.Black"/> for <see cref="Color.Black"/>, and
    /// <see cref="PointState.White"/> for <see cref="Color.White"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="color"/> is not a defined <see cref="Color"/> value.
    /// </exception>
    public static PointState ToPointState(this Color color)
    {
        throw new NotImplementedException();
    }
}

namespace GoEngine.Core;

/// <summary>
/// Identifies one of the two players and, through
/// <see cref="ColorExtensions.ToPointState(Color)"/>, the stones that player places.
/// </summary>
/// <remarks>
/// <c>0</c> is deliberately not a defined value, so an uninitialised
/// <c>default(Color)</c> is detectable: <see cref="ColorExtensions"/> rejects it with
/// <see cref="ArgumentOutOfRangeException"/>.
/// </remarks>
public enum Color
{
    /// <summary>Moves first in an even game.</summary>
    Black = 1,

    /// <summary>Receives komi.</summary>
    White = 2,
}

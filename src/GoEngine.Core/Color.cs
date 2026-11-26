namespace GoEngine.Core;

/// <summary>One of the two players.</summary>
/// <remarks><c>0</c> is undefined, so an unset <c>default(Color)</c> is detectable.</remarks>
public enum Color
{
    /// <summary>Moves first in an even game.</summary>
    Black = 1,

    /// <summary>Moves second in an even game.</summary>
    White = 2,
}

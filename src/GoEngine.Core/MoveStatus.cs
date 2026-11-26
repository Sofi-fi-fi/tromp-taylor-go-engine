namespace GoEngine.Core;

/// <summary>
/// The outcome of an attempt to play a move: <see cref="Ok"/> when the move was played,
/// otherwise the reason it was rejected.
/// </summary>
/// <remarks>
/// The rejection statuses are listed in the order in which the engine checks them, so a move
/// that violates several conditions reports the first one in this list. The numeric values
/// must not be relied on for that order.
/// <c>0</c> is deliberately not a defined value, so that <c>default(<see cref="MoveResult"/>)</c>
/// cannot be mistaken for a successful move.
/// </remarks>
public enum MoveStatus
{
    /// <summary>The move was played.</summary>
    Ok = 1,

    /// <summary>The game has already ended, so no further move is accepted.</summary>
    GameOver = 2,

    /// <summary>The point lies outside the board.</summary>
    OutOfBounds = 3,

    /// <summary>The point already holds a stone.</summary>
    Occupied = 4,

    /// <summary>
    /// The move would leave the player's own chain without liberties and the rule set does not
    /// allow suicide.
    /// </summary>
    Suicide = 5,

    /// <summary>The move would recreate a board position that occurred earlier in the game.</summary>
    Superko = 6,
}

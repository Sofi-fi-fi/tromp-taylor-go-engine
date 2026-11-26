namespace GoEngine.Core;

/// <summary>The outcome of a move: <see cref="Ok"/> or the reason it was rejected.</summary>
/// <remarks><see cref="IGoEngine"/> defines the check order. <c>0</c> is undefined, so
/// <c>default(<see cref="MoveResult"/>)</c> cannot pass for a played move.</remarks>
public enum MoveStatus
{
    /// <summary>The move was played.</summary>
    Ok = 1,

    /// <summary>The game has ended.</summary>
    GameOver = 2,

    /// <summary>The point is outside the board.</summary>
    OutOfBounds = 3,

    /// <summary>The point already holds a stone.</summary>
    Occupied = 4,

    /// <summary>A suicide that the <see cref="RuleSet"/> does not allow.</summary>
    Suicide = 5,

    /// <summary>The move would repeat an earlier board coloring.</summary>
    Superko = 6,
}

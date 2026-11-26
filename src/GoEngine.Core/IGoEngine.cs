namespace GoEngine.Core;

/// <summary>One game of Go under the <see cref="RuleSet"/> the engine was created with.</summary>
/// <remarks>
/// Rule violations are returned as <see cref="MoveResult"/>, never thrown. An invalid argument
/// throws before any rule is checked, even after the game is over.
/// Play reports the first that applies of <see cref="MoveStatus.GameOver"/>,
/// <see cref="MoveStatus.OutOfBounds"/>, <see cref="MoveStatus.Occupied"/>,
/// <see cref="MoveStatus.Suicide"/> and <see cref="MoveStatus.Superko"/>. Opponent chains left
/// without liberties are removed first; suicide is judged on the board after that.
/// Pass is rejected only with <see cref="MoveStatus.GameOver"/>.
/// A rejected Play or Pass changes nothing: position, turn, pass count and history.
/// Without a color the player to move plays. After any accepted Play or Pass the turn goes to
/// the opposite of the color that moved, so explicit colors may repeat.
/// Two consecutive passes of any colors end the game; Play and Pass then return
/// <see cref="MoveStatus.GameOver"/>, Undo still works.
/// Suicide is rejected unless <see cref="RuleSet.AllowSuicide"/>; when allowed, the own chain
/// is removed (<see cref="MoveResult.SelfCapture"/>).
/// Superko is positional: a played stone may not recreate any earlier board coloring, the
/// starting one included; the player to move is not compared. A pass never violates superko.
/// Returned sets are copies owned by the caller.
/// </remarks>
public interface IGoEngine
{
    /// <summary>Plays a stone for the player to move.</summary>
    MoveResult Play(Point point);

    /// <summary>Plays a stone of <paramref name="color"/>.</summary>
    /// <param name="point">Any point; one outside the board is rejected, not thrown.</param>
    /// <param name="color">The color to play.</param>
    /// <exception cref="ArgumentOutOfRangeException">The color is undefined.</exception>
    MoveResult Play(Point point, Color color);

    /// <summary>Passes for the player to move; an accepted pass captures nothing.</summary>
    MoveResult Pass();

    /// <summary>Passes for <paramref name="color"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The color is undefined.</exception>
    MoveResult Pass(Color color);

    /// <summary>
    /// Reverts the last accepted Play or Pass: the state, superko history included, becomes as it
    /// was before that call.
    /// </summary>
    /// <returns><see langword="false"/> if there is nothing to undo.</returns>
    bool Undo();

    /// <summary>Area score with komi; may be called at any time and changes nothing.</summary>
    /// <returns>Never <see langword="null"/>.</returns>
    ScoreResult Score();

    /// <summary>Returns the current position.</summary>
    /// <returns>Never <see langword="null"/>; not affected by later calls.</returns>
    PositionSnapshot Snapshot();

    /// <summary>The chain containing <paramref name="point"/>; empty for an empty point.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the board.</exception>
    IReadOnlySet<Point> GetChain(Point point);

    /// <summary>
    /// The liberties of the chain containing <paramref name="point"/>; empty for an empty point.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the board.</exception>
    IReadOnlySet<Point> GetLiberties(Point point);
}

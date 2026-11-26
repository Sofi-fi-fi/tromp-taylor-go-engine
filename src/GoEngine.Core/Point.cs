namespace GoEngine.Core;

/// <summary>
/// A board location given by 0-based coordinates: <see cref="X"/> is the column,
/// <see cref="Y"/> is the row, and <c>(0, 0)</c> is the top-left corner, as in SGF.
/// </summary>
/// <remarks>
/// The coordinates are not validated against any board size: a point outside the board
/// must be representable so that the engine can reject a move to it with an
/// out-of-bounds result instead of failing on construction.
/// </remarks>
/// <param name="X">0-based column, counted from the left edge.</param>
/// <param name="Y">0-based row, counted from the top edge.</param>
public readonly record struct Point(int X, int Y);

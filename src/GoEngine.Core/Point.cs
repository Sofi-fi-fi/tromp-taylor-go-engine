namespace GoEngine.Core;

/// <summary>A board point: column X, row Y; <c>(0, 0)</c> is top left, as in SGF.</summary>
/// <remarks>Unchecked, so an off-board move gets <see cref="MoveStatus.OutOfBounds"/>.</remarks>
public readonly record struct Point(int X, int Y);

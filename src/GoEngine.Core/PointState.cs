namespace GoEngine.Core;

/// <summary>
/// What occupies a single board point: nothing or a stone of one player.
/// </summary>
/// <remarks>
/// <see cref="Empty"/> is <c>0</c>, so a newly allocated <see cref="PointState"/> array
/// is an empty board.
/// </remarks>
public enum PointState
{
    /// <summary>No stone occupies the point.</summary>
    Empty = 0,

    /// <summary>A stone placed by <see cref="Color.Black"/> occupies the point.</summary>
    Black = 1,

    /// <summary>A stone placed by <see cref="Color.White"/> occupies the point.</summary>
    White = 2,
}

namespace GoEngine.Core;

/// <summary>What occupies a board point.</summary>
/// <remarks><see cref="Empty"/> is <c>0</c>, so a new array is an empty board.</remarks>
public enum PointState
{
    /// <summary>No stone.</summary>
    Empty = 0,

    /// <summary>A stone of <see cref="Color.Black"/>.</summary>
    Black = 1,

    /// <summary>A stone of <see cref="Color.White"/>.</summary>
    White = 2,
}

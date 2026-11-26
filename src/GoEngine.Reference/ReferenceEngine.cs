using GoEngine.Core;

namespace GoEngine.Reference;

/// <summary>A naive <see cref="IGoEngine"/> built only on the public contract, used as a test oracle.</summary>
public sealed class ReferenceEngine : IGoEngine
{
    private readonly RuleSet _rules;
    private readonly Grid _grid;
    private readonly Color _toMove;
    private readonly int _consecutivePasses;
    private readonly int _blackStonesRemoved;
    private readonly int _whiteStonesRemoved;

    /// <summary>Starts a game on an empty board with Black to move.</summary>
    public ReferenceEngine(RuleSet rules)
        : this(rules, EmptyPosition(rules))
    {
    }

    /// <summary>Starts a game from an arbitrary position.</summary>
    public ReferenceEngine(RuleSet rules, PositionSnapshot position)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(position);
        if (position.Size != rules.Size)
        {
            throw new ArgumentException("The position size differs from the rule set size.", nameof(position));
        }

        Grid grid = new(position);

        // Comment 4: strings without liberties cannot exist. Such a grid cannot arise under the rules,
        // and accepting it would make differential tests compare nonsense, so it is rejected.
        foreach (Point p in grid.AllPoints())
        {
            if (grid[p] != PointState.Empty && !grid.Reaches(p, PointState.Empty))
            {
                throw new ArgumentException($"The chain at ({p.X}, {p.Y}) has no liberties.", nameof(position));
            }
        }

        _rules = rules;
        _grid = grid;
        _toMove = position.ToMove;
        _consecutivePasses = position.ConsecutivePasses;
        _blackStonesRemoved = position.BlackStonesRemoved;
        _whiteStonesRemoved = position.WhiteStonesRemoved;
    }

    public MoveResult Play(Point point)
    {
        throw new NotImplementedException();
    }

    public MoveResult Play(Point point, Color color)
    {
        throw new NotImplementedException();
    }

    public MoveResult Pass()
    {
        throw new NotImplementedException();
    }

    public MoveResult Pass(Color color)
    {
        throw new NotImplementedException();
    }

    public bool Undo()
    {
        throw new NotImplementedException();
    }

    public ScoreResult Score()
    {
        throw new NotImplementedException();
    }

    public PositionSnapshot Snapshot()
    {
        PointState[] coloring = [.. _grid.AllPoints().Select(p => _grid[p])];
        return new PositionSnapshot(
            _rules.Size, coloring, _toMove, _consecutivePasses, _blackStonesRemoved, _whiteStonesRemoved);
    }

    public IReadOnlySet<Point> GetChain(Point point)
    {
        ThrowIfOutside(point);
        return _grid.Chain(point);
    }

    public IReadOnlySet<Point> GetLiberties(Point point)
    {
        ThrowIfOutside(point);
        return _grid.Liberties(point);
    }

    // Rule 5: the game starts with an empty grid and Black to move.
    private static PositionSnapshot EmptyPosition(RuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return new PositionSnapshot(rules.Size, new PointState[rules.Size * rules.Size], Color.Black, 0, 0, 0);
    }

    // Rule 1: points exist only on the grid.
    private void ThrowIfOutside(Point point)
    {
        if (!_grid.Contains(point))
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "The point is outside the board.");
        }
    }
}

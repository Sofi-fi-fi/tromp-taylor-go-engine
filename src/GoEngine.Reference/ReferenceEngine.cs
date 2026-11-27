using System.Collections.Immutable;
using GoEngine.Core;

namespace GoEngine.Reference;

/// <summary>A naive <see cref="IGoEngine"/> built only on the public contract, used as a test oracle.</summary>
public sealed class ReferenceEngine : IGoEngine
{
    private readonly RuleSet _rules;
    private Grid _grid;
    private Color _toMove;
    private int _consecutivePasses;
    private int _blackStonesRemoved;
    private int _whiteStonesRemoved;

    // Rule 6: every grid colouring so far, the current one included (so a single-stone suicide, which
    // leaves the current colouring, is Superko), oldest first. Kept whole rather than hashed so the
    // oracle cannot share Core's collision risk. Immutable, so a saved earlier state keeps its own
    // history and undo can restore it by reference instead of removing entries.
    private ImmutableList<Grid> _history;

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

        // Rule 5 starts from an empty grid; a loaded position takes its place as the first colouring
        // (project decision 3.1), since its real earlier colourings are unknown.
        _history = [grid];
    }

    // Rule 5: without a colour the player to move plays.
    public MoveResult Play(Point point)
    {
        return Play(point, _toMove);
    }

    public MoveResult Play(Point point, Color color)
    {
        // IGoEngine: an invalid argument throws before any rule is checked.
        if (!Enum.IsDefined(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Undefined color.");
        }

        // Rule 8 and IGoEngine: after the game has ended no turn is played; checked before the point.
        if (IsOver)
        {
            return MoveResult.Rejected(MoveStatus.GameOver);
        }

        // Rule 1: points exist only on the grid.
        if (!_grid.Contains(point))
        {
            return MoveResult.Rejected(MoveStatus.OutOfBounds);
        }

        // Rule 7: a move colours an empty point.
        if (_grid[point] != PointState.Empty)
        {
            return MoveResult.Rejected(MoveStatus.Occupied);
        }

        // Rule 7: colour the point, clear the opponent colour, then clear one's own colour.
        // Comment 7: at most one of the two clearings has an effect.
        PointState own = color.ToPointState();
        PointState opponent = color.Opposite().ToPointState();
        (Grid afterOpponent, int captured) = _grid.With(point, own).Clear(opponent);
        (Grid afterOwn, int selfCaptured) = afterOpponent.Clear(own);

        // Comment 7: clearing one's own colour is suicide; the new grids are dropped, nothing changed.
        if (selfCaptured > 0 && !_rules.AllowSuicide)
        {
            return MoveResult.Rejected(MoveStatus.Suicide);
        }

        // Rule 6: the colouring is judged after both clearings of rule 7. Comment 6: positional, so
        // only colourings are compared, not the player to move or removed counters. Checked after
        // Suicide, as IGoEngine orders them.
        if (_history.Any(afterOwn.SameColouring))
        {
            return MoveResult.Rejected(MoveStatus.Superko);
        }

        _history = _history.Add(afterOwn);

        _grid = afterOwn;
        if (color == Color.Black)
        {
            _whiteStonesRemoved += captured;
            _blackStonesRemoved += selfCaptured;
        }
        else
        {
            _blackStonesRemoved += captured;
            _whiteStonesRemoved += selfCaptured;
        }

        // Rule 5 and IGoEngine: the turn goes to the opposite of the colour that moved.
        _toMove = color.Opposite();

        // Rule 8: only consecutive passes count, so a move resets the count.
        _consecutivePasses = 0;

        return selfCaptured > 0 ? MoveResult.SelfCapture(selfCaptured) : MoveResult.Success(captured);
    }

    // Rule 5: without a colour the player to move passes.
    public MoveResult Pass()
    {
        return Pass(_toMove);
    }

    public MoveResult Pass(Color color)
    {
        // IGoEngine: an invalid argument throws before any rule is checked.
        if (!Enum.IsDefined(color))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "Undefined color.");
        }

        // Rule 8: after the game has ended no turn is played.
        if (IsOver)
        {
            return MoveResult.Rejected(MoveStatus.GameOver);
        }

        // Rule 6: a pass colours nothing, so the grid, removed counters and history stay; its colouring
        // is already in the history and a pass is never a superko violation.
        _consecutivePasses++;

        // Rule 5 and IGoEngine: the turn goes to the opposite of the colour that passed.
        _toMove = color.Opposite();

        return MoveResult.Success(0);
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

    // Rule 8: the game ends after two consecutive passes.
    private bool IsOver => _consecutivePasses == 2;

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

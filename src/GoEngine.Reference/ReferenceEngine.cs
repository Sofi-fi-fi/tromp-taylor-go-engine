using GoEngine.Core;

namespace GoEngine.Reference;

/// <summary>A naive <see cref="IGoEngine"/> built only on the public contract, used as a test oracle.</summary>
public sealed class ReferenceEngine : IGoEngine
{
    /// <summary>Starts a game on an empty board with Black to move.</summary>
    public ReferenceEngine(RuleSet rules)
    {
        throw new NotImplementedException();
    }

    /// <summary>Starts a game from an arbitrary position.</summary>
    public ReferenceEngine(RuleSet rules, PositionSnapshot position)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public IReadOnlySet<Point> GetChain(Point point)
    {
        throw new NotImplementedException();
    }

    public IReadOnlySet<Point> GetLiberties(Point point)
    {
        throw new NotImplementedException();
    }
}

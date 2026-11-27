using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Support;

// Shared by the contract scenarios: each engine under test supplies the two factories.
public abstract class EngineContractTests
{
    protected abstract IGoEngine CreateEngine(RuleSet rules);

    protected abstract IGoEngine CreateEngine(RuleSet rules, PositionSnapshot position);

    protected IGoEngine Load(params string[] rows)
    {
        return Load(rows, Color.Black);
    }

    protected IGoEngine Load(
        string[] rows,
        Color toMove = Color.Black,
        int consecutivePasses = 0,
        int blackStonesRemoved = 0,
        int whiteStonesRemoved = 0,
        bool allowSuicide = false,
        decimal komi = 7.5m)
    {
        PositionSnapshot position = Diagram.Parse(rows, toMove, consecutivePasses, blackStonesRemoved, whiteStonesRemoved);
        return CreateEngine(RuleSet.CreateForTests(position.Size, komi, allowSuicide), position);
    }

    protected static bool SameColoring(PositionSnapshot a, PositionSnapshot b)
    {
        if (a.Size != b.Size)
        {
            return false;
        }

        for (int y = 0; y < a.Size; y++)
        {
            for (int x = 0; x < a.Size; x++)
            {
                if (a[new Point(x, y)] != b[new Point(x, y)])
                {
                    return false;
                }
            }
        }

        return true;
    }

    protected static void AssertBoard(PositionSnapshot actual, params string[] rows)
    {
        PositionSnapshot expected = Diagram.Parse(rows);
        Assert.Equal(expected.Size, actual.Size);
        for (int y = 0; y < expected.Size; y++)
        {
            for (int x = 0; x < expected.Size; x++)
            {
                Assert.True(
                    expected[new Point(x, y)] == actual[new Point(x, y)],
                    $"Point ({x}, {y}): expected {expected[new Point(x, y)]}, actual {actual[new Point(x, y)]}.");
            }
        }
    }

    protected static void AssertPoints(IEnumerable<Point> expected, IReadOnlySet<Point> actual)
    {
        Assert.Equal(Sorted(expected), Sorted(actual));
    }

    protected static Point[] Points(int[] xy)
    {
        return [.. Enumerable.Range(0, xy.Length / 2).Select(i => new Point(xy[2 * i], xy[2 * i + 1]))];
    }

    // The snapshot plus the chain and liberties seen from every point. Comparing it rejects an undo or
    // a rejected move that restores the coloring but not the chain structure or liberties.
    protected static EngineState CaptureState(IGoEngine engine)
    {
        PositionSnapshot snapshot = engine.Snapshot();
        Point[] points = [.. Enumerable.Range(0, snapshot.Size * snapshot.Size)
            .Select(i => new Point(i % snapshot.Size, i / snapshot.Size))];
        return new EngineState(
            snapshot,
            [.. points.Select(p => Sorted(engine.GetChain(p)))],
            [.. points.Select(p => Sorted(engine.GetLiberties(p)))]);
    }

    protected static void AssertSameState(EngineState before, IGoEngine engine)
    {
        EngineState after = CaptureState(engine);
        Assert.Equal(before.Snapshot, after.Snapshot);
        for (int i = 0; i < before.Chains.Count; i++)
        {
            Point p = new(i % before.Snapshot.Size, i / before.Snapshot.Size);
            Assert.True(before.Chains[i].SequenceEqual(after.Chains[i]), $"GetChain({p.X}, {p.Y}) differs.");
            Assert.True(before.Liberties[i].SequenceEqual(after.Liberties[i]), $"GetLiberties({p.X}, {p.Y}) differs.");
        }
    }

    protected static void AssertRejected(IGoEngine engine, Func<MoveResult> action, MoveStatus status)
    {
        EngineState before = CaptureState(engine);

        Assert.Equal(MoveResult.Rejected(status), action());
        AssertSameState(before, engine);
    }

    protected static void AssertRejected(IGoEngine engine, Point point, MoveStatus status)
    {
        AssertRejected(engine, () => engine.Play(point), status);
    }

    protected static void AssertRejected(IGoEngine engine, Point point, Color color, MoveStatus status)
    {
        AssertRejected(engine, () => engine.Play(point, color), status);
    }

    private static List<Point> Sorted(IEnumerable<Point> points)
    {
        return [.. points.OrderBy(p => p.Y).ThenBy(p => p.X)];
    }

    protected sealed record EngineState(
        PositionSnapshot Snapshot,
        IReadOnlyList<List<Point>> Chains,
        IReadOnlyList<List<Point>> Liberties);
}

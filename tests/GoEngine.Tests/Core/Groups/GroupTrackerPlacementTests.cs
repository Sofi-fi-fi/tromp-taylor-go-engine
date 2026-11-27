using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Core.Groups;

// Design §4 "GroupTracker": union-find over chains, incremental liberties, merging chains when a
// stone is placed. Expected chains and liberties are written out by hand, so every test checks
// design §5: union-find chains and liberties equal a full flood fill.
public class GroupTrackerPlacementTests
{
    // Rejects: counting off-board neighbours, or a fixed liberty count of 4.
    [Theory]
    [InlineData(2, 2, new[] { 2, 1, 1, 2, 3, 2, 2, 3 })]
    [InlineData(2, 0, new[] { 1, 0, 3, 0, 2, 1 })]
    [InlineData(0, 0, new[] { 1, 0, 0, 1 })]
    public void SingleStone_LibertiesAreItsOnBoardNeighbours(int x, int y, int[] liberties)
    {
        GroupTracker tracker = new(5);

        Place(tracker, Color.Black, x, y);

        AssertPoints(tracker, [new(x, y)], Members(tracker, x, y));
        AssertPoints(tracker, Points(liberties), Liberties(tracker, x, y));
    }

    // Rejects: chains or liberties invented for an empty point.
    [Fact]
    public void EmptyPoint_HasNoMembersAndNoLiberties()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.Black, 1, 1);

        Assert.Empty(tracker.Members(tracker.Board.IndexOf(new Point(0, 0))));
        Assert.Empty(tracker.Liberties(tracker.Board.IndexOf(new Point(0, 0))));
    }

    // Rejects: no merge with an own neighbour, liberties not combined on merge, or the point
    // just played kept as a liberty of the merged chain.
    [Fact]
    public void PlacingNextToOwnChain_MergesChainsAndLiberties()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.Black, 1, 2);

        Place(tracker, Color.Black, 2, 2);

        Assert.Equal(Root(tracker, 1, 2), Root(tracker, 2, 2));
        AssertPoints(tracker, [new(1, 2), new(2, 2)], Members(tracker, 2, 2));
        AssertPoints(
            tracker,
            [new(1, 1), new(0, 2), new(1, 3), new(2, 1), new(3, 2), new(2, 3)],
            Liberties(tracker, 1, 2));
    }

    // Rejects: a merge that skips one of the neighbouring chains (e.g. stops after the first
    // union, or misses the last neighbour), or liberties kept as a list, so (1, 1), (3, 1),
    // (1, 3) and (3, 3), each next to two of the chains, would be counted twice.
    [Fact]
    public void StoneJoiningChainsFromAllFourSides_MakesOneChain()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.Black, 2, 1);
        Place(tracker, Color.Black, 1, 2);
        Place(tracker, Color.Black, 3, 2);
        Place(tracker, Color.Black, 2, 3);

        Place(tracker, Color.Black, 2, 2);

        Point[] chain = [new(2, 1), new(1, 2), new(3, 2), new(2, 3), new(2, 2)];
        int root = Root(tracker, 2, 2);
        Assert.All(chain, p => Assert.Equal(root, tracker.Root(tracker.Board.IndexOf(p))));
        AssertPoints(tracker, chain, Members(tracker, 2, 1));
        AssertPoints(
            tracker,
            [new(2, 0), new(1, 1), new(3, 1), new(0, 2), new(4, 2), new(1, 3), new(3, 3), new(2, 4)],
            Liberties(tracker, 2, 2));
    }

    // Layout for the next two tests: a black chain around (2, 2) on three sides.
    //   . . . . .
    //   . X X X .
    //   . X ? X .
    //   . . . . .
    private static GroupTracker ThreeSidedChain()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.Black, 1, 1);
        Place(tracker, Color.Black, 2, 1);
        Place(tracker, Color.Black, 3, 1);
        Place(tracker, Color.Black, 1, 2);
        Place(tracker, Color.Black, 3, 2);
        return tracker;
    }

    // Rejects: a liberty counter decremented once per adjacency (three times here) instead of a
    // set losing one point, or opponent chains merged with the new stone.
    [Fact]
    public void OpponentStoneTouchingOneChainFromThreeSides_RemovesExactlyOneLiberty()
    {
        GroupTracker tracker = ThreeSidedChain();
        Point[] before =
            [new(1, 0), new(2, 0), new(3, 0), new(0, 1), new(4, 1), new(0, 2), new(2, 2), new(4, 2), new(1, 3), new(3, 3)];
        AssertPoints(tracker, before, Liberties(tracker, 2, 1));

        Place(tracker, Color.White, 2, 2);

        AssertPoints(tracker, before.Where(p => p != new Point(2, 2)), Liberties(tracker, 2, 1));
        AssertPoints(tracker, [new(2, 3)], Liberties(tracker, 2, 2));
        AssertPoints(tracker, [new(2, 2)], Members(tracker, 2, 2));
    }

    // Rejects: a union of a chain with itself, which corrupts the size and splices the circular
    // member list with itself, splitting it into two rings.
    [Fact]
    public void OwnStoneTouchingOneChainFromThreeSides_JoinsItOnce()
    {
        GroupTracker tracker = ThreeSidedChain();
        int root = Root(tracker, 1, 1);

        Place(tracker, Color.Black, 2, 2);

        Point[] chain = [new(1, 1), new(2, 1), new(3, 1), new(1, 2), new(3, 2), new(2, 2)];
        Assert.Equal(root, Root(tracker, 2, 2));
        foreach (Point start in chain)
        {
            AssertPoints(tracker, chain, tracker.Members(tracker.Board.IndexOf(start)));
        }

        AssertPoints(
            tracker,
            [new(1, 0), new(2, 0), new(3, 0), new(0, 1), new(4, 1), new(0, 2), new(4, 2), new(1, 3), new(3, 3), new(2, 3)],
            Liberties(tracker, 2, 2));
    }

    // Rejects: an opponent stone left as a liberty, or the new stone counting the opponent's
    // stone as a liberty or merging with it.
    [Fact]
    public void PlacingNextToOpponentChain_RemovesThatLibertyFromIt()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.Black, 2, 2);

        Place(tracker, Color.White, 3, 2);

        Assert.NotEqual(Root(tracker, 2, 2), Root(tracker, 3, 2));
        AssertPoints(tracker, [new(2, 2)], Members(tracker, 2, 2));
        AssertPoints(tracker, [new(2, 1), new(1, 2), new(2, 3)], Liberties(tracker, 2, 2));
        AssertPoints(tracker, [new(3, 1), new(4, 2), new(3, 3)], Liberties(tracker, 3, 2));
    }

    // Placement does not decide captures or suicide (MoveResolver does, design §4).
    // Rejects: placement that removes, rejects or throws on a chain left without liberties.
    [Fact]
    public void OpponentChain_MayReachZeroLiberties()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.White, 0, 0);
        Place(tracker, Color.Black, 1, 0);

        Place(tracker, Color.Black, 0, 1);

        Assert.Equal(PointState.White, tracker.Board[tracker.Board.IndexOf(new Point(0, 0))]);
        AssertPoints(tracker, [new(0, 0)], Members(tracker, 0, 0));
        Assert.Empty(Liberties(tracker, 0, 0));
        AssertPoints(tracker, [new(2, 0), new(1, 1)], Liberties(tracker, 1, 0));
        AssertPoints(tracker, [new(1, 1), new(0, 2)], Liberties(tracker, 0, 1));
    }

    // Rejects: placement that refuses a stone whose own chain ends up without liberties.
    [Fact]
    public void OwnChain_MayReachZeroLiberties()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.White, 1, 0);
        Place(tracker, Color.White, 0, 1);

        Place(tracker, Color.Black, 0, 0);

        Assert.Equal(PointState.Black, tracker.Board[tracker.Board.IndexOf(new Point(0, 0))]);
        Assert.Empty(Liberties(tracker, 0, 0));
        AssertPoints(tracker, [new(2, 0), new(1, 1)], Liberties(tracker, 1, 0));
        AssertPoints(tracker, [new(1, 1), new(0, 2)], Liberties(tracker, 0, 1));
    }

    // Rejects: a merge that leaves a liberty behind once the board is full.
    [Fact]
    public void FullBoardOfOneColour_IsOneChainWithoutLiberties()
    {
        GroupTracker tracker = new(2);
        Place(tracker, Color.Black, 0, 0);
        Place(tracker, Color.Black, 1, 1);
        Place(tracker, Color.Black, 1, 0);
        Place(tracker, Color.Black, 0, 1);

        AssertPoints(tracker, [new(0, 0), new(1, 0), new(0, 1), new(1, 1)], Members(tracker, 1, 1));
        Assert.Empty(Liberties(tracker, 0, 0));
    }

    // Rejects: splicing the two circular member lists wrongly, which loses part of a ring or
    // loops forever (the walk is bounded, so a broken ring fails instead of hanging).
    [Fact]
    public void MergedChain_MembersAreWalkedFromEveryStone()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.Black, 0, 2);
        Place(tracker, Color.Black, 1, 2);
        Place(tracker, Color.Black, 3, 2);
        Place(tracker, Color.Black, 4, 2);
        Place(tracker, Color.Black, 4, 1);

        Place(tracker, Color.Black, 2, 2);

        Point[] chain = [new(0, 2), new(1, 2), new(2, 2), new(3, 2), new(4, 2), new(4, 1)];
        foreach (Point start in chain)
        {
            AssertPoints(tracker, chain, tracker.Members(tracker.Board.IndexOf(start)));
        }
    }

    // Union by size keeps the union-find trees shallow without path compression.
    // Rejects: taking the root of the first neighbour scanned, of the lower index, or of the
    // stone just placed. The larger chain is on a different side in each case.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Merge_RootIsTheLargerChainsRoot(bool largerAbove)
    {
        GroupTracker tracker = new(5);
        Point[] large = largerAbove ? [new(1, 0), new(2, 0), new(2, 1)] : [new(2, 3), new(2, 4), new(3, 4)];
        Point small = largerAbove ? new(2, 3) : new(2, 1);
        foreach (Point p in large)
        {
            Place(tracker, Color.Black, p.X, p.Y);
        }

        Place(tracker, Color.Black, small.X, small.Y);
        int largeRoot = tracker.Root(tracker.Board.IndexOf(large[0]));

        Place(tracker, Color.Black, 2, 2);

        Assert.Equal(largeRoot, Root(tracker, 2, 2));
        Assert.Equal(largeRoot, tracker.Root(tracker.Board.IndexOf(small)));
    }

    // Rejects: placement that silently overwrites a stone and leaves its chain inconsistent.
    [Fact]
    public void PlacingOnOccupiedPoint_Throws()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.Black, 1, 1);

        Assert.Throws<InvalidOperationException>(() => Place(tracker, Color.White, 1, 1));
    }

    private static void Place(GroupTracker tracker, Color color, int x, int y)
    {
        tracker.Place(tracker.Board.IndexOf(new Point(x, y)), color);
    }

    private static int Root(GroupTracker tracker, int x, int y)
    {
        return tracker.Root(tracker.Board.IndexOf(new Point(x, y)));
    }

    private static IEnumerable<int> Members(GroupTracker tracker, int x, int y)
    {
        return tracker.Members(tracker.Board.IndexOf(new Point(x, y)));
    }

    private static IEnumerable<int> Liberties(GroupTracker tracker, int x, int y)
    {
        return tracker.Liberties(tracker.Board.IndexOf(new Point(x, y)));
    }

    private static Point[] Points(int[] xy)
    {
        return [.. Enumerable.Range(0, xy.Length / 2).Select(i => new Point(xy[2 * i], xy[2 * i + 1]))];
    }

    // Compares as sets and also checks there are no duplicates; at most PointCount + 1 indices
    // are read, so a cyclic enumeration that never ends still fails.
    private static void AssertPoints(GroupTracker tracker, IEnumerable<Point> expected, IEnumerable<int> actual)
    {
        Point[] points = [.. actual.Take(tracker.Board.PointCount + 1).Select(tracker.Board.PointAt)];
        Assert.Equal(points.Length, points.Distinct().Count());
        Assert.Equal(expected.ToHashSet(), points.ToHashSet());
    }
}

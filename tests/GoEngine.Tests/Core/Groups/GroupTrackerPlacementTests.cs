using GoEngine.Core;
using Xunit;
using static GoEngine.Tests.Core.Groups.TrackerHelpers;

namespace GoEngine.Tests.Core.Groups;

// Design §5: union-find chains and liberties equal a full flood fill (expected sets by hand).
public class GroupTrackerPlacementTests
{
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

    [Fact]
    public void EmptyPoint_HasNoMembersAndNoLiberties()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.Black, 1, 1);

        Assert.Empty(tracker.Members(tracker.Board.IndexOf(new Point(0, 0))));
        Assert.Empty(tracker.Liberties(tracker.Board.IndexOf(new Point(0, 0))));
    }

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

    // Catches: liberties kept as a list; (1, 1), (3, 1), (1, 3) and (3, 3) each touch two arms.
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

    // Black chain around (2, 2) on three sides: (1, 1)-(3, 1), (1, 2) and (3, 2).
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

    // Catches: a liberty counter decremented once per adjacency instead of a set.
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

    // Catches: a union of a chain with itself, which splits its member ring in two.
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

    // Design §4: captures and suicide are decided by MoveResolver, not by placement.
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

    // The larger chain is on a different side in each case, so scan order or index cannot win by luck.
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

    [Fact]
    public void PlacingOnOccupiedPoint_Throws()
    {
        GroupTracker tracker = new(3);
        Place(tracker, Color.Black, 1, 1);

        Assert.Throws<InvalidOperationException>(() => Place(tracker, Color.White, 1, 1));
    }

    private static Point[] Points(int[] xy)
    {
        return [.. Enumerable.Range(0, xy.Length / 2).Select(i => new Point(xy[2 * i], xy[2 * i + 1]))];
    }
}

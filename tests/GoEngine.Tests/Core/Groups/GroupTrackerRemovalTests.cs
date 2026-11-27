using GoEngine.Core;
using Xunit;
using static GoEngine.Tests.Core.Groups.TrackerHelpers;

namespace GoEngine.Tests.Core.Groups;

// Design §5: union-find chains and liberties equal a full flood fill (expected sets by hand).
public class GroupTrackerRemovalTests
{
    // White chain W; A, B and C touch it, D does not.
    //   . . . . .
    //   . A A . .
    //   C W W W .
    //   . . . B .
    //   D . . . .
    private static GroupTracker Surrounded()
    {
        GroupTracker tracker = new(5);
        Place(tracker, Color.White, 1, 2);
        Place(tracker, Color.White, 2, 2);
        Place(tracker, Color.White, 3, 2);
        Place(tracker, Color.Black, 1, 1);
        Place(tracker, Color.Black, 2, 1);
        Place(tracker, Color.Black, 0, 2);
        Place(tracker, Color.Black, 3, 3);
        Place(tracker, Color.Black, 0, 4);
        return tracker;
    }

    [Fact]
    public void RemovingChain_EmptiesAllItsPoints()
    {
        GroupTracker tracker = Surrounded();

        int removed = Remove(tracker, 2, 2);

        Assert.Equal(3, removed);
        foreach (Point p in new Point[] { new(1, 2), new(2, 2), new(3, 2) })
        {
            int index = tracker.Board.IndexOf(p);
            Assert.Equal(PointState.Empty, tracker.Board[index]);
            Assert.Empty(tracker.Members(index));
            Assert.Empty(tracker.Liberties(index));
        }
    }

    // Catches: every removed point given to every adjacent chain (A must not get (3, 2)), or to D.
    [Fact]
    public void RemovingChain_GivesEachAdjacentOpponentChainExactlyTheFreedPointsItTouches()
    {
        GroupTracker tracker = Surrounded();

        Remove(tracker, 1, 2);

        AssertPoints(tracker, [new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(1, 2), new(2, 2)], Liberties(tracker, 1, 1));
        AssertPoints(tracker, [new(0, 1), new(0, 3), new(1, 2)], Liberties(tracker, 0, 2));
        AssertPoints(tracker, [new(2, 3), new(4, 3), new(3, 4), new(3, 2)], Liberties(tracker, 3, 3));
        AssertPoints(tracker, [new(0, 3), new(1, 4)], Liberties(tracker, 0, 4));
    }

    [Fact]
    public void RemovingOneOfTwoChains_LeavesTheOtherIntact()
    {
        //   W W X W W
        GroupTracker tracker = new(5);
        Place(tracker, Color.White, 0, 0);
        Place(tracker, Color.White, 1, 0);
        Place(tracker, Color.White, 3, 0);
        Place(tracker, Color.White, 4, 0);
        Place(tracker, Color.Black, 2, 0);
        int otherRoot = Root(tracker, 4, 0);

        Assert.Equal(2, Remove(tracker, 0, 0));

        Assert.Equal(PointState.White, tracker.Board[tracker.Board.IndexOf(new Point(3, 0))]);
        Assert.Equal(PointState.White, tracker.Board[tracker.Board.IndexOf(new Point(4, 0))]);
        Assert.Equal(otherRoot, Root(tracker, 3, 0));
        AssertPoints(tracker, [new(3, 0), new(4, 0)], Members(tracker, 3, 0));
        AssertPoints(tracker, [new(3, 1), new(4, 1)], Liberties(tracker, 4, 0));
        AssertPoints(tracker, [new(1, 0), new(2, 1)], Liberties(tracker, 2, 0));
    }

    // Catches: stale parent, ring or liberty data left at freed points.
    [Fact]
    public void AfterRemoval_PlacingOnFreedPointsWorksNormally()
    {
        GroupTracker tracker = Surrounded();
        Remove(tracker, 2, 2);

        Place(tracker, Color.White, 2, 2);

        AssertPoints(tracker, [new(2, 2)], Members(tracker, 2, 2));
        AssertPoints(tracker, [new(1, 2), new(3, 2), new(2, 3)], Liberties(tracker, 2, 2));
        AssertPoints(tracker, [new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(1, 2)], Liberties(tracker, 1, 1));

        Place(tracker, Color.Black, 1, 2);

        AssertPoints(tracker, [new(1, 1), new(2, 1), new(0, 2), new(1, 2)], Members(tracker, 1, 2));
        AssertPoints(
            tracker,
            [new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(0, 3), new(1, 3)],
            Liberties(tracker, 0, 2));
        AssertPoints(tracker, [new(3, 2), new(2, 3)], Liberties(tracker, 2, 2));
    }

    [Fact]
    public void RemovingAtEmptyPoint_Throws()
    {
        GroupTracker tracker = new(3);

        Assert.Throws<InvalidOperationException>(() => Remove(tracker, 1, 1));
    }

    private static int Remove(GroupTracker tracker, int x, int y)
    {
        return tracker.RemoveChain(tracker.Board.IndexOf(new Point(x, y)));
    }
}

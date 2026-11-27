using GoEngine.Core;
using Xunit;
using static GoEngine.Tests.Core.Groups.TrackerHelpers;

namespace GoEngine.Tests.Core.Groups;

// Design §4 "GroupTracker": removing a whole chain on capture. Expected chains and liberties are
// written out by hand, so every test checks design §5: union-find chains and liberties equal a
// full flood fill.
public class GroupTrackerRemovalTests
{
    // Layout for most tests: a white chain W with black chains around it.
    //   . . . . .
    //   . A A . .      A touches W at (1, 2) and (2, 2)
    //   C W W W .      C touches W at (1, 2)
    //   . . . B .      B touches W at (3, 2)
    //   D . . . .      D does not touch W
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

    // Rejects: removing only the given stone or only the root, or leaving the chain's liberty
    // set reachable from a freed point.
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

    // Each adjacent chain is updated once even when it touches several removed stones (A touches
    // two). Rejects: giving every removed point to every adjacent chain (A would get (3, 2)),
    // a liberty counter raised once per touching stone, or freeing points for chains that do
    // not touch them (D).
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

    // Rejects: a removal that walks past the chain into another ring, or clears stones by
    // colour, so the other chain of the same colour would also be affected.
    [Fact]
    public void RemovingOneOfTwoChains_LeavesTheOtherIntact()
    {
        //   W W X W W
        //   . . . . .
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

    // Rejects: stale parent, ring or liberty data left at freed points, which would make a new
    // stone appear joined to removed stones or start with the removed chain's liberties.
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

    // Rejects: a removal that silently does nothing (or corrupts state) for an empty point.
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

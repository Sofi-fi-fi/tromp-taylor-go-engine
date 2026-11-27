using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Core.Groups;

// GroupTracker calls by (x, y) instead of board index, shared by the chain and liberty tests.
internal static class TrackerHelpers
{
    public static void Place(GroupTracker tracker, Color color, int x, int y)
    {
        tracker.Place(tracker.Board.IndexOf(new Point(x, y)), color);
    }

    public static int Root(GroupTracker tracker, int x, int y)
    {
        return tracker.Root(tracker.Board.IndexOf(new Point(x, y)));
    }

    public static IEnumerable<int> Members(GroupTracker tracker, int x, int y)
    {
        return tracker.Members(tracker.Board.IndexOf(new Point(x, y)));
    }

    public static IEnumerable<int> Liberties(GroupTracker tracker, int x, int y)
    {
        return tracker.Liberties(tracker.Board.IndexOf(new Point(x, y)));
    }

    // Compares as sets and also checks there are no duplicates; at most PointCount + 1 indices
    // are read, so a cyclic enumeration that never ends still fails.
    public static void AssertPoints(GroupTracker tracker, IEnumerable<Point> expected, IEnumerable<int> actual)
    {
        Point[] points = [.. actual.Take(tracker.Board.PointCount + 1).Select(tracker.Board.PointAt)];
        Assert.Equal(points.Length, points.Distinct().Count());
        Assert.Equal(expected.ToHashSet(), points.ToHashSet());
    }
}

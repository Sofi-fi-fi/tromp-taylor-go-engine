namespace GoEngine.Core;

// Stub for the GroupTracker placement and merge tests; implemented in the next group.
internal sealed class GroupTracker
{
    public GroupTracker(int size)
    {
        throw new NotImplementedException();
    }

    public Board Board => throw new NotImplementedException();

    public void Place(int point, Color color)
    {
        throw new NotImplementedException();
    }

    public int Root(int point)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<int> Members(int point)
    {
        throw new NotImplementedException();
    }

    public IReadOnlySet<int> Liberties(int point)
    {
        throw new NotImplementedException();
    }
}

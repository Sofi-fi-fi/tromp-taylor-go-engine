using GoEngine.Core;

namespace GoEngine.Reference;

// The oracle trades speed for obviousness: every query is a full traversal with no caching or
// union-find, so it shares no algorithm with Core (design doc §7).
internal sealed class Grid
{
    private readonly PointState[] _points;

    public Grid(PositionSnapshot position)
    {
        Size = position.Size;
        _points = new PointState[Size * Size];
        foreach (Point p in AllPoints())
        {
            _points[Index(p)] = position[p];
        }
    }

    public int Size { get; }

    public PointState this[Point p] => _points[Index(p)];

    // Rule 1: the grid is Size x Size.
    public bool Contains(Point p)
    {
        return p.X >= 0 && p.X < Size && p.Y >= 0 && p.Y < Size;
    }

    public IEnumerable<Point> AllPoints()
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                yield return new Point(x, y);
            }
        }
    }

    // Rule 3: adjacency is vertical or horizontal only.
    public IEnumerable<Point> Neighbours(Point p)
    {
        Point[] candidates = [new(p.X, p.Y - 1), new(p.X - 1, p.Y), new(p.X + 1, p.Y), new(p.X, p.Y + 1)];
        return candidates.Where(Contains);
    }

    // Rule 3: P reaches C if a path of points of P's colour leads from P to a point of colour C.
    public bool Reaches(Point p, PointState target)
    {
        if (this[p] == target)
        {
            throw new ArgumentException("Rule 3 defines reaching only for a point not of the target colour.", nameof(target));
        }

        return Region(p).Any(q => Neighbours(q).Any(n => this[n] == target));
    }

    // Comment 3: a string is the set of stones of one colour connected by paths of that colour.
    public IReadOnlySet<Point> Chain(Point p)
    {
        return this[p] == PointState.Empty ? new HashSet<Point>() : Region(p);
    }

    // Comment 3: the liberties of a string are the empty points it reaches in one step.
    public IReadOnlySet<Point> Liberties(Point p)
    {
        return Chain(p).SelectMany(Neighbours).Where(n => this[n] == PointState.Empty).ToHashSet();
    }

    // All points connected to p through points of p's colour, p included.
    private HashSet<Point> Region(Point p)
    {
        PointState colour = this[p];
        HashSet<Point> visited = [p];
        Stack<Point> pending = new([p]);
        while (pending.Count > 0)
        {
            foreach (Point n in Neighbours(pending.Pop()))
            {
                if (this[n] == colour && visited.Add(n))
                {
                    pending.Push(n);
                }
            }
        }

        return visited;
    }

    private int Index(Point p)
    {
        return p.Y * Size + p.X;
    }
}

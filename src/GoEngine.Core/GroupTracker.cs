using System.Collections.Frozen;

namespace GoEngine.Core;

// Chains and liberties of the stones on a board, kept up to date stone by stone.
// Placement only colours a point and updates chains: deciding captures and suicide is
// MoveResolver's job, so a chain is allowed to reach zero liberties here.
// All state is the board colour plus four per-point arrays, changed only by Place (colour and
// arrays), Union (arrays) and RemoveChain (colour of the removed stones, liberty sets of the
// neighbouring chains, and the removed chain's own liberty set), so a later undo journal can
// record exactly those writes.
internal sealed class GroupTracker
{
    private readonly Board _board;

    // Union-find parent; a root is its own parent. Only meaningful for occupied points.
    private readonly int[] _parent;

    // Stone count of the chain; only meaningful at a root.
    private readonly int[] _size;

    // Circular linked list of chain members, so a whole chain can be walked (and later removed)
    // without scanning the board. Two rings merge by swapping one successor in each.
    private readonly int[] _next;

    // Liberty set of the chain; only non-null at a root. A set, not a counter, because one empty
    // point can touch the same chain from several sides and must count once.
    private readonly HashSet<int>?[] _liberties;

    public GroupTracker(int size)
    {
        _board = new Board(size);
        int count = _board.PointCount;
        _parent = new int[count];
        _size = new int[count];
        _next = new int[count];
        _liberties = new HashSet<int>?[count];
    }

    // The tracked board. Read it only: a write would bypass the chain bookkeeping.
    public Board Board => _board;

    public void Place(int point, Color color)
    {
        if (_board[point] != PointState.Empty)
        {
            throw new InvalidOperationException($"Point {point} is already occupied.");
        }

        PointState own = color.ToPointState();
        _board[point] = own;
        _parent[point] = point;
        _size[point] = 1;
        _next[point] = point;
        _liberties[point] = [];

        int root = point;
        foreach (int n in _board.Neighbours(point))
        {
            PointState state = _board[n];
            if (state == PointState.Empty)
            {
                // Into the current root's set: after a union the new stone's own set is dropped.
                _liberties[root]!.Add(n);
            }
            else if (state == own)
            {
                // Several neighbours may belong to one chain; joining it again would corrupt
                // its size and split its member ring.
                int other = Root(n);
                if (other != root)
                {
                    root = Union(root, other);
                }
            }
            else
            {
                _liberties[Root(n)]!.Remove(point);
            }
        }

        // The point was a liberty of every own chain just joined.
        _liberties[root]!.Remove(point);
    }

    // No path compression: it would rewrite parents on reads, and undo would have to reverse
    // those hidden writes. Union by size alone keeps trees at most log2(N*N) deep.
    public int Root(int point)
    {
        if (_board[point] == PointState.Empty)
        {
            throw new InvalidOperationException($"Point {point} has no stone.");
        }

        while (_parent[point] != point)
        {
            point = _parent[point];
        }

        return point;
    }

    public IEnumerable<int> Members(int point)
    {
        if (_board[point] == PointState.Empty)
        {
            yield break;
        }

        int current = point;
        do
        {
            yield return current;
            current = _next[current];
        }
        while (current != point);
    }

    // The live set of the chain's root: valid until the next Place or RemoveChain.
    public IReadOnlySet<int> Liberties(int point)
    {
        if (_board[point] == PointState.Empty)
        {
            return FrozenSet<int>.Empty;
        }

        return _liberties[Root(point)]!;
    }

    // Removes the whole chain containing point and returns its stone count.
    public int RemoveChain(int point)
    {
        int root = Root(point);

        // Two walks of the ring, which stays intact: first empty every stone, then give the
        // freed points to the neighbours. Emptying first means a removed neighbour is skipped,
        // and the liberty sets add each freed point to a chain once however many removed
        // stones it touches.
        int removed = 0;
        int current = point;
        do
        {
            _board[current] = PointState.Empty;
            removed++;
            current = _next[current];
        }
        while (current != point);

        do
        {
            foreach (int n in _board.Neighbours(current))
            {
                if (_board[n] != PointState.Empty)
                {
                    _liberties[Root(n)]!.Add(current);
                }
            }

            current = _next[current];
        }
        while (current != point);

        // Parent, size and ring entries of freed points are left stale; every read checks the board
        // first. Undo journal: Place overwrites a point's parent, size, next and liberties, so save
        // the old values before it does, or undoing a capture cannot restore them.
        _liberties[root] = null;
        return removed;
    }

    // Joins two distinct roots; the larger chain's root stays root and keeps its liberty set.
    private int Union(int a, int b)
    {
        (int large, int small) = _size[a] >= _size[b] ? (a, b) : (b, a);

        _parent[small] = large;
        _size[large] += _size[small];
        _liberties[large]!.UnionWith(_liberties[small]!);
        _liberties[small] = null;
        (_next[large], _next[small]) = (_next[small], _next[large]);

        return large;
    }
}

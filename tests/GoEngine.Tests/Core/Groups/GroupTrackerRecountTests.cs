using FsCheck;
using FsCheck.Fluent;
using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Core.Groups;

// Design §5: incremental liberties equal a full recount.
public class GroupTrackerRecountTests
{
    // Pick selects among the currently empty (place) or occupied (remove) points, so every
    // operation is valid whatever the board looks like by then.
    public readonly record struct Operation(bool Remove, int Pick, Color Color);

    [SeededProperty]
    public Property RandomPlacementsAndRemovals_MatchFullRecountAfterEveryStep()
    {
        return Prop.ForAll(Scenarios(), scenario =>
        {
            (int size, Operation[] operations) = scenario;
            GroupTracker tracker = new(size);
            for (int step = 0; step < operations.Length; step++)
            {
                Apply(tracker, operations[step]);
                AssertMatchesRecount(tracker, step);
            }
        });
    }

    private static Arbitrary<(int Size, Operation[] Operations)> Scenarios()
    {
        // Three placements per removal, so boards fill up and removals hit real chains.
        Gen<Operation> operation =
            from kind in Gen.Choose(0, 3)
            from pick in Gen.Choose(0, 1000)
            from color in Gen.Elements(Color.Black, Color.White)
            select new Operation(kind == 0, pick, color);

        Gen<(int, Operation[])> gen =
            from size in Gen.Choose(RuleSet.MinTestSize, RuleSet.MaxSize)
            from operations in operation.ArrayOf()
            select (size, operations);
        return Arb.From(gen);
    }

    private static void Apply(GroupTracker tracker, Operation op)
    {
        int[] candidates = [.. Enumerable.Range(0, tracker.Board.PointCount)
            .Where(i => (tracker.Board[i] == PointState.Empty) != op.Remove)];
        if (candidates.Length == 0)
        {
            return;
        }

        int point = candidates[op.Pick % candidates.Length];
        if (op.Remove)
        {
            tracker.RemoveChain(point);
        }
        else
        {
            tracker.Place(point, op.Color);
        }
    }

    // The recount uses only the board colouring: a flood fill from each stone.
    private static void AssertMatchesRecount(GroupTracker tracker, int step)
    {
        Board board = tracker.Board;
        for (int i = 0; i < board.PointCount; i++)
        {
            if (board[i] == PointState.Empty)
            {
                Assert.Empty(tracker.Members(i));
                Assert.Empty(tracker.Liberties(i));
                continue;
            }

            HashSet<int> chain = [i];
            HashSet<int> liberties = [];
            Stack<int> pending = new([i]);
            while (pending.Count > 0)
            {
                foreach (int n in board.Neighbours(pending.Pop()))
                {
                    if (board[n] == PointState.Empty)
                    {
                        liberties.Add(n);
                    }
                    else if (board[n] == board[i] && chain.Add(n))
                    {
                        pending.Push(n);
                    }
                }
            }

            int[] members = [.. tracker.Members(i).Take(board.PointCount + 1)];
            Assert.True(chain.SetEquals(members) && members.Length == chain.Count, $"Chain differs at {board.PointAt(i)} after step {step}.");
            Assert.True(liberties.SetEquals(tracker.Liberties(i)), $"Liberties differ at {board.PointAt(i)} after step {step}.");
        }
    }
}

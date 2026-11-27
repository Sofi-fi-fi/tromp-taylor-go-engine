using FsCheck;
using FsCheck.Fluent;
using GoEngine.Core;
using GoEngine.Reference;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Core.Groups;

// Design §5: union-find chains and liberties equal a full flood fill (Reference).
public class ChainDifferentialTests
{
    // 3x3 colourings without a liberty-less chain, counted by an independent brute-force script.
    private const int ValidThreeByThreeColourings = 12675;

    private static readonly PointState[] States = [PointState.Empty, PointState.Black, PointState.White];

    [SeededProperty]
    public Property RandomColouring_ChainsAndLibertiesMatchReference()
    {
        return Prop.ForAll(Colourings(), colouring =>
        {
            int size = (int)Math.Sqrt(colouring.Length);
            if (!TryLoadReference(size, colouring, out IGoEngine? reference))
            {
                return Prop.Discard<bool>().ToProperty();
            }

            AssertSameChains(size, colouring, reference);
            return true.ToProperty();
        });
    }

    [Fact]
    public void Every3x3Colouring_ChainsAndLibertiesMatchReference()
    {
        const int size = 3;
        PointState[] colouring = new PointState[size * size];
        int compared = 0;

        for (int code = 0; code < 19683; code++)
        {
            for (int i = 0, rest = code; i < colouring.Length; i++, rest /= 3)
            {
                colouring[i] = States[rest % 3];
            }

            if (TryLoadReference(size, colouring, out IGoEngine? reference))
            {
                AssertSameChains(size, colouring, reference);
                compared++;
            }
        }

        // Catches: a Reference that rejects valid colourings, which would silently shrink this test.
        Assert.Equal(ValidThreeByThreeColourings, compared);
    }

    // Empty points are weighted up on some boards so that fewer colourings are discarded.
    private static Arbitrary<PointState[]> Colourings()
    {
        Gen<PointState[]> gen =
            from size in Gen.Choose(RuleSet.MinTestSize, 9)
            from emptyWeight in Gen.Choose(1, 4)
            from points in Gen.Frequency(
                    (emptyWeight, Gen.Constant(PointState.Empty)),
                    (1, Gen.Constant(PointState.Black)),
                    (1, Gen.Constant(PointState.White)))
                .ArrayOf(size * size)
            select points;
        return Arb.From(gen);
    }

    private static bool TryLoadReference(int size, PointState[] colouring, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IGoEngine? reference)
    {
        PositionSnapshot position = new(size, colouring, Color.Black, 0, 0, 0);
        try
        {
            reference = new ReferenceEngine(RuleSet.CreateForTests(size), position);
            return true;
        }
        catch (ArgumentException)
        {
            // Reference refuses a chain without liberties: such a position cannot arise in a game.
            reference = null;
            return false;
        }
    }

    private static void AssertSameChains(int size, PointState[] colouring, IGoEngine reference)
    {
        GroupTracker tracker = new(size);
        for (int i = 0; i < colouring.Length; i++)
        {
            if (colouring[i] != PointState.Empty)
            {
                tracker.Place(i, colouring[i] == PointState.Black ? Color.Black : Color.White);
            }
        }

        for (int i = 0; i < colouring.Length; i++)
        {
            Point p = tracker.Board.PointAt(i);
            Assert.True(
                reference.GetChain(p).SetEquals(tracker.Members(i).Take(colouring.Length + 1).Select(tracker.Board.PointAt)),
                $"Chain differs at ({p.X}, {p.Y}) on {Describe(size, colouring)}.");
            Assert.True(
                reference.GetLiberties(p).SetEquals(tracker.Liberties(i).Select(tracker.Board.PointAt)),
                $"Liberties differ at ({p.X}, {p.Y}) on {Describe(size, colouring)}.");
        }
    }

    private static string Describe(int size, PointState[] colouring)
    {
        return string.Join("/", colouring.Chunk(size).Select(row => string.Concat(row.Select(s => ".XO"[(int)s]))));
    }
}

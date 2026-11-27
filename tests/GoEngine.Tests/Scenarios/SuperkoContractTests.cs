using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class SuperkoContractTests : EngineContractTests
{
    private static readonly Point KoCapture = new(2, 1);
    private static readonly Point KoRetake = new(1, 1);

    // Ko shape before White's stone at (1,1): White plays it, then Black captures it at (2,1).
    private static readonly string[] KoBeforeWhiteStone =
    [
        ".XO..",
        "X..O.",
        ".XO..",
        ".....",
        ".....",
    ];

    // Ko shape with White's stone at (1,1) in atari: Black captures it at (2,1), White retakes at (1,1).
    private static readonly string[] KoWhiteStoneInAtari =
    [
        ".XO..",
        "XO.O.",
        ".XO..",
        ".....",
        ".....",
    ];

    // Ko A (top left) before White's stone at (1,1), as in KoBeforeWhiteStone; ko B (bottom right) has
    // Black's stone at (4,5) in atari: White captures it at (5,5), Black retakes at (4,5).
    private static readonly string[] DoubleKo =
    [
        ".XO....",
        "X..O...",
        ".XO....",
        ".......",
        "....OX.",
        "...OX.X",
        "....OX.",
    ];

    // Rule 6: a move may not recreate an earlier coloring; rule 7: the coloring is judged after the
    // captured stone is cleared. Rejects comparing the coloring before clearing, which never
    // occurred and so would let the retake through. IGoEngine: the rejection changes nothing; the
    // loaded pass count and counters are non-zero (Black's capture then resets the pass count, so a
    // rejection that resets it is caught by Pass_DoesNotClearHistory_RetakeAfterPassIsSuperko).
    [Fact]
    public void SimpleKo_ImmediateRetake_IsSuperko()
    {
        IGoEngine engine = Load(
            KoBeforeWhiteStone, toMove: Color.White, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        Assert.Equal(MoveResult.Success(0), engine.Play(KoRetake));
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));

        AssertRejected(engine, KoRetake, Color.White, MoveStatus.Superko);
        PositionSnapshot after = engine.Snapshot();
        Assert.Equal(Color.White, after.ToMove);
        Assert.Equal(4, after.BlackStonesRemoved);
        Assert.Equal(7, after.WhiteStonesRemoved);
    }

    // Rule 6: only a repeated coloring is forbidden, not a point. After one exchange elsewhere the
    // retake makes a new coloring. Rejects banning the ko point for good.
    [Fact]
    public void Ko_RetakeAfterExchangeElsewhere_IsOk()
    {
        IGoEngine engine = Load(KoWhiteStoneInAtari);
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(4, 4)));
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(4, 3)));

        Assert.Equal(MoveResult.Success(1), engine.Play(KoRetake));

        AssertBoard(
            engine.Snapshot(),
            ".XO..",
            "XO.O.",
            ".XO..",
            "....X",
            "....O");
    }

    // Rule 6: any earlier coloring counts. Two kos: White fills ko A, Black takes A, White takes B,
    // White retakes A (explicit colour), Black retakes B; the result is the coloring after White's
    // first move, four plies back, while the coloring two plies back differs. Rejects checking only
    // two plies back (simple ko) or only the previous coloring.
    [Fact]
    public void DoubleKo_RepeatFourPliesBack_IsSuperko()
    {
        IGoEngine engine = Load(DoubleKo, toMove: Color.White);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1)));
        PositionSnapshot repeated = engine.Snapshot();
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(2, 1)));
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(5, 5)));
        PositionSnapshot twoPliesBack = engine.Snapshot();
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(1, 1), Color.White));

        Assert.False(SameColoring(repeated, twoPliesBack));
        AssertRejected(engine, new Point(4, 5), Color.Black, MoveStatus.Superko);
    }

    // Rule 6: a coloring made by a Black move with a capture counts like any other. On the double-ko
    // board Black takes ko A, White takes ko B, and Black's retake of B recreates the coloring after
    // Black took A. Rejects recording only moves with Captured == 0, or only White's moves.
    [Fact]
    public void RepeatOfColoringMadeByBlackCapture_IsSuperko()
    {
        IGoEngine engine = Load(DoubleKo, toMove: Color.White);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1)));
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(2, 1)));
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(5, 5)));

        AssertRejected(engine, new Point(4, 5), Color.Black, MoveStatus.Superko);
    }

    // Rule 7: the coloring a move makes is the one after both clearings, so an allowed suicide leaves
    // only the white stones. Black then refills (1,0), and the suicide at (0,0) would recreate that
    // coloring. Rejects recording the coloring after clearing the opponent but before clearing own
    // stones, or not recording SelfCapture results.
    [Fact]
    public void RepeatOfColoringMadeBySuicide_IsSuperko()
    {
        IGoEngine engine = Load(
            [
                "X.O..",
                "OO...",
                ".....",
                ".....",
                ".....",
            ],
            allowSuicide: true);
        Assert.Equal(MoveResult.SelfCapture(2), engine.Play(new Point(1, 0)));
        AssertBoard(
            engine.Snapshot(),
            "..O..",
            "OO...",
            ".....",
            ".....",
            ".....");
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 0), Color.Black));

        AssertRejected(engine, new Point(0, 0), Color.Black, MoveStatus.Superko);
    }

    // Rule 6 and IGoEngine: a rejected move changes nothing, history included. Black's suicide at
    // (0,2) is rejected; later White's capture of three stones leaves exactly the coloring that
    // suicide would have made. Rejects recording the coloring of a rejected move.
    [Fact]
    public void RejectedMove_IsNotRecordedInHistory()
    {
        IGoEngine engine = Load(
            [
                "..O",
                "OO.",
                ".XO",
            ]);
        AssertRejected(engine, new Point(0, 2), Color.Black, MoveStatus.Suicide);
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(2, 1)));
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(0, 2), Color.Black));

        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 2)));

        AssertBoard(
            engine.Snapshot(),
            "..O",
            "OO.",
            "..O");
    }

    // Comment 6 and IGoEngine: superko is positional, so only the coloring is compared. The retake
    // would recreate the loaded coloring with another player to move, another pass count and other
    // removed counters. Rejects PositionSnapshot.Equals as the key and the turn (or pass count) in it.
    [Fact]
    public void Repetition_WithOtherTurnAndCounters_IsSuperko()
    {
        IGoEngine engine = Load(
            KoWhiteStoneInAtari, toMove: Color.White, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        PositionSnapshot first = engine.Snapshot();
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture, Color.Black));
        PositionSnapshot beforeRetake = engine.Snapshot();

        // An accepted retake by White would leave Black to move, no passes and one more Black stone removed.
        Assert.NotEqual(Color.White.Opposite(), first.ToMove);
        Assert.NotEqual(0, first.ConsecutivePasses);
        Assert.NotEqual(beforeRetake.BlackStonesRemoved + 1, first.BlackStonesRemoved);
        Assert.NotEqual(beforeRetake.WhiteStonesRemoved, first.WhiteStonesRemoved);
        AssertRejected(engine, KoRetake, Color.White, MoveStatus.Superko);
    }

    // Rule 7: an allowed single-stone suicide clears the stone and leaves the current coloring, which
    // rule 6 forbids. IGoEngine: Suicide is checked before Superko, so without suicide allowed the
    // same move is Suicide. Rejects returning SelfCapture(1) and the reverse check order.
    [Theory]
    [InlineData(true, MoveStatus.Superko)]
    [InlineData(false, MoveStatus.Suicide)]
    public void SingleStoneSuicide_IsSuperkoIfAllowedElseSuicide(bool allowSuicide, MoveStatus expected)
    {
        IGoEngine engine = Load(
            [
                ".O...",
                "O....",
                ".....",
                ".....",
                ".....",
            ],
            consecutivePasses: 1,
            blackStonesRemoved: 4,
            whiteStonesRemoved: 6,
            allowSuicide: allowSuicide);

        AssertRejected(engine, new Point(0, 0), Color.Black, expected);
    }

    // Rule 5: the game starts with an empty grid, so the empty coloring is the first history entry.
    // Black fills the board with explicit colour; while the board is not full, a black-only chain
    // always has a liberty. The last stone leaves no empty point, rule 7 clears every stone, and the
    // empty board would return; on 3x3 it is 9 entries back. Rejects a history that starts with the
    // first move, and checking only the last few colorings.
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void FullBoardSuicide_RecreatingEmptyBoard_IsSuperko(int size)
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(size, allowSuicide: true));
        for (int i = 0; i < size * size - 1; i++)
        {
            Assert.Equal(MoveResult.Success(0), engine.Play(new Point(i % size, i / size), Color.Black));
        }

        AssertRejected(engine, new Point(size - 1, size - 1), Color.Black, MoveStatus.Superko);
    }

    // Project decision 3.1 and IGoEngine ("the starting one included"): a loaded position is the first
    // history entry. White has just taken the ko; Black retakes, and White's retake would recreate
    // the loaded coloring. Rejects a history that starts with the first move after loading.
    [Fact]
    public void LoadedPosition_IsFirstHistoryEntry()
    {
        IGoEngine engine = Load(KoWhiteStoneInAtari, blackStonesRemoved: 3, whiteStonesRemoved: 2);

        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));

        AssertRejected(engine, KoRetake, Color.White, MoveStatus.Superko);
    }

    // Rule 6 and IGoEngine: a pass never violates superko, although its coloring repeats the current
    // one, and it does not clear the history. Rejects superko on a pass, a pass that clears the
    // history, and checking only two plies back. The rejection keeps the pass count of 1.
    [Fact]
    public void Pass_DoesNotClearHistory_RetakeAfterPassIsSuperko()
    {
        IGoEngine engine = Load(KoBeforeWhiteStone, toMove: Color.White, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        Assert.Equal(MoveResult.Success(0), engine.Play(KoRetake));
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        PositionSnapshot beforePass = engine.Snapshot();

        Assert.Equal(MoveResult.Success(0), engine.Pass());
        PositionSnapshot afterPass = engine.Snapshot();
        Assert.True(SameColoring(beforePass, afterPass));
        Assert.Equal(1, afterPass.ConsecutivePasses);

        AssertRejected(engine, KoRetake, Color.White, MoveStatus.Superko);
    }
}

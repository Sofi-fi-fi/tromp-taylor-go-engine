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

    // Rules 6 and 7, IGoEngine.
    // Catches: comparing the coloring before the capture is cleared, which never occurred.
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

    // Rule 6.
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

    // Rule 6.
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

    // Rule 6.
    [Fact]
    public void RepeatOfColoringMadeByBlackCapture_IsSuperko()
    {
        IGoEngine engine = Load(DoubleKo, toMove: Color.White);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1)));
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(2, 1)));
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(5, 5)));

        AssertRejected(engine, new Point(4, 5), Color.Black, MoveStatus.Superko);
    }

    // Rule 7.
    // Catches: recording the coloring before the mover's own stones are cleared.
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

    // Rule 6 and IGoEngine.
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

    // Comment 6 and IGoEngine.
    // Catches: PositionSnapshot.Equals (turn, passes, counters) as the history key.
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

    // Rules 6 and 7, IGoEngine: Suicide is checked before Superko.
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

    // Rules 5 and 7.
    // Catches: a history that starts with the first move, or checks only recent colorings.
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

    // IGoEngine and design §6: a loaded position is the first history entry.
    [Fact]
    public void LoadedPosition_IsFirstHistoryEntry()
    {
        IGoEngine engine = Load(KoWhiteStoneInAtari, blackStonesRemoved: 3, whiteStonesRemoved: 2);

        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));

        AssertRejected(engine, KoRetake, Color.White, MoveStatus.Superko);
    }

    // Rule 6 and IGoEngine.
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

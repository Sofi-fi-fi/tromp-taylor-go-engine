using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class UndoContractTests : EngineContractTests
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

    private static string[] SparseBoard()
    {
        return
        [
            ".....",
            ".X...",
            ".....",
            "...O.",
            ".....",
        ];
    }

    // IGoEngine.
    [Fact]
    public void FreshGame_Undo_ReturnsFalse_StateUnchanged()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));
        EngineState before = CaptureState(engine);

        Assert.False(engine.Undo());
        AssertSameState(before, engine);
    }

    // IGoEngine.
    // Catches: an undo back to the empty board, or a stack bottom rebuilt from defaults.
    [Fact]
    public void LoadedPosition_UndoStopsAtLoadedState()
    {
        IGoEngine engine = Load(
            SparseBoard(), toMove: Color.White, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        EngineState loaded = CaptureState(engine);

        Assert.False(engine.Undo());
        AssertSameState(loaded, engine);

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        Assert.True(engine.Undo());
        AssertSameState(loaded, engine);

        Assert.False(engine.Undo());
        AssertSameState(loaded, engine);
    }

    // Rule 7 and IGoEngine.
    [Fact]
    public void UndoCapture_RestoresStonesCountersAndPassCount()
    {
        IGoEngine engine = Load(
            [
                "OO.OX",
                "XX.X.",
                ".....",
                ".....",
                ".....",
            ],
            consecutivePasses: 1,
            blackStonesRemoved: 4,
            whiteStonesRemoved: 6);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 0)));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
    }

    // Rule 7 and IGoEngine.
    // Catches: an undo that restores the colouring but not the chain data of a re-used freed point.
    [Fact]
    public void UndoTwice_AfterCaptureAndPlayOnFreedPoint_RestoresStateBeforeCapture()
    {
        IGoEngine engine = Load(
            ".XX..",
            "XOOX.",
            ".X...",
            ".....",
            ".....");
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(2), engine.Play(new Point(2, 2)));
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1)));

        Assert.True(engine.Undo());
        Assert.True(engine.Undo());
        AssertSameState(before, engine);
    }

    // Rule 7 and IGoEngine.
    [Fact]
    public void UndoSelfCapture_RestoresOwnStonesAndCounter()
    {
        IGoEngine engine = Load(
            [
                ".OOO.",
                "OX.XO",
                ".OOO.",
                ".....",
                ".....",
            ],
            blackStonesRemoved: 4,
            whiteStonesRemoved: 6,
            allowSuicide: true);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.SelfCapture(3), engine.Play(new Point(2, 1)));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
    }

    // Rule 6 and IGoEngine.
    // Catches: the turn derived from the passing colour (all four colour pairs).
    [Theory]
    [InlineData(Color.Black, Color.Black)]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.White, Color.Black)]
    [InlineData(Color.White, Color.White)]
    public void UndoPass_RestoresPassCountAndTurnBefore(Color toMove, Color passer)
    {
        IGoEngine engine = Load(SparseBoard(), toMove: toMove, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(0), engine.Pass(passer));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
    }

    // IGoEngine.
    // Catches: the turn derived from the undone stone's colour (all four colour pairs).
    [Theory]
    [InlineData(Color.Black, Color.Black)]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.White, Color.Black)]
    [InlineData(Color.White, Color.White)]
    public void UndoMove_RestoresTurnBefore(Color toMove, Color mover)
    {
        IGoEngine engine = Load(
            SparseBoard(), toMove: toMove, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2), mover));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
    }

    // Rule 8 and IGoEngine.
    [Fact]
    public void UndoAfterGameOver_GameContinues()
    {
        IGoEngine engine = Load(SparseBoard(), consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.True(engine.Snapshot().IsOver);
        Assert.Equal(MoveResult.Rejected(MoveStatus.GameOver), engine.Play(new Point(2, 2)));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
        Assert.False(engine.Snapshot().IsOver);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
    }

    // Rule 6 and IGoEngine.
    [Fact]
    public void PlayUndoSamePlay_IsOk()
    {
        IGoEngine engine = Load(SparseBoard());
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        EngineState afterFirst = CaptureState(engine);

        Assert.True(engine.Undo());
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        AssertSameState(afterFirst, engine);
    }

    // Rule 6 and IGoEngine.
    [Fact]
    public void UndoKoCapture_RollsHistoryBack()
    {
        IGoEngine engine = Load(KoBeforeWhiteStone, toMove: Color.White);
        Assert.Equal(MoveResult.Success(0), engine.Play(KoRetake));
        EngineState beforeCapture = CaptureState(engine);
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Superko), engine.Play(KoRetake));

        Assert.True(engine.Undo());
        AssertSameState(beforeCapture, engine);

        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Superko), engine.Play(KoRetake));
    }

    // IGoEngine.
    [Fact]
    public void RejectedMoves_AreNotUndone_UndoRevertsLastAcceptedMove()
    {
        IGoEngine engine = Load(
            [
                ".O...",
                "O....",
                ".....",
                ".....",
                ".....",
            ],
            blackStonesRemoved: 4,
            whiteStonesRemoved: 6);
        EngineState before = CaptureState(engine);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2), Color.White));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Occupied), engine.Play(new Point(2, 2)));
        Assert.Equal(MoveResult.Rejected(MoveStatus.OutOfBounds), engine.Play(new Point(5, 0)));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Suicide), engine.Play(new Point(0, 0)));

        Assert.True(engine.Undo());
        AssertSameState(before, engine);
        Assert.False(engine.Undo());
        AssertSameState(before, engine);
    }

    // IGoEngine.
    [Fact]
    public void SeveralUndos_PassThroughEverySnapshot_BackToStart()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));
        List<EngineState> states = [CaptureState(engine)];
        Func<MoveResult>[] moves =
        [
            () => engine.Play(new Point(1, 0)),
            () => engine.Play(new Point(0, 0)),
            () => engine.Play(new Point(0, 1)),
            () => engine.Pass(),
            () => engine.Play(new Point(3, 3), Color.White),
            () => engine.Pass(),
        ];
        MoveResult[] expected =
        [
            MoveResult.Success(0),
            MoveResult.Success(0),
            MoveResult.Success(1),
            MoveResult.Success(0),
            MoveResult.Success(0),
            MoveResult.Success(0),
        ];
        for (int i = 0; i < moves.Length; i++)
        {
            Assert.Equal(expected[i], moves[i]());
            states.Add(CaptureState(engine));
        }

        for (int i = moves.Length - 1; i >= 0; i--)
        {
            Assert.True(engine.Undo());
            AssertSameState(states[i], engine);
        }

        Assert.False(engine.Undo());
        AssertSameState(states[0], engine);
    }
}

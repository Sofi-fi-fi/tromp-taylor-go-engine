using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class UndoContractTests
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

    protected abstract IGoEngine CreateEngine(RuleSet rules);

    protected abstract IGoEngine CreateEngine(RuleSet rules, PositionSnapshot position);

    private IGoEngine Load(
        string[] rows,
        Color toMove = Color.Black,
        int consecutivePasses = 0,
        int blackStonesRemoved = 0,
        int whiteStonesRemoved = 0,
        bool allowSuicide = false)
    {
        PositionSnapshot board = Diagram.Parse(rows);
        PositionSnapshot position = new(
            board.Size,
            [.. Enumerable.Range(0, board.Size * board.Size).Select(i => board[new Point(i % board.Size, i / board.Size)])],
            toMove,
            consecutivePasses,
            blackStonesRemoved,
            whiteStonesRemoved);
        return CreateEngine(RuleSet.CreateForTests(board.Size, allowSuicide: allowSuicide), position);
    }

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

    // IGoEngine: Undo returns false when no Play or Pass was accepted, and changes nothing.
    // Rejects an undo that reports success or resets the game.
    [Fact]
    public void FreshGame_Undo_ReturnsFalse_StateUnchanged()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));
        PositionSnapshot before = engine.Snapshot();

        Assert.False(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // IGoEngine: a loaded position has no accepted Play or Pass behind it, so there is nothing to
    // undo; after one move, undo returns to the loaded state exactly (White to move, pass count 1,
    // removed counters 4 and 6), and a second undo finds nothing. Rejects an undo that goes back to
    // the empty board, and rebuilding the bottom of the undo stack from defaults.
    [Fact]
    public void LoadedPosition_UndoStopsAtLoadedState()
    {
        IGoEngine engine = Load(
            SparseBoard(), toMove: Color.White, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        PositionSnapshot loaded = engine.Snapshot();

        Assert.False(engine.Undo());
        Assert.Equal(loaded, engine.Snapshot());

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        Assert.True(engine.Undo());
        Assert.Equal(loaded, engine.Snapshot());

        Assert.False(engine.Undo());
        Assert.Equal(loaded, engine.Snapshot());
    }

    // Rule 7 and IGoEngine: undo of a capture puts the three captured stones back and restores the
    // White removed counter and the pass count of 1 that the move had reset. Rejects restoring the
    // board only, subtracting from the wrong counter, and setting the pass count to 0.
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
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 0)));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // Rule 7 and IGoEngine: undo of an allowed suicide puts the mover's own stones back and restores
    // the mover's removed counter. Rejects undo that handles only opponent captures.
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
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.SelfCapture(3), engine.Play(new Point(2, 1)));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // Rule 6 and IGoEngine: undo of a pass restores the pass count and the turn that was there
    // before. Every pair of player to move and passing colour: when they differ, the turn before is
    // not the colour that passed; when they match, it is not the turn after the pass. Rejects
    // deriving the turn from the passing colour and keeping the turn after the pass.
    [Theory]
    [InlineData(Color.Black, Color.Black)]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.White, Color.Black)]
    [InlineData(Color.White, Color.White)]
    public void UndoPass_RestoresPassCountAndTurnBefore(Color toMove, Color passer)
    {
        IGoEngine engine = Load(SparseBoard(), toMove: toMove, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.Success(0), engine.Pass(passer));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // IGoEngine: undo of a move restores the turn that was there before, not one derived from the
    // undone stone. Every pair of player to move and stone colour: see UndoPass_RestoresPassCountAndTurnBefore.
    // Rejects setting the turn to the colour of the undone stone (the opposite of the current turn)
    // and keeping the current turn.
    [Theory]
    [InlineData(Color.Black, Color.Black)]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.White, Color.Black)]
    [InlineData(Color.White, Color.White)]
    public void UndoMove_RestoresTurnBefore(Color toMove, Color mover)
    {
        IGoEngine engine = Load(
            SparseBoard(), toMove: toMove, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2), mover));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // Rule 8 and IGoEngine: Undo still works after the game has ended; undoing the second pass
    // leaves one pass, so the game is not over and the next move is accepted. A rejected GameOver
    // move before the undo is not undone instead. Rejects an undo that keeps the game over.
    [Fact]
    public void UndoAfterGameOver_GameContinues()
    {
        IGoEngine engine = Load(SparseBoard(), consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6);
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.True(engine.Snapshot().IsOver);
        Assert.Equal(MoveResult.Rejected(MoveStatus.GameOver), engine.Play(new Point(2, 2)));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
        Assert.False(engine.Snapshot().IsOver);
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
    }

    // Rule 6 and IGoEngine: undo restores the superko history too, so the same move again makes a
    // coloring that is no longer earlier. Rejects an undo that leaves the undone coloring in history.
    [Fact]
    public void PlayUndoSamePlay_IsOk()
    {
        IGoEngine engine = Load(SparseBoard());
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        PositionSnapshot afterFirst = engine.Snapshot();

        Assert.True(engine.Undo());
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        Assert.Equal(afterFirst, engine.Snapshot());
    }

    // Rule 6 and IGoEngine: after a ko capture White's retake is Superko. Undo of the capture rolls
    // the history back to before it, so Black's capture is legal again, and the retake after it is
    // again Superko. Rejects an undo that leaves the capture's coloring in history, and one that
    // clears the whole history.
    [Fact]
    public void UndoKoCapture_RollsHistoryBack()
    {
        IGoEngine engine = Load(KoBeforeWhiteStone, toMove: Color.White);
        Assert.Equal(MoveResult.Success(0), engine.Play(KoRetake));
        PositionSnapshot beforeCapture = engine.Snapshot();
        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Superko), engine.Play(KoRetake));

        Assert.True(engine.Undo());
        Assert.Equal(beforeCapture, engine.Snapshot());

        Assert.Equal(MoveResult.Success(1), engine.Play(KoCapture));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Superko), engine.Play(KoRetake));
    }

    // IGoEngine: a rejected move changes nothing, history included, so one undo after rejected
    // moves of several kinds undoes the last accepted move, and a second finds nothing left.
    // Rejects putting rejected moves on the undo stack.
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
        PositionSnapshot before = engine.Snapshot();
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2), Color.White));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Occupied), engine.Play(new Point(2, 2)));
        Assert.Equal(MoveResult.Rejected(MoveStatus.OutOfBounds), engine.Play(new Point(5, 0)));
        Assert.Equal(MoveResult.Rejected(MoveStatus.Suicide), engine.Play(new Point(0, 0)));

        Assert.True(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
        Assert.False(engine.Undo());
        Assert.Equal(before, engine.Snapshot());
    }

    // IGoEngine: each undo reverts one accepted Play or Pass, so undoing a game of captures, passes
    // and explicit colours step by step passes through every earlier snapshot back to the start.
    // Rejects an undo that skips or merges steps, and one that loses state on the way.
    [Fact]
    public void SeveralUndos_PassThroughEverySnapshot_BackToStart()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));
        List<PositionSnapshot> snapshots = [engine.Snapshot()];
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
            snapshots.Add(engine.Snapshot());
        }

        for (int i = moves.Length - 1; i >= 0; i--)
        {
            Assert.True(engine.Undo());
            Assert.Equal(snapshots[i], engine.Snapshot());
        }

        Assert.False(engine.Undo());
        Assert.Equal(snapshots[0], engine.Snapshot());
    }
}

using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class MoveContractTests
{
    protected abstract IGoEngine CreateEngine(RuleSet rules);

    protected abstract IGoEngine CreateEngine(RuleSet rules, PositionSnapshot position);

    private IGoEngine Load(params string[] rows)
    {
        return Load(allowSuicide: false, rows);
    }

    private IGoEngine LoadAllowingSuicide(params string[] rows)
    {
        return Load(allowSuicide: true, rows);
    }

    private IGoEngine Load(bool allowSuicide, string[] rows)
    {
        PositionSnapshot position = Diagram.Parse(rows);
        return CreateEngine(RuleSet.CreateForTests(position.Size, allowSuicide: allowSuicide), position);
    }

    // A pass count and removed counters that a rejected move must not reset or touch.
    private IGoEngine LoadMidGame(params string[] rows)
    {
        PositionSnapshot board = Diagram.Parse(rows);
        return CreateEngine(
            RuleSet.CreateForTests(board.Size),
            WithState(board, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6));
    }

    private static PositionSnapshot WithState(
        PositionSnapshot board,
        int consecutivePasses,
        int blackStonesRemoved,
        int whiteStonesRemoved,
        Color toMove = Color.Black)
    {
        return new PositionSnapshot(
            board.Size,
            [.. Enumerable.Range(0, board.Size * board.Size).Select(i => board[new Point(i % board.Size, i / board.Size)])],
            toMove,
            consecutivePasses,
            blackStonesRemoved,
            whiteStonesRemoved);
    }

    // Diagrams in colour theories draw the mover as X; for White the colours are swapped.
    private static string[] ForMover(Color mover, params string[] rows)
    {
        return mover == Color.Black
            ? rows
            : [.. rows.Select(r => new string([.. r.Select(c => c switch { 'X' => 'O', 'O' => 'X', _ => c })]))];
    }

    private static void AssertBoard(PositionSnapshot actual, params string[] rows)
    {
        PositionSnapshot expected = Diagram.Parse(rows);
        Assert.Equal(expected.Size, actual.Size);
        for (int y = 0; y < expected.Size; y++)
        {
            for (int x = 0; x < expected.Size; x++)
            {
                Assert.True(
                    expected[new Point(x, y)] == actual[new Point(x, y)],
                    $"Point ({x}, {y}): expected {expected[new Point(x, y)]}, actual {actual[new Point(x, y)]}.");
            }
        }
    }

    private static void AssertRejected(IGoEngine engine, Point point, MoveStatus status)
    {
        PositionSnapshot before = engine.Snapshot();

        Assert.Equal(MoveResult.Rejected(status), engine.Play(point));
        Assert.Equal(before, engine.Snapshot());
    }

    private static void AssertRejected(IGoEngine engine, string call, MoveStatus status)
    {
        PositionSnapshot before = engine.Snapshot();

        Assert.Equal(MoveResult.Rejected(status), Call(engine, call));
        Assert.Equal(before, engine.Snapshot());
    }

    // Black to move. (0,0) is suicide for Black, (2,1) is occupied, (3,3) is empty.
    private static PositionSnapshot EndgameBoard()
    {
        return Diagram.Parse(
            ".O...",
            "O.X..",
            ".....",
            ".....",
            ".....");
    }

    private static MoveResult Call(IGoEngine engine, string call)
    {
        return call switch
        {
            "Play empty" => engine.Play(new Point(3, 3)),
            "Play occupied" => engine.Play(new Point(2, 1)),
            "Play outside" => engine.Play(new Point(-1, 0)),
            "Play suicide" => engine.Play(new Point(0, 0)),
            "Play with colour" => engine.Play(new Point(3, 3), Color.White),
            "Pass" => engine.Pass(),
            "Pass with colour" => engine.Pass(Color.White),
            _ => throw new ArgumentOutOfRangeException(nameof(call), call, "Unknown call."),
        };
    }

    private static void AssertPoints(IEnumerable<Point> expected, IReadOnlySet<Point> actual)
    {
        Assert.Equal(Sorted(expected), Sorted(actual));
    }

    private static List<Point> Sorted(IEnumerable<Point> points)
    {
        return [.. points.OrderBy(p => p.Y).ThenBy(p => p.X)];
    }

    // Rule 5: Black moves first on an empty grid, then the turn alternates.
    // Rule 7: a move colours an empty point; nothing is cleared here.
    [Fact]
    public void EmptyBoard_PlaysBlackStone_TurnGoesToWhite()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            ".....",
            ".....",
            "..X..",
            ".....",
            ".....");
        Assert.Equal(Color.White, after.ToMove);
        Assert.Equal(0, after.ConsecutivePasses);
        Assert.Equal(0, after.BlackStonesRemoved);
        Assert.Equal(0, after.WhiteStonesRemoved);
    }

    // Rule 5: players alternate, so without a colour the second move is White's.
    [Fact]
    public void PlayWithoutColour_UsesPlayerToMove()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(0, 0)));
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1)));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            "X....",
            ".O...",
            ".....",
            ".....",
            ".....");
        Assert.Equal(Color.Black, after.ToMove);
    }

    // Rule 7: a move colours a point the mover's colour. IGoEngine: an explicit colour may repeat,
    // and the turn goes to the opposite of the colour that moved, not of the previous player to move.
    [Fact]
    public void ExplicitBlackTwice_IsAccepted_TurnGoesToWhiteEachTime()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(0, 0), Color.Black));
        Assert.Equal(Color.White, engine.Snapshot().ToMove);

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 1), Color.Black));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            "X....",
            ".X...",
            ".....",
            ".....",
            ".....");
        Assert.Equal(Color.White, after.ToMove);
    }

    // Rule 7 and IGoEngine: White may move first with an explicit colour; the turn then goes to Black.
    [Fact]
    public void ExplicitWhiteOnFreshBoard_TurnGoesToBlack()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(0, 0), Color.White));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            "O....",
            ".....",
            ".....",
            ".....",
            ".....");
        Assert.Equal(Color.Black, after.ToMove);
    }

    // Rule 1: points exist only on the grid. IGoEngine: such a point is rejected, not thrown,
    // and a rejection changes nothing.
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void PointOutsideBoard_IsRejectedAsOutOfBounds(int x, int y)
    {
        IGoEngine engine = LoadMidGame(
            ".....",
            ".X...",
            ".....",
            "...O.",
            ".....");

        AssertRejected(engine, new Point(x, y), MoveStatus.OutOfBounds);
    }

    // Rule 7: a move colours an empty point, so any stone, own or opponent, blocks it.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void OccupiedPoint_IsRejectedAsOccupied(int x, int y)
    {
        IGoEngine engine = LoadMidGame(
            ".....",
            ".XO..",
            ".....",
            ".....",
            ".....");

        AssertRejected(engine, new Point(x, y), MoveStatus.Occupied);
    }

    // Rule 7: after colouring, the opponent colour is cleared; the freed point becomes a liberty.
    [Fact]
    public void CaptureInMiddle_RemovesStone_CapturingStoneGainsLiberty()
    {
        IGoEngine engine = Load(
            ".....",
            "..X..",
            ".XO..",
            "..X..",
            ".....");

        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(3, 2)));

        AssertBoard(
            engine.Snapshot(),
            ".....",
            "..X..",
            ".X.X.",
            "..X..",
            ".....");
        AssertPoints([new(3, 1), new(2, 2), new(4, 2), new(3, 3)], engine.GetLiberties(new Point(3, 2)));
    }

    // Rule 7: clearing on the edge, where the captured stone has only three neighbours.
    [Fact]
    public void CaptureOnEdge_RemovesStone_CapturingStoneGainsLiberty()
    {
        IGoEngine engine = Load(
            ".XO..",
            "..X..",
            ".....",
            ".....",
            ".....");

        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(3, 0)));

        AssertBoard(
            engine.Snapshot(),
            ".X.X.",
            "..X..",
            ".....",
            ".....",
            ".....");
        AssertPoints([new(2, 0), new(4, 0), new(3, 1)], engine.GetLiberties(new Point(3, 0)));
    }

    // Rule 7: clearing in the corner, where the captured stone has only two neighbours.
    [Fact]
    public void CaptureInCorner_RemovesStone_CapturingStoneGainsLiberty()
    {
        IGoEngine engine = Load(
            "OX...",
            ".....",
            ".....",
            ".....",
            ".....");

        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(0, 1)));

        AssertBoard(
            engine.Snapshot(),
            ".X...",
            "X....",
            ".....",
            ".....",
            ".....");
        AssertPoints([new(0, 0), new(1, 1), new(0, 2)], engine.GetLiberties(new Point(0, 1)));
    }

    // Rule 7: clearing the opponent colour empties every opponent string that does not reach empty,
    // so one move removes both strings and Captured counts all their stones.
    [Fact]
    public void CaptureOfTwoChains_CapturedIsSumOfBoth()
    {
        IGoEngine engine = Load(
            "OO.OX",
            "XX.X.",
            ".....",
            ".....",
            ".....");

        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 0)));

        AssertBoard(
            engine.Snapshot(),
            "..X.X",
            "XX.X.",
            ".....",
            ".....",
            ".....");
    }

    // Rule 7 and comment 7: the opponent colour is cleared before the mover's own, so a stone with
    // no empty neighbour that captures is not suicide: rejected neither without suicide allowed
    // nor self-captured with it, since the own chain never lacks liberties.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaptureBeforeSuicide_PlayedStoneStays(bool allowSuicide)
    {
        IGoEngine engine = Load(
            allowSuicide,
            [
                ".XO..",
                "XO.O.",
                ".XO..",
                ".....",
                ".....",
            ]);

        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(2, 1)));

        AssertBoard(
            engine.Snapshot(),
            ".XO..",
            "X.XO.",
            ".XO..",
            ".....",
            ".....");
        AssertPoints([new(1, 1)], engine.GetLiberties(new Point(2, 1)));
    }

    // Rule 7: the played stone joins the Black chain (0,0)-(1,0), whose only liberty it fills, and
    // touches no empty point; clearing White's (0,1) gives the merged chain its liberty back, so the
    // move stands. Unlike CaptureBeforeSuicide_PlayedStoneStays the freed chain has several stones.
    // Rejects judging suicide, or keeping the merged chain's liberties, from the board before the
    // capture: (0,0) is not next to the played point, yet its chain must gain (0,1).
    [Fact]
    public void CaptureFreesMergedOwnChain_MoveStands()
    {
        IGoEngine engine = Load(
            "XXO..",
            "O.O..",
            "XO...",
            ".....",
            ".....");

        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(1, 1)));

        AssertBoard(
            engine.Snapshot(),
            "XXO..",
            ".XO..",
            "XO...",
            ".....",
            ".....");
        AssertPoints([new(0, 0), new(1, 0), new(1, 1)], engine.GetChain(new Point(1, 1)));
        AssertPoints([new(0, 1)], engine.GetLiberties(new Point(0, 0)));
    }

    // Rule 7: every opponent string without liberties is cleared, whichever side of the played stone
    // it lies on. Here four White stones are captured above, left, right and below. Rejects a capture
    // loop that skips a direction: no other test captures downward.
    [Fact]
    public void CaptureInFourDirections_RemovesAllFourStones()
    {
        IGoEngine engine = Load(
            "..X..",
            ".XOX.",
            "XO.OX",
            ".XOX.",
            "..X..");

        Assert.Equal(MoveResult.Success(4), engine.Play(new Point(2, 2)));

        AssertBoard(
            engine.Snapshot(),
            "..X..",
            ".X.X.",
            "X.X.X",
            ".X.X.",
            "..X..");
        AssertPoints([new(2, 1), new(1, 2), new(3, 2), new(2, 3)], engine.GetLiberties(new Point(2, 2)));
    }

    // Comment 7: a move that would clear the mover's own colour is suicide; comment 4: no string
    // without liberties may remain, so with suicide not allowed the move is rejected.
    [Fact]
    public void SingleStoneSuicide_NotAllowed_IsRejected()
    {
        IGoEngine engine = LoadMidGame(
            ".O...",
            "O....",
            ".....",
            ".....",
            ".....");

        AssertRejected(engine, new Point(0, 0), MoveStatus.Suicide);
    }

    // Comment 7 and comment 4: joining an own string and leaving it without liberties is suicide too.
    [Fact]
    public void MultiStoneSuicide_NotAllowed_IsRejected()
    {
        IGoEngine engine = LoadMidGame(
            "X.O..",
            "OO...",
            ".....",
            ".....",
            ".....");

        AssertRejected(engine, new Point(1, 0), MoveStatus.Suicide);
    }

    // Rule 7: with suicide allowed, the mover's own string is cleared, the played stone included,
    // and the turn passes. Only multi-stone strings: a single-stone suicide recreates the current
    // colouring and is Superko (SuperkoContractTests.SingleStoneSuicide_IsSuperkoIfAllowedElseSuicide).
    [Fact]
    public void SuicideInCorner_Allowed_SelfCapturesChain()
    {
        IGoEngine engine = LoadAllowingSuicide(
            "X.O..",
            "OO...",
            ".....",
            ".....",
            ".....");

        Assert.Equal(MoveResult.SelfCapture(2), engine.Play(new Point(1, 0)));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            "..O..",
            "OO...",
            ".....",
            ".....",
            ".....");
        Assert.Equal(Color.White, after.ToMove);
    }

    // Rule 7: see SuicideInCorner_Allowed_SelfCapturesChain; the played stone joins two own stones.
    [Fact]
    public void SuicideInMiddle_Allowed_SelfCapturesChain()
    {
        IGoEngine engine = LoadAllowingSuicide(
            ".OOO.",
            "OX.XO",
            ".OOO.",
            ".....",
            ".....");

        Assert.Equal(MoveResult.SelfCapture(3), engine.Play(new Point(2, 1)));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            ".OOO.",
            "O...O",
            ".OOO.",
            ".....",
            ".....");
        Assert.Equal(Color.White, after.ToMove);
    }

    // Rule 7 and IGoEngine: captured stones are added to the opponent's removed counter; the
    // mover's counter does not change. Different non-zero start values catch assignment instead of
    // addition and a swapped counter.
    [Theory]
    [InlineData(Color.Black, 4, 9)]
    [InlineData(Color.White, 7, 6)]
    public void Capture_AddsCapturedToOpponentRemovedCounter(Color mover, int black, int white)
    {
        PositionSnapshot board = Diagram.Parse(ForMover(
            mover,
            "OO.OX",
            "XX.X.",
            ".....",
            ".....",
            "....."));
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(board, consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6));

        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 0), mover));

        PositionSnapshot after = engine.Snapshot();
        Assert.Equal(black, after.BlackStonesRemoved);
        Assert.Equal(white, after.WhiteStonesRemoved);
    }

    // Rule 5 and rule 7: without a colour White, the player to move, captures; Black's counter grows
    // and the turn goes to Black.
    [Fact]
    public void WhiteToMove_PlayWithoutColour_CapturesForWhite()
    {
        PositionSnapshot board = Diagram.Parse(
            "XX.XO",
            "OO.O.",
            ".....",
            ".....",
            ".....");
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(board, consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6, toMove: Color.White));

        Assert.Equal(MoveResult.Success(3), engine.Play(new Point(2, 0)));

        PositionSnapshot after = engine.Snapshot();
        AssertBoard(
            after,
            "..O.O",
            "OO.O.",
            ".....",
            ".....",
            ".....");
        Assert.Equal(7, after.BlackStonesRemoved);
        Assert.Equal(6, after.WhiteStonesRemoved);
        Assert.Equal(Color.Black, after.ToMove);
    }

    // Rule 7 and IGoEngine: self-captured stones are added to the mover's own removed counter.
    [Theory]
    [InlineData(Color.Black, 7, 6)]
    [InlineData(Color.White, 4, 9)]
    public void SelfCapture_AddsSelfCapturedToMoverRemovedCounter(Color mover, int black, int white)
    {
        PositionSnapshot board = Diagram.Parse(ForMover(
            mover,
            ".OOO.",
            "OX.XO",
            ".OOO.",
            ".....",
            "....."));
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5, allowSuicide: true),
            WithState(board, consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6));

        Assert.Equal(MoveResult.SelfCapture(3), engine.Play(new Point(2, 1), mover));

        PositionSnapshot after = engine.Snapshot();
        Assert.Equal(black, after.BlackStonesRemoved);
        Assert.Equal(white, after.WhiteStonesRemoved);
    }

    // Rule 8: only consecutive passes end the game, so a move resets the count.
    [Fact]
    public void AcceptedPlay_ResetsConsecutivePasses()
    {
        PositionSnapshot board = Diagram.Parse(
            ".....",
            ".X...",
            ".....",
            "...O.",
            ".....");
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(board, consecutivePasses: 1, blackStonesRemoved: 0, whiteStonesRemoved: 0));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));

        Assert.Equal(0, engine.Snapshot().ConsecutivePasses);
    }

    // Rule 1: there are only two players. IGoEngine: an invalid argument throws before any rule
    // is checked, so a point outside the board does not turn it into OutOfBounds.
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(3, 5, 0)]
    public void UndefinedColour_Throws(int color, int x, int y)
    {
        IGoEngine engine = Load(
            ".....",
            ".X...",
            ".....",
            ".....",
            ".....");
        PositionSnapshot before = engine.Snapshot();

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Play(new Point(x, y), (Color)color));
        Assert.Equal(before, engine.Snapshot());
    }

    // IGoEngine: returned sets are copies owned by the caller, so a later move does not change them.
    [Fact]
    public void LibertiesTakenBeforeMove_AreNotChangedByMove()
    {
        IGoEngine engine = Load(
            ".....",
            ".XX..",
            ".....",
            ".....",
            ".....");
        IReadOnlySet<Point> before = engine.GetLiberties(new Point(1, 1));

        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(1, 0), Color.White));

        AssertPoints([new(1, 0), new(2, 0), new(0, 1), new(3, 1), new(1, 2), new(2, 2)], before);
        Assert.DoesNotContain(new Point(1, 0), engine.GetLiberties(new Point(1, 1)));
    }

    // Rule 6: a turn is either a pass or a move; a pass colours nothing, so the board and both
    // removed counters stay. Rule 5: the turn alternates. Rule 8: the pass starts a run of passes.
    [Fact]
    public void Pass_MidGame_ChangesOnlyTurnAndPassCount()
    {
        PositionSnapshot board = EndgameBoard();
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(board, consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6));

        Assert.Equal(MoveResult.Success(0), engine.Pass());

        PositionSnapshot after = engine.Snapshot();
        Assert.Equal(
            WithState(board, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6, toMove: Color.White),
            after);
        Assert.False(after.IsOver);
    }

    // Rule 6 and IGoEngine: an explicit colour may pass; the turn goes to the opposite of that colour.
    [Fact]
    public void PassWhiteOnFreshBoard_TurnGoesToBlack()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Pass(Color.White));

        PositionSnapshot after = engine.Snapshot();
        Assert.Equal(Color.Black, after.ToMove);
        Assert.Equal(1, after.ConsecutivePasses);
    }

    // Rule 5 and IGoEngine: without a colour the player to move passes, here White; mirrors
    // WhiteToMove_PlayWithoutColour_CapturesForWhite.
    [Fact]
    public void WhiteToMove_PassWithoutColour_PassesForWhite()
    {
        PositionSnapshot board = EndgameBoard();
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(board, consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6, toMove: Color.White));

        Assert.Equal(MoveResult.Success(0), engine.Pass());

        Assert.Equal(
            WithState(board, consecutivePasses: 1, blackStonesRemoved: 4, whiteStonesRemoved: 6, toMove: Color.Black),
            engine.Snapshot());
    }

    // Rule 8: the game ends after two consecutive passes. IGoEngine: of any colours.
    [Theory]
    [InlineData(Color.Black, Color.White)]
    [InlineData(Color.Black, Color.Black)]
    [InlineData(Color.White, Color.Black)]
    [InlineData(Color.White, Color.White)]
    public void TwoConsecutivePasses_EndGame(Color first, Color second)
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Pass(first));
        Assert.False(engine.Snapshot().IsOver);

        Assert.Equal(MoveResult.Success(0), engine.Pass(second));
        PositionSnapshot after = engine.Snapshot();
        Assert.True(after.IsOver);
        Assert.Equal(2, after.ConsecutivePasses);
    }

    // Rule 8: only consecutive passes end the game, so a move between them breaks the run.
    [Fact]
    public void PassPlayPass_DoesNotEndGame()
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5));

        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.Equal(MoveResult.Success(0), engine.Play(new Point(2, 2)));
        Assert.Equal(MoveResult.Success(0), engine.Pass());

        PositionSnapshot after = engine.Snapshot();
        Assert.False(after.IsOver);
        Assert.Equal(1, after.ConsecutivePasses);
    }

    // Rule 8: after the game has ended there are no more turns. IGoEngine: GameOver is checked
    // first, before OutOfBounds, Occupied and Suicide, and a rejection changes nothing.
    [Theory]
    [InlineData("Play empty")]
    [InlineData("Play occupied")]
    [InlineData("Play outside")]
    [InlineData("Play suicide")]
    [InlineData("Play with colour")]
    [InlineData("Pass")]
    [InlineData("Pass with colour")]
    public void LoadedGameOver_EveryTurnIsRejected(string call)
    {
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(EndgameBoard(), consecutivePasses: 2, blackStonesRemoved: 4, whiteStonesRemoved: 6));

        AssertRejected(engine, call, MoveStatus.GameOver);
    }

    // Rule 8: see LoadedGameOver_EveryTurnIsRejected; here the game ends through two real passes.
    [Theory]
    [InlineData("Play empty")]
    [InlineData("Play occupied")]
    [InlineData("Play outside")]
    [InlineData("Play suicide")]
    [InlineData("Play with colour")]
    [InlineData("Pass")]
    [InlineData("Pass with colour")]
    public void GameEndedByTwoPasses_EveryTurnIsRejected(string call)
    {
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(EndgameBoard(), consecutivePasses: 0, blackStonesRemoved: 4, whiteStonesRemoved: 6));
        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.Equal(MoveResult.Success(0), engine.Pass());

        AssertRejected(engine, call, MoveStatus.GameOver);
    }

    // Rule 1: there are only two players. IGoEngine: an invalid argument throws before any rule
    // is checked, also after the game has ended.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 0)]
    [InlineData(0, 2)]
    [InlineData(3, 2)]
    public void PassUndefinedColour_Throws(int color, int consecutivePasses)
    {
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(EndgameBoard(), consecutivePasses, blackStonesRemoved: 4, whiteStonesRemoved: 6));
        PositionSnapshot before = engine.Snapshot();

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Pass((Color)color));
        Assert.Equal(before, engine.Snapshot());
    }

    // IGoEngine: see PassUndefinedColour_Throws; an undefined colour is not reported as GameOver.
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void PlayUndefinedColour_AfterGameOver_Throws(int color)
    {
        IGoEngine engine = CreateEngine(
            RuleSet.CreateForTests(5),
            WithState(EndgameBoard(), consecutivePasses: 2, blackStonesRemoved: 4, whiteStonesRemoved: 6));
        PositionSnapshot before = engine.Snapshot();

        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Play(new Point(3, 3), (Color)color));
        Assert.Equal(before, engine.Snapshot());
    }
}

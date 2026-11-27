using GoEngine.Core;
using GoEngine.Tests.Support;
using Xunit;

namespace GoEngine.Tests.Scenarios;

public abstract class ScoreContractTests : EngineContractTests
{
    // Removed counters that scoring must ignore: area scoring counts the board only.
    private IGoEngine LoadWithRemoved(string[] rows, decimal komi = 7.5m, int consecutivePasses = 0)
    {
        return Load(rows, consecutivePasses: consecutivePasses, blackStonesRemoved: 4, whiteStonesRemoved: 6, komi: komi);
    }

    // Rule 9: no stones and one empty region reaching no colour, so both areas are 0 and all 25 points
    // are neutral. Rule 10 with komi (comment 10): komi alone decides; integer komi 0 is a tie
    // (Winner null). Rejects counting an empty region that reaches nothing for a player, and
    // ignoring or negating komi.
    [Theory]
    [InlineData(7.5, Color.White, 7.5)]
    [InlineData(0, null, 0)]
    [InlineData(-0.5, Color.Black, 0.5)]
    public void EmptyBoard_KomiDecides(double komi, Color? winner, double margin)
    {
        IGoEngine engine = CreateEngine(RuleSet.CreateForTests(5, (decimal)komi));

        ScoreResult score = engine.Score();

        Assert.Equal(new ScoreResult(0, 0, 25, (decimal)komi), score);
        Assert.Equal(winner, score.Winner);
        Assert.Equal((decimal)margin, score.Margin);
    }

    // Rule 9: stones count as points of their colour; the empty column between the walls reaches both
    // colours and is neutral; the two columns right of the white wall reach only White and count for
    // it. Rejects giving a two-colour region to the first colour found, and counting stones of one
    // colour only.
    [Fact]
    public void RegionReachingBothColours_IsNeutral()
    {
        IGoEngine engine = LoadWithRemoved(
            [
                "X.O..",
                "X.O..",
                "X.O..",
                "X.O..",
                "X.O..",
            ]);

        Assert.Equal(new ScoreResult(5, 15, 5, 7.5m), engine.Score());
    }

    // Rule 9: the two-point region reaching only Black counts for Black, the one-point region reaching
    // only White for White, and the large region reaching both is neutral: Black 4 stones + 2, White
    // 3 stones + 1. The eyes differ in size, so swapped owners would give 5 and 5. Rejects swapping
    // the colours of eyes and counting only the largest region.
    [Fact]
    public void RegionsReachingOneColour_CountForIt()
    {
        IGoEngine engine = LoadWithRemoved(
            [
                "..XO.",
                "XXXOO",
                ".....",
                ".....",
                ".....",
            ],
            komi: 0m);

        ScoreResult score = engine.Score();

        Assert.Equal(new ScoreResult(6, 4, 15, 0m), score);
        Assert.Equal(Color.Black, score.Winner);
    }

    // Rule 3: reaching goes along vertical or horizontal paths only, so the empty corner (0,0), whose
    // orthogonal neighbours are both Black, reaches only Black although the White stone (1,1) touches
    // it diagonally; rule 9 counts it for Black: Black 2 stones + 1, White 1 stone, 21 neutral. With
    // diagonal reach the corner would reach White too and be neutral (2, 1, 22). Rejects 8-connected
    // scoring.
    [Fact]
    public void RegionTouchingOpponentOnlyDiagonally_CountsForOrthogonalColour()
    {
        IGoEngine engine = LoadWithRemoved(
            [
                ".X...",
                "XO...",
                ".....",
                ".....",
                ".....",
            ]);

        Assert.Equal(new ScoreResult(3, 1, 21, 7.5m), engine.Score());
    }

    // Rule 9 literally, with no dead-stone removal: the black stone inside White's corner makes that
    // corner region reach Black too, so it is neutral, and the stone counts for Black. Rejects
    // treating surrounded stones as dead or ignoring them when a region is scored.
    [Fact]
    public void DeadLookingStoneInsideWhiteArea_MakesRegionNeutral()
    {
        IGoEngine engine = LoadWithRemoved(
            [
                "X.O..",
                "..O..",
                "OOO..",
                ".....",
                ".....",
            ]);

        Assert.Equal(new ScoreResult(1, 21, 3, 7.5m), engine.Score());
    }

    // Rule 9 on the board after play: Black captures White's (1,0), both pass, and the score counts
    // the current coloring: Black 11 stones and the 4 empty points of column 1, White 5 stones,
    // column 3 neutral. Rule 10 and comment 10: with komi 10 the scores are equal, a tie. The loaded
    // board would score 10, 6 and 9. Rejects scoring the loaded or initial coloring, and ignoring
    // komi in the comparison.
    [Fact]
    public void ScoreAfterCaptureAndTwoPasses_CountsCurrentBoard_Tie()
    {
        IGoEngine engine = LoadWithRemoved(
            [
                "XOX.O",
                "X.X.O",
                "X.X.O",
                "X.X.O",
                "X.X.O",
            ],
            komi: 10m);
        Assert.Equal(MoveResult.Success(1), engine.Play(new Point(1, 1)));
        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.Equal(MoveResult.Success(0), engine.Pass());
        Assert.True(engine.Snapshot().IsOver);

        ScoreResult score = engine.Score();

        Assert.Equal(new ScoreResult(15, 5, 5, 10m), score);
        Assert.Null(score.Winner);
    }

    // IGoEngine: Score may be called at any time, before or after the game ends, gives the same area
    // score and changes nothing; the game goes on after it. Rejects scoring only finished games
    // and a Score that changes the state.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Score_AnyTime_ChangesNothing(int consecutivePasses)
    {
        IGoEngine engine = LoadWithRemoved(
            [
                "X.O..",
                "X.O..",
                "X.O..",
                "X.O..",
                "X.O..",
            ],
            consecutivePasses: consecutivePasses);
        PositionSnapshot before = engine.Snapshot();

        Assert.Equal(new ScoreResult(5, 15, 5, 7.5m), engine.Score());
        Assert.Equal(before, engine.Snapshot());

        MoveResult expected = consecutivePasses == 2
            ? MoveResult.Rejected(MoveStatus.GameOver)
            : MoveResult.Success(0);
        Assert.Equal(expected, engine.Play(new Point(1, 2)));
    }
}

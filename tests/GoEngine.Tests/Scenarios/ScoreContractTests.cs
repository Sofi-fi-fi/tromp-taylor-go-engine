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

    // Rules 9 and 10, comment 10.
    // No stones; all 25 points reach no colour: 0 : 0, 25 neutral, so komi alone decides.
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

    // Rule 9.
    // Black 5 stones; White 5 stones + 10 empty (columns 3-4); column 1 reaches both: 5 neutral.
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

    // Rule 9.
    // Black 4 stones + 2 (eye at top left); White 3 stones + 1 (top right); rows 2-4: 15 neutral.
    // Catches: swapped eye owners (the eyes differ in size, so a swap changes both scores).
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

    // Rules 3 and 9.
    // Black 2 stones + 1 (corner (0, 0)); White 1 stone; the other 21 points reach both.
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

    // Rule 9: no dead-stone removal.
    // Black 1 stone; White 5 stones + 16 empty outside the corner; (1,0), (0,1), (1,1) reach both: 3 neutral.
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

    // Rules 9 and 10, comment 10.
    // Black 11 stones + 4 empty in column 1 = 15; White 5 stones + komi 10 = 15; column 3: 5 neutral.
    // Catches: scoring the loaded coloring (10, 6 and 9) instead of the current one.
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

    // IGoEngine.
    // As in RegionReachingBothColours_IsNeutral: Black 5, White 5 + 10, column 1 neutral.
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

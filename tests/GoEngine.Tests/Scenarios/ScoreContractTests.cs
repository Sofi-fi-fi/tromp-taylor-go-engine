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

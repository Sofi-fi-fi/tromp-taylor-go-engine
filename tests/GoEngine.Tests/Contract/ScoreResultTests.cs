using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Contract;

public class ScoreResultTests
{
    [Fact]
    public void Constructor_KeepsCountsAndKomi()
    {
        ScoreResult score = new(40, 41, 3, 7.5m);

        Assert.Equal(40, score.BlackArea);
        Assert.Equal(41, score.WhiteArea);
        Assert.Equal(3, score.NeutralPoints);
        Assert.Equal(7.5m, score.Komi);
    }

    [Fact]
    public void WhiteScore_IsWhiteAreaPlusKomi()
    {
        ScoreResult score = new(40, 41, 0, 7.5m);

        Assert.Equal(48.5m, score.WhiteScore);
    }

    public static TheoryData<int, int, int, decimal, Color, decimal> DecisiveScores => new()
    {
        { 40, 41, 0, 7.5m, Color.White, 8.5m },
        { 50, 30, 1, 7.5m, Color.Black, 12.5m },
        { 0, 0, 81, 7.5m, Color.White, 7.5m },
        { 0, 0, 81, -0.5m, Color.Black, 0.5m },
        { 10, 12, 59, -3m, Color.Black, 1m },
        { 40, 40, 1, 0.5m, Color.White, 0.5m },
        { 41, 40, 0, 0.5m, Color.Black, 0.5m },
    };

    [Theory]
    [MemberData(nameof(DecisiveScores))]
    public void Winner_UnequalScores_IsHigherWithMargin(
        int blackArea, int whiteArea, int neutralPoints, decimal komi, Color winner, decimal margin)
    {
        ScoreResult score = new(blackArea, whiteArea, neutralPoints, komi);

        Assert.Equal(winner, score.Winner);
        Assert.False(score.IsDraw);
        Assert.Equal(margin, score.Margin);
    }

    public static TheoryData<int, int, int, decimal> DrawScores => new()
    {
        { 40, 40, 1, 0m },
        { 47, 40, 0, 7m },
    };

    [Theory]
    [MemberData(nameof(DrawScores))]
    public void Winner_EqualScores_IsNullAndDraw(int blackArea, int whiteArea, int neutralPoints, decimal komi)
    {
        ScoreResult score = new(blackArea, whiteArea, neutralPoints, komi);

        Assert.Null(score.Winner);
        Assert.True(score.IsDraw);
        Assert.Equal(0m, score.Margin);
    }

    [Fact]
    public void NeutralPoints_DoNotAffectWinnerOrMargin()
    {
        ScoreResult withoutNeutral = new(40, 41, 0, 7.5m);
        ScoreResult withNeutral = new(40, 41, 100, 7.5m);

        Assert.Equal(withoutNeutral.Winner, withNeutral.Winner);
        Assert.Equal(withoutNeutral.Margin, withNeutral.Margin);
        Assert.Equal(withoutNeutral.IsDraw, withNeutral.IsDraw);
    }

    public static TheoryData<int, int, int, string> NegativeCounts => new()
    {
        { -1, 0, 0, "blackArea" },
        { 0, -1, 0, "whiteArea" },
        { 0, 0, -1, "neutralPoints" },
        { -1, -1, -1, "blackArea" },
    };

    [Theory]
    [MemberData(nameof(NegativeCounts))]
    public void Constructor_NegativeCount_ThrowsNamingFirstNegative(
        int blackArea, int whiteArea, int neutralPoints, string paramName)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScoreResult(blackArea, whiteArea, neutralPoints, 0m));

        Assert.Equal(paramName, ex.ParamName);
    }

    public static TheoryData<decimal> KomiAtBound => new()
    {
        RuleSet.MaxAbsKomi,
        -RuleSet.MaxAbsKomi,
    };

    public static TheoryData<decimal> KomiOutsideBound => new()
    {
        RuleSet.MaxAbsKomi + 0.5m,
        -(RuleSet.MaxAbsKomi + 0.5m),
    };

    [Theory]
    [MemberData(nameof(KomiAtBound))]
    public void Constructor_KomiAtBound_IsKept(decimal komi)
    {
        Assert.Equal(komi, new ScoreResult(40, 41, 0, komi).Komi);
    }

    [Theory]
    [MemberData(nameof(KomiOutsideBound))]
    public void Constructor_KomiOutsideBound_Throws(decimal komi)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScoreResult(40, 41, 0, komi));

        Assert.Equal("komi", ex.ParamName);
    }

    [Fact]
    public void Constructor_NegativeCountAndKomiOutsideBound_NamesCount()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScoreResult(-1, 0, 0, RuleSet.MaxAbsKomi + 0.5m));

        Assert.Equal("blackArea", ex.ParamName);
    }

    [Fact]
    public void Constructor_SameArguments_AreEqualWithSameHash()
    {
        ScoreResult a = new(40, 41, 0, 7.5m);
        ScoreResult b = new(40, 41, 0, 7.5m);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}

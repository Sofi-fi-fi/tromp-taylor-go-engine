using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Contract;

public class RuleSetTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(20)]
    public void Create_SizeOutsidePublicRange_Throws(int size)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.Create(size));

        Assert.Equal("size", ex.ParamName);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(19)]
    public void Create_SizeAtPublicBoundary_KeepsSize(int size)
    {
        Assert.Equal(size, RuleSet.Create(size).Size);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    public void Create_SizeInsidePublicRange_KeepsSize(int size)
    {
        Assert.Equal(size, RuleSet.Create(size).Size);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void CreateForTests_SizeOutsideInternalRange_Throws(int size)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.CreateForTests(size));

        Assert.Equal("size", ex.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(19)]
    public void CreateForTests_SizeAtInternalBoundary_KeepsSize(int size)
    {
        Assert.Equal(size, RuleSet.CreateForTests(size).Size);
    }

    [Fact]
    public void CreateForTests_GivenKomiAndSuicide_AreKept()
    {
        RuleSet rules = RuleSet.CreateForTests(3, 6.5m, allowSuicide: true);

        Assert.Equal(3, rules.Size);
        Assert.Equal(6.5m, rules.Komi);
        Assert.True(rules.AllowSuicide);
    }

    [Fact]
    public void CreateForTests_Defaults_AreKomi7Point5AndNoSuicide()
    {
        RuleSet rules = RuleSet.CreateForTests(3);

        Assert.Equal(7.5m, rules.Komi);
        Assert.False(rules.AllowSuicide);
    }

    [Fact]
    public void Create_Defaults_AreKomi7Point5AndNoSuicide()
    {
        RuleSet rules = RuleSet.Create(9);

        Assert.Equal(7.5m, rules.Komi);
        Assert.False(rules.AllowSuicide);
    }

    public static TheoryData<decimal> KomiInsideRange => new()
    {
        -361m, -0.5m, 0m, 0.25m, 6.5m, 7m, 361m,
    };

    public static TheoryData<decimal> KomiOutsideRange => new()
    {
        -361.5m, 361.5m,
    };

    [Theory]
    [MemberData(nameof(KomiInsideRange))]
    public void Create_GivenKomi_IsKept(decimal komi)
    {
        Assert.Equal(komi, RuleSet.Create(9, komi).Komi);
    }

    [Theory]
    [MemberData(nameof(KomiInsideRange))]
    public void CreateForTests_GivenKomi_IsKept(decimal komi)
    {
        Assert.Equal(komi, RuleSet.CreateForTests(3, komi).Komi);
    }

    [Theory]
    [MemberData(nameof(KomiOutsideRange))]
    public void Create_KomiOutsideRange_Throws(decimal komi)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.Create(9, komi));

        Assert.Equal("komi", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(KomiOutsideRange))]
    public void CreateForTests_KomiOutsideRange_Throws(decimal komi)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.CreateForTests(3, komi));

        Assert.Equal("komi", ex.ParamName);
    }

    public static TheoryData<int, decimal> PublicSizeAndKomiOutOfRange => new()
    {
        { 8, 400m },
        { 8, -400m },
        { 20, 400m },
        { 20, -400m },
    };

    public static TheoryData<int, decimal> InternalSizeAndKomiOutOfRange => new()
    {
        { 1, 400m },
        { 1, -400m },
        { 20, 400m },
        { 20, -400m },
    };

    [Theory]
    [MemberData(nameof(PublicSizeAndKomiOutOfRange))]
    public void Create_SizeAndKomiOutOfRange_NamesSize(int size, decimal komi)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.Create(size, komi));

        Assert.Equal("size", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(InternalSizeAndKomiOutOfRange))]
    public void CreateForTests_SizeAndKomiOutOfRange_NamesSize(int size, decimal komi)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => RuleSet.CreateForTests(size, komi));

        Assert.Equal("size", ex.ParamName);
    }

    [Fact]
    public void Create_AllowSuicideTrue_IsKept()
    {
        Assert.True(RuleSet.Create(9, allowSuicide: true).AllowSuicide);
    }

    [Fact]
    public void Create_SameArguments_AreEqualWithSameHash()
    {
        RuleSet a = RuleSet.Create(13, 6.5m, allowSuicide: true);
        RuleSet b = RuleSet.Create(13, 6.5m, allowSuicide: true);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Create_DifferentKomi_AreNotEqual()
    {
        Assert.NotEqual(RuleSet.Create(9, 6.5m), RuleSet.Create(9, 7.5m));
    }

    [Fact]
    public void Create_DifferentSize_AreNotEqual()
    {
        Assert.NotEqual(RuleSet.Create(9, 6.5m, allowSuicide: false), RuleSet.Create(13, 6.5m, allowSuicide: false));
    }

    [Fact]
    public void Create_DifferentAllowSuicide_AreNotEqual()
    {
        Assert.NotEqual(RuleSet.Create(9, 6.5m, allowSuicide: false), RuleSet.Create(9, 6.5m, allowSuicide: true));
    }
}

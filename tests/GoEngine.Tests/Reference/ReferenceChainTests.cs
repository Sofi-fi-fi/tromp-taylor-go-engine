using GoEngine.Core;
using GoEngine.Reference;
using GoEngine.Tests.Scenarios;
using Xunit;

namespace GoEngine.Tests.Reference;

public sealed class ReferenceChainTests : ChainContractTests
{
    protected override IGoEngine CreateEngine(RuleSet rules, PositionSnapshot position)
    {
        return new ReferenceEngine(rules, position);
    }

    [Fact]
    public void Constructor_NullRules_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new ReferenceEngine(null!));

        Assert.Equal("rules", ex.ParamName);
    }

    [Fact]
    public void ConstructorWithPosition_NullRules_Throws()
    {
        PositionSnapshot position = new(3, new PointState[9], Color.Black, 0, 0, 0);

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new ReferenceEngine(null!, position));

        Assert.Equal("rules", ex.ParamName);
    }

    [Fact]
    public void ConstructorWithPosition_NullPosition_Throws()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new ReferenceEngine(RuleSet.CreateForTests(3), null!));

        Assert.Equal("position", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyBoardWithBlackToMove()
    {
        ReferenceEngine engine = new(RuleSet.CreateForTests(3));

        Assert.Equal(new PositionSnapshot(3, new PointState[9], Color.Black, 0, 0, 0), engine.Snapshot());
    }
}

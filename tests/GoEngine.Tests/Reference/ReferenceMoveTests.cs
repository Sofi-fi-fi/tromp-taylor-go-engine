using GoEngine.Core;
using GoEngine.Reference;
using GoEngine.Tests.Scenarios;

namespace GoEngine.Tests.Reference;

public sealed class ReferenceMoveTests : MoveContractTests
{
    protected override IGoEngine CreateEngine(RuleSet rules)
    {
        return new ReferenceEngine(rules);
    }

    protected override IGoEngine CreateEngine(RuleSet rules, PositionSnapshot position)
    {
        return new ReferenceEngine(rules, position);
    }
}

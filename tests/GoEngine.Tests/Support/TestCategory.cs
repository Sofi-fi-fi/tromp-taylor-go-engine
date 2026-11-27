namespace GoEngine.Tests.Support;

// Values of the "Category" trait: [Trait(TestCategory.Name, TestCategory.Heavy)]. The fast suite
// (CI, Stryker) runs with --filter "Category!=Performance&Category!=Exhaustive&Category!=Heavy".
internal static class TestCategory
{
    public const string Name = "Category";

    // Large boards (13x13, 19x19) or long games: too slow for every mutant.
    public const string Heavy = "Heavy";

    // Full enumerations too large for the fast suite, such as all 4x4 colourings.
    public const string Exhaustive = "Exhaustive";

    // Timing thresholds: noisy under load, so run alone and never under Stryker.
    public const string Performance = "Performance";
}

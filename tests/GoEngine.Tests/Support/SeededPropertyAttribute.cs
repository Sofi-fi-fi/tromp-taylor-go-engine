using System.Globalization;
using FsCheck.Xunit;

namespace GoEngine.Tests.Support;

// An FsCheck property with a fixed seed, so every run (and every Stryker mutant) sees the same
// inputs and a failure reproduces; the number of cases comes from FSCHECK_MAX_TEST.
// Mechanism, checked for FsCheck.Xunit 3.6.0: attribute arguments must be constants and cannot
// read the environment, but PropertyAttribute is not sealed and the runner reads Replay and
// MaxTest from the attribute instance, so a subclass sets them in its constructor. Replay is
// "(seed,gamma)"; a named argument such as MaxTest = 20 still overrides the constructor.
internal sealed class SeededPropertyAttribute : PropertyAttribute
{
    public const ulong Seed = 20261127;

    // SplitMix gamma; must be odd.
    public const ulong Gamma = 1442695040888963407;

    public const int DefaultMaxTest = 100;

    public const string MaxTestVariable = "FSCHECK_MAX_TEST";

    public SeededPropertyAttribute()
    {
        Replay = string.Create(CultureInfo.InvariantCulture, $"({Seed},{Gamma})");
        MaxTest = MaxTestFromEnvironment();
    }

    public static int MaxTestFromEnvironment()
    {
        return ParseMaxTest(Environment.GetEnvironmentVariable(MaxTestVariable));
    }

    // Separate from the environment read, so tests can check it without changing process-wide
    // state. An unparsable value throws instead of falling back, so a typo cannot silently run
    // the default number of cases.
    public static int ParseMaxTest(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultMaxTest;
        }

        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int maxTest) && maxTest > 0)
        {
            return maxTest;
        }

        throw new InvalidOperationException($"{MaxTestVariable} must be a positive integer, got '{value}'.");
    }
}

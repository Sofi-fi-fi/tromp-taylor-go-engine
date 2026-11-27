using FsCheck;
using FsCheck.Fluent;
using Microsoft.FSharp.Core;
using Xunit;

namespace GoEngine.Tests.Support;

// Design §6: properties run with a fixed FsCheck seed.
public class SeededPropertyTests
{
    // The inputs the attribute's run may see: the same generator under the same seed.
    private static readonly Lazy<HashSet<string>> ExpectedInputs = new(() => [.. Inputs(SeededPropertyAttribute.Seed)]);

    [Fact]
    public void SameSeed_TwoRuns_SeeTheSameInputs()
    {
        Assert.Equal(Inputs(SeededPropertyAttribute.Seed), Inputs(SeededPropertyAttribute.Seed));
    }

    [Fact]
    public void SeededRun_MakesMaxTestCases()
    {
        Assert.Equal(SeededPropertyAttribute.MaxTestFromEnvironment(), Inputs(SeededPropertyAttribute.Seed).Count);
    }

    // Catches: a generator that ignores the seed, which would make SameSeed_TwoRuns_SeeTheSameInputs pass vacuously.
    [Fact]
    public void DifferentSeed_SeesDifferentInputs()
    {
        Assert.NotEqual(Inputs(SeededPropertyAttribute.Seed), Inputs(SeededPropertyAttribute.Seed + 1));
    }

    // Membership, not position: no per-run state, so a second run in the same process (Stryker, IDE)
    // sees the same result. Catches: an ignored Replay (FsCheck then picks a random seed).
    [SeededProperty]
    public void Attribute_RunsWithTheFixedSeed(int[] xs)
    {
        Assert.Contains(Format(xs), ExpectedInputs.Value);
    }

    [Theory]
    [InlineData(null, SeededPropertyAttribute.DefaultMaxTest)]
    [InlineData("", SeededPropertyAttribute.DefaultMaxTest)]
    [InlineData(" ", SeededPropertyAttribute.DefaultMaxTest)]
    [InlineData("1", 1)]
    [InlineData("250", 250)]
    public void ParseMaxTest_AcceptsUnsetOrPositive(string? value, int expected)
    {
        Assert.Equal(expected, SeededPropertyAttribute.ParseMaxTest(value));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    [InlineData("1e3")]
    [InlineData("99999999999")]
    public void ParseMaxTest_RejectsInvalid(string value)
    {
        Assert.Throws<InvalidOperationException>(() => SeededPropertyAttribute.ParseMaxTest(value));
    }

    [Fact]
    public void Attribute_TakesMaxTestFromEnvironment()
    {
        Assert.Equal(SeededPropertyAttribute.MaxTestFromEnvironment(), new SeededPropertyAttribute().MaxTest);
    }

    private static List<string> Inputs(ulong seed)
    {
        Config config = Config.QuickThrowOnFailure
            .WithReplay(FSharpOption<Replay>.Some(new Replay(new Rnd(seed, SeededPropertyAttribute.Gamma), FSharpOption<int>.None)))
            .WithMaxTest(SeededPropertyAttribute.MaxTestFromEnvironment())
            .WithQuietOnSuccess(true);

        List<string> inputs = [];
        Check.One(config, Prop.ForAll<int[]>(xs => inputs.Add(Format(xs))));
        return inputs;
    }

    private static string Format(int[] xs)
    {
        return string.Join(",", xs);
    }
}

using FsCheck;
using FsCheck.Fluent;
using Microsoft.FSharp.Core;
using Xunit;

namespace GoEngine.Tests.Support;

// Design §6: properties run with a fixed FsCheck seed.
public class SeededPropertyTests
{
    // The inputs the attribute's run must see: the same generator under the same seed.
    private static readonly Lazy<List<string>> ExpectedInputs = new(() => Inputs(SeededPropertyAttribute.Seed));

    private static int _attributeCalls;

    [Fact]
    public void SameSeed_TwoRuns_SeeTheSameInputs()
    {
        List<string> first = Inputs(SeededPropertyAttribute.Seed);
        List<string> second = Inputs(SeededPropertyAttribute.Seed);

        Assert.Equal(SeededPropertyAttribute.MaxTestFromEnvironment(), first.Count);
        Assert.Equal(first, second);
    }

    // Catches: a generator that ignores the seed, which would make the test above pass vacuously.
    [Fact]
    public void DifferentSeed_SeesDifferentInputs()
    {
        Assert.NotEqual(Inputs(SeededPropertyAttribute.Seed), Inputs(SeededPropertyAttribute.Seed + 1));
    }

    // Catches: an ignored Replay (FsCheck then picks a random seed).
    [SeededProperty]
    public void Attribute_RunsWithTheFixedSeed(int[] xs)
    {
        int call = Interlocked.Increment(ref _attributeCalls) - 1;

        Assert.True(call < ExpectedInputs.Value.Count, $"Property ran more than {ExpectedInputs.Value.Count} times.");
        Assert.Equal(ExpectedInputs.Value[call], Format(xs));
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

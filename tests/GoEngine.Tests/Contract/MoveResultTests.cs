using GoEngine.Core;
using Xunit;

namespace GoEngine.Tests.Contract;

public class MoveResultTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Success_NonNegativeCaptured_IsOkWithGivenCaptured(int captured)
    {
        MoveResult result = MoveResult.Success(captured);

        Assert.Equal(MoveStatus.Ok, result.Status);
        Assert.Equal(captured, result.Captured);
        Assert.Equal(0, result.SelfCaptured);
        Assert.True(result.IsOk);
    }

    [Fact]
    public void Success_NegativeCaptured_Throws()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => MoveResult.Success(-1));

        Assert.Equal("captured", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void SelfCapture_PositiveSelfCaptured_IsOkWithNoCaptures(int selfCaptured)
    {
        MoveResult result = MoveResult.SelfCapture(selfCaptured);

        Assert.Equal(MoveStatus.Ok, result.Status);
        Assert.Equal(0, result.Captured);
        Assert.Equal(selfCaptured, result.SelfCaptured);
        Assert.True(result.IsOk);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SelfCapture_SelfCapturedBelowOne_Throws(int selfCaptured)
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => MoveResult.SelfCapture(selfCaptured));

        Assert.Equal("selfCaptured", ex.ParamName);
    }

    [Theory]
    [InlineData(MoveStatus.GameOver)]
    [InlineData(MoveStatus.OutOfBounds)]
    [InlineData(MoveStatus.Occupied)]
    [InlineData(MoveStatus.Suicide)]
    [InlineData(MoveStatus.Superko)]
    public void Rejected_RejectionStatus_KeepsStatusWithNoCaptures(MoveStatus status)
    {
        MoveResult result = MoveResult.Rejected(status);

        Assert.Equal(status, result.Status);
        Assert.Equal(0, result.Captured);
        Assert.Equal(0, result.SelfCaptured);
        Assert.False(result.IsOk);
    }

    [Theory]
    [InlineData((int)MoveStatus.Ok)]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Rejected_OkOrUndefinedStatus_Throws(int value)
    {
        MoveStatus status = (MoveStatus)value;

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => MoveResult.Rejected(status));

        Assert.Equal("status", ex.ParamName);
    }

    [Fact]
    public void Default_IsNotOkAndDiffersFromSuccess()
    {
        MoveResult result = default;

        Assert.False(result.IsOk);
        Assert.NotEqual(MoveResult.Success(0), result);
    }

    [Fact]
    public void Success_SameCaptured_AreEqualWithSameHash()
    {
        MoveResult a = MoveResult.Success(3);
        MoveResult b = MoveResult.Success(3);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}

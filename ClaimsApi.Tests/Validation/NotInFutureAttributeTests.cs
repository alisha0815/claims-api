using ClaimsApi.Validation;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Validation;

public class NotInFutureAttributeTests()
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly NotInFutureAttribute _attribute = new()
    {
        TimeProvider = new FakeTimeProvider(Now),
    };

    private readonly DateOnly _today = DateOnly.FromDateTime(Now.UtcDateTime);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Tomorrow_IsInvalid(int daysAhead)
    {
        var future = _today.AddDays(daysAhead);

        var result = _attribute.IsValid(future);

        Assert.False(result);
    }

    [Fact]
    public void Yesterday_IsValid()
    {
        var yesterday = _today.AddDays(-1);

        var result = _attribute.IsValid(yesterday);

        Assert.True(result);
    }

    [Fact]
    public void Today_IsValid()
    {
        var result = _attribute.IsValid(_today);

        Assert.True(result);
    }

    [Fact]
    public void Null_IsValid()
    {
        Assert.True(_attribute.IsValid(null));
    }

    [Fact]
    public void NonDateValue_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => _attribute.IsValid("hello"));
    }
}

using ClaimsApi.Validation;
using Xunit.Abstractions;

namespace ClaimsApi.Tests.Validation;

public class NotInFutureAttributeTests(ITestOutputHelper output)
{
    private readonly NotInFutureAttribute _attribute = new();
    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Tomorrow_IsInvalid()
    {
        var tomorrow = _today.AddDays(1);

        var result = _attribute.IsValid(tomorrow);

        output.WriteLine($"tomorrow: {tomorrow}, result: {result}");

        Assert.False(result);
    }

    [Fact]
    public void Yesterday_IsValid()
    {
        var yesterday = _today.AddDays(-1);

        var result = _attribute.IsValid(yesterday);

        output.WriteLine($"yesterday: {yesterday}, result: {result}");

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

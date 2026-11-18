using ClaimsApi.Auth;

namespace ClaimsApi.Tests.Auth;

public class UsernameNormalizerTests
{
    [Theory]
    [InlineData(" Alice", "alice")]
    [InlineData("ALICE", "alice")]
    public void Normalize_ReturnsTrimmedLowercaseUsername(string input, string expected)
    {
        // Act
        var result = UsernameNormalizer.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}

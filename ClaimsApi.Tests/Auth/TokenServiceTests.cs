using System.IdentityModel.Tokens.Jwt;
using ClaimsApi.Auth;
using ClaimsApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace ClaimsApi.Tests.Auth;

public class TokenServiceTests(ITestOutputHelper output)
{
    [Fact]
    public void CreateToken_SetsExpiresAt_ToNowPlusExpiresMinutes()
    {
        // Arrange
        var fixedNow = new DateTimeOffset(2026, 11, 1, 9, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedNow);

        var options = Options.Create(
            new JwtOptions
            {
                Issuer = "test-issuer",
                Audience = "test-audience",
                Key = "test-signing-key-for-unit-tests-only-1234567890",
                ExpiresMinutes = 60,
            }
        );

        var user = new User
        {
            Id = 3,
            Username = "test-user",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = fixedNow,
        };

        // Act
        var result = new TokenService(options, timeProvider).CreateToken(user);

        output.WriteLine($"Token     = {result.Token}");
        output.WriteLine($"ExpiresAt = {result.ExpiresAt}");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        output.WriteLine($"Header    = {jwt.Header.SerializeToJson()}");
        output.WriteLine($"Payload   = {jwt.Payload.SerializeToJson()}");

        // Assert
        Assert.Equal(fixedNow.AddMinutes(60), result.ExpiresAt);
    }
}

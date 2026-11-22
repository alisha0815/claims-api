using System.IdentityModel.Tokens.Jwt;
using ClaimsApi.Auth;
using ClaimsApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit.Abstractions;

namespace ClaimsApi.Tests.Auth;

public class TokenServiceTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 1, 9, 30, 0, TimeSpan.Zero);

    private const string PasswordHash = "not-a-real-hash";

    private const int RefreshTokenDays = 7;
    private const int ExpiresMinutes = 60;

    private static TokenService CreateSut() =>
        new(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    Key = "test-signing-key-for-unit-tests-only-1234567890",
                    ExpiresMinutes = ExpiresMinutes,
                    RefreshTokenDays = RefreshTokenDays,
                }
            ),
            new FakeTimeProvider(FixedNow)
        );

    private static User CreateUser() =>
        new()
        {
            Id = 3,
            Username = "test-user",
            PasswordHash = PasswordHash,
            Role = Roles.Customer,
            CreatedAt = FixedNow,
        };

    [Fact]
    public void CreateToken_SetsExpiresAt_ToNowPlusExpiresMinutes()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(FixedNow);

        var user = CreateUser();

        // Act
        var result = CreateSut().CreateToken(CreateUser());

        output.WriteLine($"Token     = {result.Token}");
        output.WriteLine($"ExpiresAt = {result.ExpiresAt}");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        output.WriteLine($"Header    = {jwt.Header.SerializeToJson()}");
        output.WriteLine($"Payload   = {jwt.Payload.SerializeToJson()}");

        // Assert
        Assert.Equal(FixedNow.AddMinutes(60), result.ExpiresAt);
    }

    [Fact]
    public async Task CreateToken_PutsUserAndSettingsIntoToken()
    {
        // Act
        var result = CreateSut().CreateToken(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        // Assert
        Assert.Equal("3", jwt.Subject);
        Assert.Equal(Roles.Customer, jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("test-user", jwt.Claims.Single(c => c.Type == "unique_name").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
        Assert.Equal(FixedNow.UtcDateTime, jwt.ValidFrom);
        Assert.Equal(FixedNow.AddMinutes(60).UtcDateTime, jwt.ValidTo);
    }

    [Fact]
    public void CreateToken_DoesNotPutPasswordHashInPayload()
    {
        // Arrange
        var user = CreateUser();

        // Act
        var result = CreateSut().CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        var payloadJson = jwt.Payload.SerializeToJson();

        // Assert
        Assert.DoesNotContain(PasswordHash, payloadJson);
    }

    [Fact]
    public void CreateRefreshToken_SetsExpiresAt_ToNowPlusRefreshTokenDays()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var refreshToken = sut.CreateRefreshToken();

        // Assert
        Assert.Equal(FixedNow.AddDays(RefreshTokenDays), refreshToken.ExpiresAt);
    }
}

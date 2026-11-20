using System.IdentityModel.Tokens.Jwt;
using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Models;
using ClaimsApi.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Asn1.Cms;

namespace ClaimsApi.Tests.Auth;

public class AuthServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string Password = "password";
    private readonly DateTimeOffset _fixedTime = new(2026, 11, 1, 10, 30, 0, TimeSpan.Zero);

    private readonly PasswordHasher<User> _hasher = new();

    private static RegisterRequest ValidRegisterRequest(string username) =>
        new(Username: username, Password: Password);

    private AuthService CreateAuthService(AppDbContext db, FakeTimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? new FakeTimeProvider(_fixedTime);

        var tokenService = new TokenService(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "test-issuer",
                    Audience = "test-audience",
                    Key = "test-signing-key-for-unit-tests-only-1234567890",
                    ExpiresMinutes = 60,
                }
            ),
            clock
        );

        return new AuthService(db, _hasher, clock, tokenService);
    }

    // RegisterAsync - success
    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesCustomer()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var service = CreateAuthService(arrangeDb);

        // Act
        var result = await service.RegisterAsync(ValidRegisterRequest(" Alice "));

        // Assert
        Assert.Equal(RegisterStatus.Created, result.Status);
        Assert.NotNull(result.Response);

        await using var assertDb = fixture.CreateDbContext();

        var saved = await assertDb.Users.SingleAsync(u => u.Id == result.Response.Id);

        Assert.NotEmpty(saved.PasswordHash);
        Assert.NotEqual(Password, saved.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _hasher.VerifyHashedPassword(saved, saved.PasswordHash, Password)
        );

        Assert.Equal("alice", saved.Username);
        Assert.Equal(Roles.Customer, saved.Role);
        Assert.Equal(_fixedTime, saved.CreatedAt);

        // Ids start at 100 to stay clear of the seeded customer (Id 1).
        Assert.InRange(saved.Id, 100, int.MaxValue);

        Assert.Equal(
            new RegisterResponse(saved.Id, "alice", Roles.Customer, _fixedTime),
            result.Response
        );
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ReturnsUsernameTaken()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        // Register first time
        await arrangeService.RegisterAsync(ValidRegisterRequest(" Erin "));

        // Act
        // Register again with the same name in different casing
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        var result = await actService.RegisterAsync(ValidRegisterRequest(" ERIN "));

        // Assert
        await using var assertDb = fixture.CreateDbContext();

        var count = await assertDb.Users.CountAsync(u => u.Username == "erin");

        Assert.Equal(RegisterStatus.UsernameTaken, result.Status);
        Assert.Null(result.Response);
        Assert.Equal(1, count);
    }

    // Login -success
    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenPasswordIsCorrect()
    {
        // Arrange
        // register "bob"
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        var registerResult = await arrangeService.RegisterAsync(ValidRegisterRequest("bob"));

        var userId = registerResult.Response!.Id;

        // Act
        await using var actDb = fixture.CreateDbContext();

        var actService = CreateAuthService(actDb);

        // Login
        var result = await actService.LoginAsync(
            new LoginRequest(" BOB ", Password),
            CancellationToken.None
        );

        Assert.Equal(LoginStatus.Success, result.Status);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Response!.AccessToken);

        // Assert
        Assert.Equal(userId.ToString(), jwt.Subject);
    }
}

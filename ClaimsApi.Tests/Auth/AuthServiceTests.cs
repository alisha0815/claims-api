using System.IdentityModel.Tokens.Jwt;
using ClaimsApi.Auth;
using ClaimsApi.Auth.Tokens;
using ClaimsApi.Data;
using ClaimsApi.Dtos.Auth;
using ClaimsApi.Models;
using ClaimsApi.Services.Admin;
using ClaimsApi.Services.Auth;
using ClaimsApi.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Auth;

public class AuthServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string Password = "password";

    private const int RefreshTokenDays = 7;
    private const int ExpiresMinutes = 60;

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
                    ExpiresMinutes = ExpiresMinutes,
                    RefreshTokenDays = RefreshTokenDays,
                }
            ),
            clock
        );

        return new AuthService(db, _hasher, clock, tokenService);
    }

    private AccountAdminService CreateAccountAdminService(AppDbContext db) => new(db);

    private static readonly string PasswordHashValue = new PasswordHasher<User>().HashPassword(
        null!,
        Password
    );

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

    // Login - invalid credentials
    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenUserDoesNotExist()
    {
        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        // Login with "nobody"
        var result = await actService.LoginAsync(
            new LoginRequest("nobody", Password),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task LoginAsync_ReturnsInvalidCredentials_WhenPasswordIsWrong()
    {
        // Arrange
        // register "bob"
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        await arrangeService.RegisterAsync(ValidRegisterRequest("bob"));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        // Login with a wrong password
        var result = await actService.LoginAsync(
            new LoginRequest("bob", "wrong-password"),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task LoginAsync_StoresRefreshTokenHash_WhenPasswordIsCorrect()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        var registered = await arrangeService.RegisterAsync(
            ValidRegisterRequest("carol"),
            CancellationToken.None
        );
        var userId = registered.Response!.Id;

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        // Login with a correct password
        var result = await actService.LoginAsync(
            new LoginRequest("carol", Password),
            CancellationToken.None
        );

        // Assert
        await using var assertDb = fixture.CreateDbContext();

        var saved = await assertDb.RefreshTokens.SingleAsync(
            t => t.UserId == userId,
            CancellationToken.None
        );

        var refreshToken = result.Response!.RefreshToken;

        Assert.NotEqual(refreshToken, saved.TokenHash);
        Assert.Equal(RefreshTokenHasher.Hash(refreshToken), saved.TokenHash);
        Assert.Equal(_fixedTime.AddDays(RefreshTokenDays), saved.ExpiresAt);
        Assert.Null(saved.RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        // register
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        await arrangeService.RegisterAsync(ValidRegisterRequest("miranda"), CancellationToken.None);

        // login
        var login = await arrangeService.LoginAsync(
            new LoginRequest("miranda", Password),
            CancellationToken.None
        );

        var oldRefreshToken = login.Response!.RefreshToken;

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        var result = await actService.RefreshAsync(
            new RefreshRequest(oldRefreshToken),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(RefreshStatus.Success, result.Status);

        var response = result.Response!;
        Assert.NotEqual(oldRefreshToken, response.RefreshToken);
        Assert.False(string.IsNullOrEmpty(response.AccessToken));
    }

    [Fact]
    public async Task RefreshAsync_ReturnsInvalid_WhenRefreshTokenIsExpired()
    {
        // Arrange
        // register
        await using var arrangeDb = fixture.CreateDbContext();

        // Move past the 7-day refresh token lifetime
        var clock = new FakeTimeProvider(_fixedTime);
        var arrangeService = CreateAuthService(arrangeDb, clock);

        await arrangeService.RegisterAsync(ValidRegisterRequest("nina"), CancellationToken.None);

        // login
        var login = await arrangeService.LoginAsync(
            new LoginRequest("nina", Password),
            CancellationToken.None
        );

        var oldRefreshToken = login.Response!.RefreshToken;

        clock.Advance(TimeSpan.FromDays(8));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb, clock);

        var result = await actService.RefreshAsync(
            new RefreshRequest(oldRefreshToken),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(RefreshStatus.Invalid, result.Status);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsInvalid_WhenRefreshTokenIsReused()
    {
        // Arrange
        // register
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        await arrangeService.RegisterAsync(ValidRegisterRequest("jan"), CancellationToken.None);

        // login
        var login = await arrangeService.LoginAsync(
            new LoginRequest("jan", Password),
            CancellationToken.None
        );

        var oldRefreshToken = login.Response!.RefreshToken;

        // First use: rotates and revokes oldRefreshToken
        await arrangeService.RefreshAsync(
            new RefreshRequest(oldRefreshToken),
            CancellationToken.None
        );

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        // reuse the old refreshToken
        var result = await actService.RefreshAsync(
            new RefreshRequest(oldRefreshToken),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(RefreshStatus.Invalid, result.Status);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken_WhenTokenIsValid()
    {
        // Arrange
        // register
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        await arrangeService.RegisterAsync(ValidRegisterRequest("tom"), CancellationToken.None);

        // login
        var login = await arrangeService.LoginAsync(
            new LoginRequest("tom", Password),
            CancellationToken.None
        );

        var oldRefreshToken = login.Response!.RefreshToken;

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        // logout
        await actService.LogoutAsync(new RefreshRequest(oldRefreshToken), CancellationToken.None);

        // Assert
        await using var assertDb = fixture.CreateDbContext();

        var result = await assertDb.RefreshTokens.SingleAsync(
            r => r.TokenHash == RefreshTokenHasher.Hash(oldRefreshToken),
            CancellationToken.None
        );

        Assert.Equal(_fixedTime, result.RevokedAt);
    }

    // Create handler
    [Fact]
    public async Task CreateHandlerAsync_WithValidRequest_CreatesHandler()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var service = CreateAuthService(arrangeDb);

        // Act
        var result = await service.CreateHandlerAsync(ValidRegisterRequest(" Handler "));

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

        Assert.Equal("handler", saved.Username);
        Assert.Equal(Roles.Handler, saved.Role);
        Assert.Equal(_fixedTime, saved.CreatedAt);

        // Ids start at 100 to stay clear of the seeded customer (Id 1).
        Assert.InRange(saved.Id, 100, int.MaxValue);

        Assert.Equal(
            new RegisterResponse(saved.Id, "handler", Roles.Handler, _fixedTime),
            result.Response
        );
    }

    [Fact]
    public async Task CreateHandlerAsync_ExistingUsername_ReturnsUsernameTaken()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateAuthService(arrangeDb);

        // The handler already exists
        var guid = Guid.NewGuid();
        await arrangeService.CreateHandlerAsync(ValidRegisterRequest($" Handler-{guid} "));

        // Act
        // Create handler with the name already taken.
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAuthService(actDb);

        var result = await actService.CreateHandlerAsync(ValidRegisterRequest($"HANDLER-{guid}"));

        // Assert
        Assert.Equal(RegisterStatus.UsernameTaken, result.Status);
        Assert.Null(result.Response);

        await using var assertDb = fixture.CreateDbContext();

        var count = await assertDb.Users.CountAsync(u => u.Username == $"handler-{guid}");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetAllHandlers_ReturnsOnlyHandlers()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        var guid = Guid.NewGuid();

        var customerName = $"test-customer-{guid}";
        var adminName = $"test-admin-{guid}";
        var handlerOneName = $"handler-one-{guid}";
        var handlerTwoName = $"handler-two-{guid}";

        // Create customer, admin, handler
        var users = new List<User>
        {
            new()
            {
                Username = customerName,
                PasswordHash = PasswordHashValue,
                Role = Roles.Customer,
                CreatedAt = _fixedTime,
            },
            new()
            {
                Username = adminName,
                PasswordHash = PasswordHashValue,
                Role = Roles.Admin,
                CreatedAt = _fixedTime,
            },
            new()
            {
                Username = handlerOneName,
                PasswordHash = PasswordHashValue,
                Role = Roles.Handler,
                CreatedAt = _fixedTime,
            },
            new()
            {
                Username = handlerTwoName,
                PasswordHash = PasswordHashValue,
                Role = Roles.Handler,
                CreatedAt = _fixedTime,
            },
        };

        // Store
        arrangeDb.Users.AddRange(users);

        // Save it to DB
        await arrangeDb.SaveChangesAsync();

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateAccountAdminService(actDb);

        var result = await actService.GetAllHandlersAsync();

        // Assert
        var usernames = result.Select(u => u.Username).ToList();

        Assert.Contains(handlerOneName, usernames);
        Assert.Contains(handlerTwoName, usernames);
        Assert.DoesNotContain(customerName, usernames);
        Assert.DoesNotContain(adminName, usernames);
    }
}

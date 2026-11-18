using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Models;
using ClaimsApi.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Auth;

public class AuthServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string Password = "alice-password";
    private readonly DateTimeOffset _fixedTime = new(2026, 11, 1, 10, 30, 0, TimeSpan.Zero);

    private readonly PasswordHasher<User> _hasher = new();

    private static RegisterRequest ValidRegisterRequest(string username) =>
        new(Username: username, Password: Password);

    private AuthService CreateAuthService(AppDbContext db, FakeTimeProvider? timeProvider = null) =>
        new(db, _hasher, timeProvider ?? new FakeTimeProvider(_fixedTime));

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
}

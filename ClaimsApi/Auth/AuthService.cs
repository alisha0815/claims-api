using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Mappings;
using ClaimsApi.Models;
using Microsoft.AspNetCore.Identity;

namespace ClaimsApi.Auth;

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, TimeProvider timeProvider)
    : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var now = timeProvider.GetUtcNow();

        var normalizedUsername = UsernameNormalizer.Normalize(request.Username);

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = string.Empty,
            Role = Roles.Customer,
            CreatedAt = now,
        };

        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.ToRegisterResponse();
    }
}

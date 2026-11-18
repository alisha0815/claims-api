using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Mappings;
using ClaimsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Auth;

public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, TimeProvider timeProvider)
    : IAuthService
{
    public async Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedUsername = UsernameNormalizer.Normalize(request.Username);

        var usernameTaken = await db.Users.AnyAsync(
            u => u.Username == normalizedUsername,
            cancellationToken
        );

        if (usernameTaken)
        {
            return new RegisterResult(RegisterStatus.UsernameTaken, null);
        }

        var now = timeProvider.GetUtcNow();

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

        return new RegisterResult(RegisterStatus.Created, user.ToRegisterResponse());
    }
}

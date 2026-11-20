using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Mappings;
using ClaimsApi.Models;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Auth;

public class AuthService(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    TimeProvider timeProvider,
    ITokenService tokenService
) : IAuthService
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

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        // Race condition: two requests with the same username can both pass the AnyAsync check
        // The unique index rejects the second INSERT and return UsernameTaken (409)
        catch (UniqueConstraintException ex)
            when (ex.ConstraintProperties.Contains(nameof(User.Username)))
        {
            return new RegisterResult(RegisterStatus.UsernameTaken, null);
        }

        return new RegisterResult(RegisterStatus.Created, user.ToRegisterResponse());
    }

    public async Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedUsername = UsernameNormalizer.Normalize(request.Username);

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Username == normalizedUsername,
            cancellationToken
        );

        if (user is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials, null);
        }

        var verifiedResult = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verifiedResult == PasswordVerificationResult.Failed)
        {
            return new LoginResult(LoginStatus.InvalidCredentials, null);
        }

        var jwt = tokenService.CreateToken(user);

        return new LoginResult(LoginStatus.Success, new LoginResponse(jwt.Token, jwt.ExpiresAt));
    }
}

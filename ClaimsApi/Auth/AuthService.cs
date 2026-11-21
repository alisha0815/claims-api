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
    private string IssueRefreshToken(int userId)
    {
        var issued = tokenService.CreateRefreshToken();

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = RefreshTokenHasher.Hash(issued.Token),
            ExpiresAt = issued.ExpiresAt,
        };

        db.RefreshTokens.Add(entity);

        return issued.Token;
    }

    private async Task<RefreshToken?> FindRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        var hashedRefreshToken = RefreshTokenHasher.Hash(token);

        return await db.RefreshTokens.FirstOrDefaultAsync(
            r => r.TokenHash == hashedRefreshToken,
            cancellationToken
        );
    }

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

        var issuedRefreshToken = IssueRefreshToken(user.Id);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResult(
            LoginStatus.Success,
            new LoginResponse(jwt.Token, jwt.ExpiresAt, issuedRefreshToken)
        );
    }

    public async Task<RefreshResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var found = await FindRefreshTokenAsync(request.RefreshToken, cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (found is null || found.ExpiresAt <= now || found.RevokedAt is not null)
        {
            return new RefreshResult(RefreshStatus.Invalid, null);
        }

        // Rotation: a refresh token can be used only once.
        found.RevokedAt = now;

        var user = await db.Users.FirstAsync(u => u.Id == found.UserId, cancellationToken);

        var newRefreshToken = IssueRefreshToken(user.Id);

        // create a new AT
        var jwt = tokenService.CreateToken(user);

        // save
        await db.SaveChangesAsync(cancellationToken);

        // return success with LoginResponse
        return new RefreshResult(
            RefreshStatus.Success,
            new LoginResponse(jwt.Token, jwt.ExpiresAt, newRefreshToken)
        );
    }

    public async Task LogoutAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var found = await FindRefreshTokenAsync(request.RefreshToken, cancellationToken);

        var now = timeProvider.GetUtcNow();

        // Nothing to revoke: still a successful logout (always 204)
        if (found is null || found.ExpiresAt <= now || found.RevokedAt is not null)
        {
            return;
        }

        // Logout: this refresh token can no longer be used.
        found.RevokedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }
}

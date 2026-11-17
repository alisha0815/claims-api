using ClaimsApi.Data;
using ClaimsApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Auth;

public static class AdminBootstrapper
{
    public static async Task EnsureAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AdminBootstrapper");

        var username = config["Admin:Username"];
        var password = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin not configured; skipping bootstrap.");
            return;
        }

        username = username.Trim().ToLowerInvariant();

        // Creates the admin only once. If an admin already exists, we stop here,
        // so changing Admin:Username or Admin:Password later has no effect.
        if (await db.Users.AnyAsync(u => u.Role == Roles.Admin))
        {
            logger.LogInformation("Admin already exists; skipping bootsrap.");
            return;
        }
    }
}

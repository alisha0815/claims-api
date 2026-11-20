using ClaimsApi.Auth;
using ClaimsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Claim>()
            .Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Claim>().HasIndex(c => c.CustomerId);

        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<User>().Property(u => u.Username).HasMaxLength(50);

        modelBuilder.Entity<User>().Property(u => u.Role).HasMaxLength(20);

        modelBuilder
            .Entity<User>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Users_Role",
                    $"\"Role\" IN ('{Roles.Customer}', '{Roles.Handler}', '{Roles.Admin}')"
                )
            );

        // Seeded users use fixed Ids (customer = 1). Start auto-generated Ids at 100 to avoid collisions with seed data.
        modelBuilder.Entity<User>().Property(u => u.Id).HasIdentityOptions(startValue: 100);

        modelBuilder
            .Entity<User>()
            .HasData(
                new User
                {
                    Id = 1,
                    Username = "customer",
                    // Fixed hash of the demo password. Generated once with PasswordHasher
                    PasswordHash =
                        "AQAAAAIAAYagAAAAEFGwj4qQW2zVnoy9l/K/JnjmIPCyZtp62KwxW21UmV/iMCVqTQrmxYcrzYf1HgnrWQ==",
                    Role = Roles.Customer,
                    CreatedAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
                }
            );

        modelBuilder
            .Entity<Claim>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<RefreshToken>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>().HasIndex(r => r.TokenHash).IsUnique();
    }
}

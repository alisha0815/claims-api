using ClaimsApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Claim>()
            .Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Claim>().HasIndex(c => c.CustomerId);
    }
}

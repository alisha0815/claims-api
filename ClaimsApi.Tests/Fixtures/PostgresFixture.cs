using ClaimsApi.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ClaimsApi.Tests.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(
        "postgres:17-alpine"
    ).Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Models;
using ClaimsApi.Services;
using ClaimsApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Services;

public class ClaimServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly DateTimeOffset _fixedTime = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private ClaimService CreateService(AppDbContext db) =>
        new(db, new FakeTimeProvider(_fixedTime));

    private static CreateClaimRequest ValidRequest() =>
        new(
            PolicyNumber: "POL-2026-00123",
            Title: "Water damage in kitchen",
            Description: "A pipe under the sink burst overnight and damaged the floor and cabinets.",
            Amount: 15000.50m,
            IncidentDate: new DateOnly(2026, 11, 3)
        );

    // CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsStatusToReceived()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);

        // Act
        var response = await service.CreateAsync(42, ValidRequest());

        // Assert
        Assert.Equal(ClaimStatus.Received, response.Status);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsTimestampsToCurrentTime()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);

        // Act
        var response = await service.CreateAsync(42, ValidRequest());

        // Assert
        Assert.Equal(_fixedTime.UtcDateTime, response.CreatedAt);
        Assert.Equal(_fixedTime.UtcDateTime, response.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_UsesGivenCustomerId()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);
        const int customerId = 42;

        // Act
        var response = await service.CreateAsync(customerId, ValidRequest());

        // Assert
        Assert.Equal(customerId, response.CustomerId);
    }

    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsClaim()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateService(arrangeDb);

        var created = await arrangeService.CreateAsync(42, ValidRequest());

        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        // Act
        var result = await actService.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created, result);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);
        const int nonExistentId = int.MaxValue;

        // Act
        var result = await service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_NoClaims_ReturnsEmptyList()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        await db.Claims.ExecuteDeleteAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }
}

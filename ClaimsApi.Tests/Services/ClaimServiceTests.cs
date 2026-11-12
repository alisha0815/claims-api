using ClaimsApi.Data;
using ClaimsApi.Dtos;
using ClaimsApi.Models;
using ClaimsApi.Services;
using ClaimsApi.Tests.Fixtures;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Services;

public class ClaimServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly DateTimeOffset _fixedTime = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private ClaimService CreateService(AppDbContext db) =>
        new(db, new FakeTimeProvider(_fixedTime));

    private static CreateClaimRequest ValidRequest() =>
        new CreateClaimRequest(
            PolicyNumber: "POL-2026-00123",
            Title: "Water damage in kitchen",
            Description: "A pipe under the sink burst overnight and damaged the floor and cabinets.",
            Amount: 15000.50m,
            IncidentDate: new DateOnly(2026, 11, 3)
        );

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
}

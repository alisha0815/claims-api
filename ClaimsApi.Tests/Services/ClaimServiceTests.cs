using ClaimsApi.Auth;
using ClaimsApi.Data;
using ClaimsApi.Dtos.Claims;
using ClaimsApi.Models;
using ClaimsApi.Services;
using ClaimsApi.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ClaimsApi.Tests.Services;

public class ClaimServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly DateTimeOffset _fixedTime = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private ClaimService CreateService(AppDbContext db, FakeTimeProvider? timeProvider = null) =>
        new(db, timeProvider ?? new FakeTimeProvider(_fixedTime));

    private static CreateClaimRequest ValidRequest() =>
        new(
            PolicyNumber: "POL-2026-00123",
            Title: "Water damage in kitchen",
            Description: "A pipe under the sink burst overnight and damaged the floor and cabinets.",
            Amount: 15000.50m,
            IncidentDate: new DateOnly(2026, 11, 3)
        );

    private static UpdateClaimRequest ValidUpdateRequest() =>
        new(
            Title: "Water damage in kitchen and hallway",
            IncidentDate: new DateOnly(2026, 11, 2),
            Amount: 18000.75m,
            Description: "The leak also spread to the hallway floor."
        );

    private static UpdateClaimStatusRequest StatusRequest(ClaimStatus status) => new(status);

    // Seeded demo customer. Claims.CustomerId has a FK to Users.Id, so tests must use an existing customer Id.
    private const int SeededCustomerId = 1;

    // CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsStatusToReceived()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);

        // Act
        var response = await service.CreateAsync(SeededCustomerId, ValidRequest());

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
        var response = await service.CreateAsync(SeededCustomerId, ValidRequest());

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

        // Act
        var response = await service.CreateAsync(SeededCustomerId, ValidRequest());

        // Assert
        Assert.Equal(SeededCustomerId, response.CustomerId);
    }

    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsClaim()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateService(arrangeDb);

        var created = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        // Act
        var result = await actService.GetByIdAsync(created.Id, SeededCustomerId);

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
        var result = await service.GetByIdAsync(nonExistentId, SeededCustomerId);

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
        var result = await service.GetAllAsync(SeededCustomerId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleClaims_ReturnsNewestFirst()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        await arrangeDb.Claims.ExecuteDeleteAsync(); // Empty table

        // Created at 10:30
        var olderService = CreateService(arrangeDb);
        var older = await olderService.CreateAsync(SeededCustomerId, ValidRequest());

        // Created at 10:31
        var timeProvider = new FakeTimeProvider(_fixedTime);
        timeProvider.Advance(TimeSpan.FromMinutes(1));

        var newerService = CreateService(arrangeDb, timeProvider);
        var newer = await newerService.CreateAsync(SeededCustomerId, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);
        var result = await actService.GetAllAsync(SeededCustomerId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
    }

    [Fact]
    public async Task GetAllAsync_StatusFilter_ReturnsOnlyMatchingClaims()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        await arrangeDb.Claims.ExecuteDeleteAsync(); // Empty table

        // Create two claims (both Received)
        var arrangeService = CreateService(arrangeDb);
        var received = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());
        var toApprove = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        // Approve one of them
        await arrangeDb
            .Claims.Where(c => c.Id == toApprove.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ClaimStatus.Approved));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);
        var result = await actService.GetAllAsync(SeededCustomerId, status: ClaimStatus.Approved);

        // Assert
        var claim = Assert.Single(result);
        Assert.Equal(toApprove.Id, claim.Id);
    }

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsClaimsOfAllStatuses()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        await arrangeDb.Claims.ExecuteDeleteAsync(); // Empty table

        // Create two claims (both Received)
        var arrangeService = CreateService(arrangeDb);
        var received = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());
        var toApprove = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        // Approve one of them
        await arrangeDb
            .Claims.Where(c => c.Id == toApprove.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ClaimStatus.Approved));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);
        var result = await actService.GetAllAsync(SeededCustomerId, status: null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == received.Id);
        Assert.Contains(result, c => c.Id == toApprove.Id);
    }

    [Fact]
    public async Task UpdateAsync_ReceivedClaim_UpdatesFields()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create one claim (Received)
        var arrangeService = CreateService(arrangeDb);
        var received = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var id = received.Id;
        var request = ValidUpdateRequest();

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.UpdateAsync(id, SeededCustomerId, request);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var updated = await CreateService(assertDb).GetByIdAsync(id, SeededCustomerId);

        Assert.Equal(UpdateClaimResult.Updated, result);
        Assert.NotNull(updated);
        Assert.Equal(request.Title, updated.Title);
        Assert.Equal(request.IncidentDate!.Value, updated.IncidentDate);
        Assert.Equal(request.Amount, updated.Amount);
        Assert.Equal(request.Description, updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_InReviewClaim_ReturnsNotEditable()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create one claim (Received)
        var arrangeService = CreateService(arrangeDb);
        var created = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var id = created.Id;
        var request = ValidUpdateRequest();

        // Move to InReview
        await arrangeDb
            .Claims.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ClaimStatus.InReview));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.UpdateAsync(id, SeededCustomerId, request);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var stored = await CreateService(assertDb).GetByIdAsync(id, SeededCustomerId);

        Assert.Equal(UpdateClaimResult.NotEditable, result);
        Assert.NotNull(stored);
        Assert.Equal(created.Title, stored.Title);
        Assert.Equal(ClaimStatus.InReview, stored.Status);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);
        const int nonExistentId = int.MaxValue;

        var request = ValidUpdateRequest();

        // Act
        var result = await service.UpdateAsync(nonExistentId, SeededCustomerId, request);

        // Assert
        Assert.Equal(UpdateClaimResult.NotFound, result);
    }

    [Fact]
    public async Task DeleteAsync_ReceivedClaim_DeletesClaim()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        var arrangeService = CreateService(arrangeDb);

        var created = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var idToDelete = created.Id;

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.DeleteAsync(idToDelete, SeededCustomerId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(idToDelete, SeededCustomerId);

        Assert.Equal(DeleteClaimResult.Deleted, result);
        Assert.Null(stored);
    }

    [Fact]
    public async Task DeleteAsync_InReviewClaim_ReturnsNotDeletable()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create one claim (Received)
        var arrangeService = CreateService(arrangeDb);
        var created = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var id = created.Id;

        // Move to InReview
        await arrangeDb
            .Claims.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, ClaimStatus.InReview));

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.DeleteAsync(id, SeededCustomerId);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(id, SeededCustomerId);

        Assert.Equal(DeleteClaimResult.NotDeletable, result);
        Assert.NotNull(stored);
        Assert.Equal(ClaimStatus.InReview, stored.Status);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);
        const int nonExistentId = int.MaxValue;

        // Act
        var result = await service.DeleteAsync(nonExistentId, SeededCustomerId);

        // Assert

        Assert.Equal(DeleteClaimResult.NotFound, result);
    }

    [Fact]
    public async Task UpdateStatusAsync_AllowedTransition_UpdatesStatus()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create one claim (Received)
        var arrangeService = CreateService(arrangeDb);
        var received = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var id = received.Id;
        var request = StatusRequest(ClaimStatus.InReview);

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.UpdateStatusAsync(id, request);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(id, SeededCustomerId);

        Assert.Equal(UpdateClaimStatusResult.Updated, result);
        Assert.NotNull(stored);
        Assert.Equal(ClaimStatus.InReview, stored.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsInvalidTransition()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create one claim (Received)
        var arrangeService = CreateService(arrangeDb);
        var received = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());

        var id = received.Id;
        var request = StatusRequest(ClaimStatus.Approved);

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.UpdateStatusAsync(id, request);

        // Assert
        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(id, SeededCustomerId);

        Assert.Equal(UpdateClaimStatusResult.InvalidTransition, result);
        Assert.NotNull(stored);
        Assert.Equal(ClaimStatus.Received, stored.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);

        const int nonExistentId = int.MaxValue;

        var request = StatusRequest(ClaimStatus.InReview);

        // Act
        var result = await service.UpdateStatusAsync(nonExistentId, request);

        // Assert
        Assert.Equal(UpdateClaimStatusResult.NotFound, result);
    }

    [Fact]
    public async Task GetAllAsync_OtherCustomerHasClaims_ReturnsOnlyOwnClaims()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        await arrangeDb.Claims.ExecuteDeleteAsync();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        // One claim for me, one for the other customer
        var myClaim = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());
        await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.GetAllAsync(SeededCustomerId);

        // Assert
        var claim = Assert.Single(result);

        Assert.Equal(myClaim.Id, claim.Id);
    }

    [Fact]
    public async Task GetByIdAsync_OtherCustomersClaim_ReturnsNull()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        var claim = await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.GetByIdAsync(claim.Id, SeededCustomerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_OtherCustomersClaim_ReturnsNotFound()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        var otherCustomerClaim = await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var request = ValidUpdateRequest();

        var result = await actService.UpdateAsync(otherCustomerClaim.Id, SeededCustomerId, request);

        // Assert
        Assert.Equal(UpdateClaimResult.NotFound, result);

        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(otherCustomerClaim.Id, otherCustomer.Id);

        Assert.NotNull(stored);
        Assert.Equal(ValidRequest().Title, stored.Title);
    }

    [Fact]
    public async Task DeleteAsync_OtherCustomersClaim_ReturnsNotFound()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        var otherCustomerClaim = await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.DeleteAsync(otherCustomerClaim.Id, SeededCustomerId);

        // Assert
        Assert.Equal(DeleteClaimResult.NotFound, result);

        await using var assertDb = fixture.CreateDbContext();
        var assertService = CreateService(assertDb);

        var stored = await assertService.GetByIdAsync(otherCustomerClaim.Id, otherCustomer.Id);

        Assert.NotNull(stored);
        Assert.Equal(ValidRequest().Title, stored.Title);
    }

    [Fact]
    public async Task GetAllAsync_NullCustomerId_ReturnsAllCustomerClaims()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();
        await arrangeDb.Claims.ExecuteDeleteAsync();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        // One claim for me, one for the other customer
        var myClaim = await arrangeService.CreateAsync(SeededCustomerId, ValidRequest());
        var otherCustomerClaim = await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.GetAllAsync(null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Id == myClaim.Id);
        Assert.Contains(result, c => c.Id == otherCustomerClaim.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NullCustomerId_ReturnsAnyCustomersClaim()
    {
        // Arrange
        await using var arrangeDb = fixture.CreateDbContext();

        // Create other customer
        var otherCustomer = new User
        {
            Username = $"other-{Guid.NewGuid()}",
            PasswordHash = "not-a-real-hash",
            Role = Roles.Customer,
            CreatedAt = _fixedTime,
        };

        arrangeDb.Users.Add(otherCustomer);
        await arrangeDb.SaveChangesAsync();

        var arrangeService = CreateService(arrangeDb);

        var claim = await arrangeService.CreateAsync(otherCustomer.Id, ValidRequest());

        // Act
        await using var actDb = fixture.CreateDbContext();
        var actService = CreateService(actDb);

        var result = await actService.GetByIdAsync(claim.Id, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(claim.Id, result.Id);
    }
}

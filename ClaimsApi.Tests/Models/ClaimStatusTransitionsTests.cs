using ClaimsApi.Models;

namespace ClaimsApi.Tests.Models;

public class ClaimStatusTransitionsTests
{
    [Theory]
    [InlineData(ClaimStatus.Received, ClaimStatus.InReview)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.Rejected)]
    public void CanTransition_AllowedTransition_ReturnsTrue(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var result = ClaimStatusTransitions.CanTransition(from, to);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(ClaimStatus.Received, ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Received, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.InReview, ClaimStatus.Received)]
    [InlineData(ClaimStatus.Received, ClaimStatus.Received)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Rejected)]
    [InlineData(ClaimStatus.Rejected, ClaimStatus.Approved)]
    public void CanTransition_ForbiddenTransition_ReturnsFalse(ClaimStatus from, ClaimStatus to)
    {
        // Act
        var result = ClaimStatusTransitions.CanTransition(from, to);

        // Assert
        Assert.False(result);
    }
}

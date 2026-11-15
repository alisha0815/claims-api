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
}

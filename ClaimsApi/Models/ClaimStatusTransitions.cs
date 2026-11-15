namespace ClaimsApi.Models;

public static class ClaimStatusTransitions
{
    public static bool CanTransition(ClaimStatus from, ClaimStatus to) =>
        (from, to) switch
        {
            (ClaimStatus.Received, ClaimStatus.InReview) => true,
            (ClaimStatus.InReview, ClaimStatus.Approved) => true,
            (ClaimStatus.InReview, ClaimStatus.Rejected) => true,
            _ => false,
        };
}

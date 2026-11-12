using System.Text.Json.Serialization;

namespace ClaimsApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ClaimStatus>))]
public enum ClaimStatus
{
    Received,
    InReview,
    Approved,
    Rejected,
}

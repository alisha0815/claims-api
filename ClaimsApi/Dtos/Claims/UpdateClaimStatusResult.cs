using System.ComponentModel.DataAnnotations;
using ClaimsApi.Models;

namespace ClaimsApi.Dtos.Claims;

public record UpdateClaimStatusRequest([Required] ClaimStatus? Status);

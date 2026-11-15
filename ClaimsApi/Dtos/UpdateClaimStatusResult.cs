using System.ComponentModel.DataAnnotations;
using ClaimsApi.Models;

namespace ClaimsApi.Dtos;

public record UpdateClaimStatusRequest([Required] ClaimStatus? Status);

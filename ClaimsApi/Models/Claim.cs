using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;


namespace ClaimsApi.Models;

public class Claim
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [MaxLength(50)]
    public required string PolicyNumber { get; set; }

    [MaxLength(100)]
    public required string Title { get; set; }

    public string? Description { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    public DateOnly IncidentDate { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Received;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
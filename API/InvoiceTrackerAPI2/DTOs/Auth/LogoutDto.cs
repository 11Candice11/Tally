using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerAPI2.DTOs.Auth;

public record LogoutDto
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

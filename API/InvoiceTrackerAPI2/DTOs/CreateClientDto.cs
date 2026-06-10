using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackerAPI2.DTOs;

public record CreateClientDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Must be a valid email address.")]
    [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(50, ErrorMessage = "Phone must not exceed 50 characters.")]
    public string? Phone { get; init; }
}

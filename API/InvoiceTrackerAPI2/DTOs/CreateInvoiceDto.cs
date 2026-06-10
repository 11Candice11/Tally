using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using InvoiceTrackerAPI2.Models.Enums;

namespace InvoiceTrackerAPI2.DTOs;

public record CreateInvoiceDto
{
    [Required(ErrorMessage = "Client name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Client name must be between 2 and 200 characters.")]
    public string ClientName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Client email is required.")]
    [EmailAddress(ErrorMessage = "Client email must be a valid email address.")]
    [StringLength(200, ErrorMessage = "Client email must not exceed 200 characters.")]
    public string ClientEmail { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(InvoiceStatus), ErrorMessage = "Invalid invoice status.")]
    public InvoiceStatus Status { get; init; } = InvoiceStatus.Draft;

    [Required(ErrorMessage = "Issue date is required.")]
    public DateTime IssueDate { get; init; } = DateTime.UtcNow;

    [Required(ErrorMessage = "Due date is required.")]
    public DateTime DueDate { get; init; }

    [Range(0, 1, ErrorMessage = "VAT rate must be between 0 and 1 (e.g. 0.15 for 15%).")]
    public decimal VatRate { get; init; } = 0.15m;

    [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative.")]
    public decimal Discount { get; init; } = 0m;

    [Range(0, double.MaxValue, ErrorMessage = "Late fee cannot be negative.")]
    public decimal LateFee { get; init; } = 0m;

    [StringLength(50, ErrorMessage = "Payment terms must not exceed 50 characters.")]
    public string PaymentTerms { get; init; } = "Net 30";

    [StringLength(10, ErrorMessage = "Currency code must not exceed 10 characters.")]
    public string Currency { get; init; } = "ZAR";

    [StringLength(1000, ErrorMessage = "Notes must not exceed 1000 characters.")]
    public string? Notes { get; init; }

    [MinLength(1, ErrorMessage = "At least one line item is required.")]
    public List<CreateLineItemDto> LineItems { get; init; } = [];
}

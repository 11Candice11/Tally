namespace InvoiceTrackerAPI2.DTOs;

public record InvoiceSummaryDto
{
    public decimal TotalRevenue    { get; init; }
    public decimal Outstanding     { get; init; }
    public decimal OverdueTotals   { get; init; }
    public decimal PaidThisMonth   { get; init; }
    public decimal PaidLastMonth   { get; init; }
    public int     OverdueCount    { get; init; }
    public int     PendingCount    { get; init; }
}

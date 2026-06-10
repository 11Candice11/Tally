namespace InvoiceTrackerAPI2.Models;

public class Client
{
    public int     Id        { get; set; }
    public int     UserId    { get; set; }
    public string  Name      { get; set; } = string.Empty;
    public string  Email     { get; set; } = string.Empty;
    public string? Phone     { get; set; }
    public User    User      { get; set; } = null!;
    public List<Invoice> Invoices { get; set; } = [];
}

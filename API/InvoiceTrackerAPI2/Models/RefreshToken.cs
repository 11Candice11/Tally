namespace InvoiceTrackerAPI2.Models;

public class RefreshToken
{
    public int    Id        { get; set; }
    public int    UserId    { get; set; }
    public User   User      { get; set; } = null!;

    // SHA-256 hash of the token sent to the client — never store plaintext
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt  { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public bool     IsRevoked  { get; set; } = false;
}

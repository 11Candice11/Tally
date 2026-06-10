namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface ITokenRevocationService
{
    /// <summary>Marks a token as revoked. TTL matches the token's remaining lifetime.</summary>
    Task RevokeAsync(string jti, TimeSpan ttl);

    /// <summary>Returns true if the token has been revoked.</summary>
    Task<bool> IsRevokedAsync(string jti);
}

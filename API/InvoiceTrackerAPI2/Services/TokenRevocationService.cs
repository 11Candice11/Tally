using InvoiceTrackerAPI2.Services.Interfaces;
using StackExchange.Redis;

namespace InvoiceTrackerAPI2.Services;

// Stores revoked JWT IDs (jti claim) in Redis with a TTL matching the token's
// remaining lifetime. Once the TTL expires the key is automatically removed —
// no manual cleanup needed.
//
// Key format:  revoked:{jti}
// Value:       "1"  (we only care about existence, not the value)
// TTL:         remaining lifetime of the original token

// stores revoked JWT IDs in Redis with a TTL so they auto expire
// looked this up, Redis is perfect for this because you dont need the data after the token expires anyway
public class TokenRevocationService(IConnectionMultiplexer redis) : ITokenRevocationService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public Task RevokeAsync(string jti, TimeSpan ttl) =>
        _db.StringSetAsync($"revoked:{jti}", "1", ttl);

    public async Task<bool> IsRevokedAsync(string jti) =>
        await _db.KeyExistsAsync($"revoked:{jti}");
}

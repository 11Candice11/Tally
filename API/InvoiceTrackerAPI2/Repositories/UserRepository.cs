using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Repositories;

// user repo handles all the auth related db stuff, refresh tokens live here too
// FindByResetTokenAsync checks expiry in the query so expired tokens never come back
public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email);

    public Task<User?> FindByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User> AddAsync(User user)
    {
        db.Users.Add(user);
        return user;
    }

    public Task SaveAsync() => db.SaveChangesAsync();

    public Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash) =>
        db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash);

    public Task RevokeAllRefreshTokensAsync(int userId)
    {
        var tokens = db.RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked);
        db.RefreshTokens.RemoveRange(tokens);
        return Task.CompletedTask;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
    }

    public Task<User?> FindByResetTokenAsync(string email, string tokenHash) =>
        db.Users.FirstOrDefaultAsync(u =>
            u.Email == email &&
            u.PasswordResetToken == tokenHash &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);
}

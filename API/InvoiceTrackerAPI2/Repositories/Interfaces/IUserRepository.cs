using InvoiceTrackerAPI2.Models;

namespace InvoiceTrackerAPI2.Repositories.Interfaces;

public interface IUserRepository
{
    Task<bool>   ExistsByEmailAsync(string email);
    Task<User?>  FindByEmailAsync(string email);
    Task<User>   AddAsync(User user);
    Task         SaveAsync();

    // Refresh tokens
    Task<RefreshToken?> FindRefreshTokenAsync(string tokenHash);
    Task                RevokeAllRefreshTokensAsync(int userId);
    Task                AddRefreshTokenAsync(RefreshToken token);

    // Password reset
    Task<User?> FindByResetTokenAsync(string email, string tokenHash);
}

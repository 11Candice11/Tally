using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InvoiceTrackerAPI2.DTOs.Auth;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace InvoiceTrackerAPI2.Services;

// BCrypt is slow by design, thats the point, makes brute force attacks expensive
// looked this up and the default work factor of 11 is fine for most apps
public class AuthService(
    IUserRepository users,
    IConfiguration config,
    ITokenRevocationService revocation) : IAuthService
{
    private const int AccessTokenMinutes = 50;
    private const int RefreshTokenDays   = 1;

    // ── Register ──────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var cleanEmail = Sanitizer.Email(dto.Email);
        var cleanName  = Sanitizer.Name(dto.Name);

        if (await users.ExistsByEmailAsync(cleanEmail))
            throw new InvalidOperationException("An account with that email already exists.");

        var user = new User
        {
            Name         = cleanName,
            Email        = cleanEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        await users.AddAsync(user);
        await users.SaveAsync();

        return await BuildResponseAsync(user);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var cleanEmail = Sanitizer.Email(dto.Email);
        var user = await users.FindByEmailAsync(cleanEmail)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await BuildResponseAsync(user);
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    // refresh token rotation means every refresh issues a new token and revokes the old one
    // so if someone steals a refresh token and uses it, the legit user gets kicked out on next refresh
    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored    = await users.FindRefreshTokenAsync(tokenHash);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        stored.IsRevoked = true;
        await users.SaveAsync();

        return await BuildResponseAsync(stored.User);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    public async Task LogoutAsync(string jti, DateTime jtiExpiry, string refreshToken)
    {
        var ttl = jtiExpiry - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            await revocation.RevokeAsync(jti, ttl);

        var tokenHash = HashToken(refreshToken);
        var stored    = await users.FindRefreshTokenAsync(tokenHash);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await users.SaveAsync();
        }
    }

    // ── Forgot / Reset password ───────────────────────────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var cleanEmail = Sanitizer.Email(dto.Email);
        var user       = await users.FindByEmailAsync(cleanEmail);
        if (user is null) return;

        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken       = HashToken(plaintext);
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await users.SaveAsync();
        // TODO send the reset link via email — currently the token is stored but never emailed to the user
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var cleanEmail = Sanitizer.Email(dto.Email);
        var tokenHash  = HashToken(dto.Token.Trim());

        var user = await users.FindByResetTokenAsync(cleanEmail, tokenHash)
            ?? throw new InvalidOperationException("Invalid or expired reset token.");

        user.PasswordHash             = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken       = null;
        user.PasswordResetTokenExpiry = null;
        await users.SaveAsync();
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponseDto> CreateAdminAsync(RegisterDto dto)
    {
        var cleanEmail = Sanitizer.Email(dto.Email);
        var cleanName  = Sanitizer.Name(dto.Name);

        if (await users.ExistsByEmailAsync(cleanEmail))
            throw new InvalidOperationException("An account with that email already exists.");

        var user = new User
        {
            Name         = cleanName,
            Email        = cleanEmail,
            Role         = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };
        await users.AddAsync(user);
        await users.SaveAsync();

        return await BuildResponseAsync(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AuthResponseDto> BuildResponseAsync(User user)
    {
        var (accessToken, _, _) = GenerateAccessToken(user);
        var refreshPlaintext    = await CreateRefreshTokenAsync(user.Id);

        return new AuthResponseDto
        {
            Token        = accessToken,
            RefreshToken = refreshPlaintext,
            User         = new UserDto { Id = user.Id, Name = user.Name, Email = user.Email }
        };
    }

    private (string token, string jti, DateTime expiry) GenerateAccessToken(User user)
    {
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jti    = Guid.NewGuid().ToString();
        var expiry = DateTime.UtcNow.AddMinutes(AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name",                        user.Name),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim(JwtRegisteredClaimNames.Jti,   jti),
        };

        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), jti, expiry);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId)
    {
        // TODO consider storing refresh tokens in HttpOnly cookies instead of returning them in the response body
        await users.RevokeAllRefreshTokensAsync(userId);

        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        await users.AddRefreshTokenAsync(new RefreshToken
        {
            UserId    = userId,
            TokenHash = HashToken(plaintext),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
        });
        await users.SaveAsync();
        return plaintext;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

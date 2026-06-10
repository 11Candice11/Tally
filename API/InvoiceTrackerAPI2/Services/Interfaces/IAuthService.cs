using InvoiceTrackerAPI2.DTOs.Auth;

namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task<AuthResponseDto> CreateAdminAsync(RegisterDto dto);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    Task LogoutAsync(string jti, DateTime jtiExpiry, string refreshToken);
}

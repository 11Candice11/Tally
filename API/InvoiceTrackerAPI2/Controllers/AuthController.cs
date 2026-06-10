using InvoiceTrackerAPI2.DTOs.Auth;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace InvoiceTrackerAPI2.Controllers;

// handles all auth routes, register/login/logout/refresh and password reset
// all the heavy lifting is in AuthService, this just maps http responses
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        try
        {
            return Ok(await authService.RegisterAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            return Ok(await authService.LoginAsync(dto));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto)
    {
        try
        {
            return Ok(await authService.RefreshAsync(dto.RefreshToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutDto dto)
    {
        var jti      = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        var expiry   = expClaim is not null
            ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
            : DateTime.UtcNow;

        await authService.LogoutAsync(jti, expiry, dto.RefreshToken);
        return Ok(new { message = "Logged out successfully." });
    }

// forgot password always returns 200 even if email doesnt exist
// this is intentional so you cant enumerate valid emails
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await authService.ForgotPasswordAsync(dto);
        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        try
        {
            await authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("admin/create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAdmin(RegisterDto dto)
    {
        try
        {
            return Ok(await authService.CreateAdminAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

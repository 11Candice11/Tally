using InvoiceTrackerAPI2.Controllers;
using InvoiceTrackerAPI2.DTOs.Auth;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvoiceTrackerAPI2.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authSvc = new();
    private AuthController CreateController() => new(_authSvc.Object);

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Success_Returns200WithToken()
    {
        var response = new AuthResponseDto { Token = "jwt", User = new UserDto { Id = 1, Name = "Alice", Email = "alice@test.com" } };
        _authSvc.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>())).ReturnsAsync(response);

        var result = await CreateController().Register(new RegisterDto { Name = "Alice", Email = "alice@test.com", Password = "password123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        _authSvc.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new InvalidOperationException("An account with that email already exists."));

        var result = await CreateController().Register(new RegisterDto { Name = "Alice", Email = "alice@test.com", Password = "password123" });

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = new AuthResponseDto { Token = "jwt", User = new UserDto { Id = 1, Name = "Bob", Email = "bob@test.com" } };
        _authSvc.Setup(s => s.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync(response);

        var result = await CreateController().Login(new LoginDto { Email = "bob@test.com", Password = "password123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _authSvc.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password."));

        var result = await CreateController().Login(new LoginDto { Email = "bob@test.com", Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ── Forgot password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AlwaysReturns200()
    {
        _authSvc.Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordDto>())).Returns(Task.CompletedTask);

        var result = await CreateController().ForgotPassword(new ForgotPasswordDto { Email = "anyone@test.com" });

        Assert.IsType<OkObjectResult>(result);
    }

    // ── Reset password ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200()
    {
        _authSvc.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordDto>())).Returns(Task.CompletedTask);

        var result = await CreateController().ResetPassword(new ResetPasswordDto { Token = "tok", Email = "a@b.com", NewPassword = "newpass123" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        _authSvc.Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired reset token."));

        var result = await CreateController().ResetPassword(new ResetPasswordDto { Token = "bad", Email = "a@b.com", NewPassword = "newpass123" });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}

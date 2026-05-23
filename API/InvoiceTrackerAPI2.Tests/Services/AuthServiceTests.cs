using InvoiceTrackerAPI2.DTOs.Auth;
using InvoiceTrackerAPI2.Repositories;
using InvoiceTrackerAPI2.Services;
using InvoiceTrackerAPI2.Services.Interfaces;
using InvoiceTrackerAPI2.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace InvoiceTrackerAPI2.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(string? dbName = null)
    {
        var db         = TestDbContext.Create(dbName);
        var userRepo   = new UserRepository(db);
        var config     = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = "super_secret_test_key_that_is_long_enough_32chars",
                ["Jwt:Issuer"]   = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();
        var revocation = new Mock<ITokenRevocationService>();
        revocation.Setup(r => r.RevokeAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                  .Returns(Task.CompletedTask);
        return new AuthService(userRepo, config, revocation.Object);
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidDto_ReturnsTokenAndUser()
    {
        var svc    = CreateService();
        var dto    = new RegisterDto { Name = "Alice", Email = "alice@test.com", Password = "password123" };

        var result = await svc.RegisterAsync(dto);

        Assert.NotEmpty(result.Token);
        Assert.Equal("alice@test.com", result.User.Email);
        Assert.Equal("Alice", result.User.Name);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        var dto    = new RegisterDto { Name = "Alice", Email = "alice@test.com", Password = "password123" };

        await svc.RegisterAsync(dto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RegisterAsync(dto));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.RegisterAsync(new RegisterDto { Name = "Bob", Email = "bob@test.com", Password = "password123" });

        var result = await svc.LoginAsync(new LoginDto { Email = "bob@test.com", Password = "password123" });

        Assert.NotEmpty(result.Token);
        Assert.Equal("bob@test.com", result.User.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.RegisterAsync(new RegisterDto { Name = "Bob", Email = "bob@test.com", Password = "password123" });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginDto { Email = "bob@test.com", Password = "wrongpassword" }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorizedAccessException()
    {
        var svc = CreateService();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginDto { Email = "nobody@test.com", Password = "password123" }));
    }

    // ── Forgot / Reset password ───────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotThrow()
    {
        var svc = CreateService();

        await svc.ForgotPasswordAsync(new ForgotPasswordDto { Email = "ghost@test.com" });
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsInvalidOperationException()
    {
        var svc = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ResetPasswordAsync(new ResetPasswordDto
            {
                Email       = "nobody@test.com",
                Token       = "badtoken",
                NewPassword = "newpassword123"
            }));
    }

    [Fact]
    public async Task ForgotPassword_WrongTokenAfterRequest_StillFails()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.RegisterAsync(new RegisterDto { Name = "Carol", Email = "carol@test.com", Password = "password123" });
        await svc.ForgotPasswordAsync(new ForgotPasswordDto { Email = "carol@test.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ResetPasswordAsync(new ResetPasswordDto
            {
                Email       = "carol@test.com",
                Token       = "wrongtoken",
                NewPassword = "newpassword123"
            }));
    }
}

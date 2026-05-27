using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Repositories;
using InvoiceTrackerAPI2.Services;
using InvoiceTrackerAPI2.Tests.Helpers;

namespace InvoiceTrackerAPI2.Tests.Services;

public class ClientServiceTests
{
    private static ClientService CreateService(string? dbName = null)
    {
        var db   = TestDbContext.Create(dbName);
        var repo = new ClientRepository(db);
        return new ClientService(repo, db);
    }

    private static CreateClientDto SampleDto(string name = "Acme Corp", string email = "billing@acme.com") => new()
    {
        Name  = name,
        Email = email,
        Phone = "+27 11 000 0000",
    };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_ReturnsClientWithId()
    {
        var svc = CreateService();

        var result = await svc.CreateAsync(userId: 1, SampleDto());

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Acme Corp", result.Name);
        Assert.Equal("billing@acme.com", result.Email);
    }

    [Fact]
    public async Task Create_NormalisesEmailToLowercase()
    {
        var svc = CreateService();

        var result = await svc.CreateAsync(userId: 1, SampleDto(email: "Billing@ACME.COM"));

        Assert.Equal("billing@acme.com", result.Email);
    }

    [Fact]
    public async Task Create_DuplicateEmailSameUser_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.CreateAsync(userId: 1, SampleDto());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(userId: 1, SampleDto()));
    }

    [Fact]
    public async Task Create_SameEmailDifferentUser_Succeeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.CreateAsync(userId: 1, SampleDto());

        // Different user — should not conflict
        var result = await svc.CreateAsync(userId: 2, SampleDto());

        Assert.NotEqual(0, result.Id);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyClientsForUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.CreateAsync(userId: 1, SampleDto("User1 Client", "user1@test.com"));
        await svc.CreateAsync(userId: 2, SampleDto("User2 Client", "user2@test.com"));

        var result = (await svc.GetAllAsync(userId: 1)).ToList();

        Assert.Single(result);
        Assert.Equal("User1 Client", result[0].Name);
    }

    [Fact]
    public async Task GetAll_ReturnsClientsOrderedByName()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.CreateAsync(userId: 1, SampleDto("Zeta Corp",  "zeta@test.com"));
        await svc.CreateAsync(userId: 1, SampleDto("Alpha Ltd",  "alpha@test.com"));
        await svc.CreateAsync(userId: 1, SampleDto("Beta Inc",   "beta@test.com"));

        var result = (await svc.GetAllAsync(userId: 1)).ToList();

        Assert.Equal("Alpha Ltd",  result[0].Name);
        Assert.Equal("Beta Inc",   result[1].Name);
        Assert.Equal("Zeta Corp",  result[2].Name);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsClient()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var result = await svc.GetByIdAsync(created.Id, userId: 1);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
    }

    [Fact]
    public async Task GetById_WrongUser_ReturnsNull()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var result = await svc.GetByIdAsync(created.Id, userId: 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        var svc = CreateService();

        var result = await svc.GetByIdAsync(id: 9999, userId: 1);

        Assert.Null(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidDto_UpdatesName()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var updated = await svc.UpdateAsync(created.Id, userId: 1, new UpdateClientDto { Name = "Updated Corp" });

        Assert.NotNull(updated);
        Assert.Equal("Updated Corp", updated!.Name);
    }

    [Fact]
    public async Task Update_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);
        await svc.CreateAsync(userId: 1, SampleDto("Client A", "a@test.com"));
        var b = await svc.CreateAsync(userId: 1, SampleDto("Client B", "b@test.com"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateAsync(b.Id, userId: 1, new UpdateClientDto { Email = "a@test.com" }));
    }

    [Fact]
    public async Task Update_WrongUser_ReturnsNull()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var result = await svc.UpdateAsync(created.Id, userId: 99, new UpdateClientDto { Name = "X" });

        Assert.Null(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingClient_ReturnsTrue()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var deleted = await svc.DeleteAsync(created.Id, userId: 1);

        Assert.True(deleted);
    }

    [Fact]
    public async Task Delete_NonExistentClient_ReturnsFalse()
    {
        var svc = CreateService();

        var deleted = await svc.DeleteAsync(id: 9999, userId: 1);

        Assert.False(deleted);
    }

    [Fact]
    public async Task Delete_WrongUser_ReturnsFalse()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleDto());

        var deleted = await svc.DeleteAsync(created.Id, userId: 99);

        Assert.False(deleted);
    }
}

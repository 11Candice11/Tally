using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Repositories;
using InvoiceTrackerAPI2.Services;
using InvoiceTrackerAPI2.Tests.Helpers;

namespace InvoiceTrackerAPI2.Tests.Services;

public class InvoiceServiceTests
{
    private static InvoiceService CreateService(string? dbName = null)
    {
        var db        = TestDbContext.Create(dbName);
        var repo      = new InvoiceRepository(db);
        var mapper    = MapperFactory.Create();
        var allocator = new FakeInvoiceNumberAllocator();
        return new InvoiceService(repo, mapper, allocator);
    }

    private static CreateInvoiceDto SampleCreateDto(string client = "Acme Corp") => new()
    {
        ClientName  = client,
        ClientEmail = "billing@acme.com",
        Status      = InvoiceStatus.Draft,
        IssueDate   = DateTime.UtcNow,
        DueDate     = DateTime.UtcNow.AddDays(30),
        VatRate     = 0.15m,
        LineItems   =
        [
            new CreateLineItemDto { Description = "Consulting", Qty = 2, UnitPrice = 1000m }
        ]
    };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_ReturnsInvoiceWithGeneratedNumber()
    {
        var svc = CreateService();

        var result = await svc.CreateAsync(userId: 1, SampleCreateDto());

        Assert.NotEqual(0, result.Id);
        Assert.StartsWith("INV-", result.InvoiceNumber);
        Assert.Equal("Acme Corp", result.ClientName);
    }

    [Fact]
    public async Task Create_MultipleInvoices_NumbersAreUniqueAndSequential()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);

        var a = await svc.CreateAsync(userId: 1, SampleCreateDto());
        var b = await svc.CreateAsync(userId: 1, SampleCreateDto());
        var c = await svc.CreateAsync(userId: 1, SampleCreateDto());

        var numbers = new[] { a.InvoiceNumber, b.InvoiceNumber, c.InvoiceNumber };
        Assert.Equal(numbers.Distinct().Count(), numbers.Length); // all unique
        Assert.EndsWith("-0001", numbers[0]);
        Assert.EndsWith("-0002", numbers[1]);
        Assert.EndsWith("-0003", numbers[2]);
    }

    [Fact]
    public async Task Create_ComputesTotalsCorrectly()
    {
        var svc = CreateService();

        var result = await svc.CreateAsync(userId: 1, SampleCreateDto());

        // 2 × 1000 = 2000 subtotal, 15% VAT = 300, total = 2300
        Assert.Equal(2000m, result.Subtotal);
        Assert.Equal(300m,  result.VatAmount);
        Assert.Equal(2300m, result.Total);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOnlyInvoicesForUser()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);

        await svc.CreateAsync(userId: 1, SampleCreateDto("User1 Client"));
        await svc.CreateAsync(userId: 2, SampleCreateDto("User2 Client"));

        var result = await svc.GetAllAsync(userId: 1, new InvoiceFilterDto());

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("User1 Client", result.Items.First().ClientName);
    }

    [Fact]
    public async Task GetAll_FilterByStatus_ReturnsMatchingInvoices()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);

        await svc.CreateAsync(1, SampleCreateDto() with { Status = InvoiceStatus.Draft });
        await svc.CreateAsync(1, SampleCreateDto() with { Status = InvoiceStatus.Sent });

        var result = await svc.GetAllAsync(1, new InvoiceFilterDto { Status = InvoiceStatus.Draft });

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal(InvoiceStatus.Draft, result.Items.First().Status);
    }

    [Fact]
    public async Task GetAll_Pagination_ReturnsCorrectPage()
    {
        var dbName = Guid.NewGuid().ToString();
        var svc    = CreateService(dbName);

        for (var i = 0; i < 5; i++)
            await svc.CreateAsync(userId: 1, SampleCreateDto());

        var result = await svc.GetAllAsync(userId: 1, new InvoiceFilterDto { Page = 2, PageSize = 2 });

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsInvoice()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(1, SampleCreateDto());

        var result = await svc.GetByIdAsync(created.Id, userId: 1);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetById_WrongUser_ReturnsNull()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());

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
    public async Task Update_ValidDto_UpdatesInvoice()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(1, SampleCreateDto());

        var updated = await svc.UpdateAsync(created.Id, userId: 1, new UpdateInvoiceDto { ClientName = "Updated Client" });

        Assert.NotNull(updated);
        Assert.Equal("Updated Client", updated!.ClientName);
    }

    [Fact]
    public async Task Update_WrongUser_ReturnsNull()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());

        var result = await svc.UpdateAsync(created.Id, userId: 99, new UpdateInvoiceDto { ClientName = "X" });

        Assert.Null(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingInvoice_ReturnsTrue()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(1, SampleCreateDto());

        var deleted = await svc.DeleteAsync(created.Id, userId: 1);

        Assert.True(deleted);
    }

    [Fact]
    public async Task Delete_NonExistentInvoice_ReturnsFalse()
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
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());

        var deleted = await svc.DeleteAsync(created.Id, userId: 99);

        Assert.False(deleted);
    }

    // ── UpdateStatus lifecycle ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ValidTransition_Succeeds()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());

        var result = await svc.UpdateStatusAsync(created.Id, userId: 1, InvoiceStatus.Sent);

        Assert.NotNull(result);
        Assert.Equal(InvoiceStatus.Sent, result!.Status);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ThrowsInvalidOperationException()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());

        // Draft → Paid is not allowed
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateStatusAsync(created.Id, userId: 1, InvoiceStatus.Paid));
    }

    [Fact]
    public async Task UpdateStatus_PaidInvoice_CannotBeChanged()
    {
        var dbName  = Guid.NewGuid().ToString();
        var svc     = CreateService(dbName);
        var created = await svc.CreateAsync(userId: 1, SampleCreateDto());
        await svc.UpdateStatusAsync(created.Id, userId: 1, InvoiceStatus.Sent);
        await svc.UpdateStatusAsync(created.Id, userId: 1, InvoiceStatus.Paid);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.UpdateStatusAsync(created.Id, userId: 1, InvoiceStatus.Draft));
    }
}

using System.Security.Claims;
using InvoiceTrackerAPI2.Controllers;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvoiceTrackerAPI2.Tests.Controllers;

public class ClientsControllerTests
{
    private readonly Mock<IClientService> _svc = new();

    private ClientsController CreateController(int userId = 1)
    {
        var controller = new ClientsController(_svc.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                ]))
            }
        };
        return controller;
    }

    private static ClientDto SampleDto(int id = 1) => new()
    {
        Id           = id,
        Name         = "Acme Corp",
        Email        = "billing@acme.com",
        Phone        = "+27 11 000 0000",
        InvoiceCount = 2,
        TotalBilled  = 5000m,
        LastInvoice  = "2025-04-01",
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithClientList()
    {
        _svc.Setup(s => s.GetAllAsync(1))
            .ReturnsAsync([SampleDto(1), SampleDto(2)]);

        var result = await CreateController().GetAll();

        var ok      = Assert.IsType<OkObjectResult>(result);
        var clients = Assert.IsAssignableFrom<IEnumerable<ClientDto>>(ok.Value);
        Assert.Equal(2, clients.Count());
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Found_Returns200()
    {
        _svc.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(SampleDto(1));

        var result = await CreateController().GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetByIdAsync(99, 1)).ReturnsAsync((ClientDto?)null);

        var result = await CreateController().GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        var dto = new CreateClientDto { Name = "Acme Corp", Email = "billing@acme.com" };
        _svc.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(SampleDto(1));

        var result = await CreateController().Create(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ClientsController.GetById), created.ActionName);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409Conflict()
    {
        var dto = new CreateClientDto { Name = "Acme Corp", Email = "billing@acme.com" };
        _svc.Setup(s => s.CreateAsync(1, dto))
            .ThrowsAsync(new InvalidOperationException("A client with that email already exists."));

        var result = await CreateController().Create(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Found_Returns200()
    {
        var dto = new UpdateClientDto { Name = "Updated Corp" };
        _svc.Setup(s => s.UpdateAsync(1, 1, dto)).ReturnsAsync(SampleDto(1));

        var result = await CreateController().Update(1, dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var dto = new UpdateClientDto { Name = "X" };
        _svc.Setup(s => s.UpdateAsync(99, 1, dto)).ReturnsAsync((ClientDto?)null);

        var result = await CreateController().Update(99, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_DuplicateEmail_Returns409Conflict()
    {
        var dto = new UpdateClientDto { Email = "taken@test.com" };
        _svc.Setup(s => s.UpdateAsync(1, 1, dto))
            .ThrowsAsync(new InvalidOperationException("A client with that email already exists."));

        var result = await CreateController().Update(1, dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Existing_Returns204()
    {
        _svc.Setup(s => s.DeleteAsync(1, 1)).ReturnsAsync(true);

        var result = await CreateController().Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        _svc.Setup(s => s.DeleteAsync(99, 1)).ReturnsAsync(false);

        var result = await CreateController().Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }
}

using System.Security.Claims;
using InvoiceTrackerAPI2.Controllers;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InvoiceTrackerAPI2.Tests.Controllers;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _svc   = new();
    private readonly Mock<IEmailService>   _email = new();

    private InvoicesController CreateController(int userId = 1)
    {
        var controller = new InvoicesController(_svc.Object, _email.Object);
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

    private static InvoiceDto SampleDto(int id = 1, InvoiceStatus status = InvoiceStatus.Draft) => new()
    {
        Id            = id,
        InvoiceNumber = "INV-202504-ABC123",
        ClientName    = "Acme Corp",
        ClientEmail   = "billing@acme.com",
        Status        = status,
        IssueDate     = DateTime.UtcNow,
        DueDate       = DateTime.UtcNow.AddDays(30),
        VatRate       = 0.15m,
        Subtotal      = 2000m,
        VatAmount     = 300m,
        Total         = 2300m,
        LineItems     = []
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithPagedResult()
    {
        _svc.Setup(s => s.GetAllAsync(1, It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new PagedResult<InvoiceDto>
            {
                Items    = [SampleDto(1), SampleDto(2)],
                Total    = 2,
                Page     = 1,
                PageSize = 20,
            });

        var result = await CreateController().GetAll(new InvoiceFilterDto());

        var ok     = Assert.IsType<OkObjectResult>(result);
        var paged  = Assert.IsType<PagedResult<InvoiceDto>>(ok.Value);
        Assert.Equal(2, paged.Total);
        Assert.Equal(2, paged.Items.Count());
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
        _svc.Setup(s => s.GetByIdAsync(99, 1)).ReturnsAsync((InvoiceDto?)null);

        var result = await CreateController().GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_Returns201Created()
    {
        var dto = new CreateInvoiceDto
        {
            ClientName  = "Acme",
            ClientEmail = "billing@acme.com",
            DueDate     = DateTime.UtcNow.AddDays(30),
            LineItems   = [new CreateLineItemDto { Description = "Work", Qty = 1, UnitPrice = 500 }]
        };
        _svc.Setup(s => s.CreateAsync(1, dto)).ReturnsAsync(SampleDto(1));

        var result = await CreateController().Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Found_Returns200()
    {
        var dto = new UpdateInvoiceDto { ClientName = "Updated" };
        _svc.Setup(s => s.UpdateAsync(1, 1, dto)).ReturnsAsync(SampleDto(1));

        var result = await CreateController().Update(1, dto);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var dto = new UpdateInvoiceDto { ClientName = "X" };
        _svc.Setup(s => s.UpdateAsync(99, 1, dto)).ReturnsAsync((InvoiceDto?)null);

        var result = await CreateController().Update(99, dto);

        Assert.IsType<NotFoundResult>(result);
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

    // ── Send ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_DraftInvoice_CommitsStatusThenSendsEmail_Returns200()
    {
        var dto = new SendInvoiceDto { ToEmail = "client@test.com" };
        _svc.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(SampleDto(1, InvoiceStatus.Draft));
        _svc.Setup(s => s.UpdateStatusAsync(1, 1, InvoiceStatus.Sent)).ReturnsAsync(SampleDto(1, InvoiceStatus.Sent));
        _email.Setup(e => e.SendInvoiceAsync(It.IsAny<string>(), It.IsAny<InvoiceDto>()))
              .Returns(Task.CompletedTask);

        var result = await CreateController().Send(1, dto);

        // Status committed before email
        _svc.Verify(s => s.UpdateStatusAsync(1, 1, InvoiceStatus.Sent), Times.Once);
        _email.Verify(e => e.SendInvoiceAsync(It.IsAny<string>(), It.IsAny<InvoiceDto>()), Times.Once);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Send_AlreadySentInvoice_Returns200WithoutSendingEmail()
    {
        var dto = new SendInvoiceDto { ToEmail = "client@test.com" };
        _svc.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(SampleDto(1, InvoiceStatus.Sent));

        var result = await CreateController().Send(1, dto);

        _email.Verify(e => e.SendInvoiceAsync(It.IsAny<string>(), It.IsAny<InvoiceDto>()), Times.Never);
        _svc.Verify(s => s.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<InvoiceStatus>()), Times.Never);
        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task Send_TerminalStatus_Returns409WithoutSendingEmail(InvoiceStatus status)
    {
        var dto = new SendInvoiceDto { ToEmail = "client@test.com" };
        _svc.Setup(s => s.GetByIdAsync(1, 1)).ReturnsAsync(SampleDto(1, status));

        var result = await CreateController().Send(1, dto);

        _email.Verify(e => e.SendInvoiceAsync(It.IsAny<string>(), It.IsAny<InvoiceDto>()), Times.Never);
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Send_NotFound_Returns404()
    {
        _svc.Setup(s => s.GetByIdAsync(99, 1)).ReturnsAsync((InvoiceDto?)null);

        var result = await CreateController().Send(99, new SendInvoiceDto { ToEmail = "x@test.com" });

        Assert.IsType<NotFoundResult>(result);
    }
}

using System.Security.Claims;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace InvoiceTrackerAPI2.Controllers;

// API is fully stateless

[ApiController]
[Route("api/invoices")]
[Authorize] // requires valid JWT
public class InvoicesController(IInvoiceService invoiceService, IEmailService emailService) : ControllerBase
{
    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceFilterDto filter)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await invoiceService.GetAllAsync(userId, filter));
    }

// summary endpoint is cached per user for 30 seconds, output caching is configured in Program.cs
// the SummaryCache policy varies by Authorization header so users dont see each others data
    [HttpGet("summary")]
    [OutputCache(PolicyName = "SummaryCache")]
    public async Task<IActionResult> GetSummary()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await invoiceService.GetSummaryAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var invoice = await invoiceService.GetByIdAsync(id, userId);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var created = await invoiceService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateInvoiceDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var updated = await invoiceService.UpdateAsync(id, userId, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var updated = await invoiceService.UpdateStatusAsync(id, userId, dto.Status);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var deleted = await invoiceService.DeleteAsync(id, userId);
        return deleted ? NoContent() : NotFound();
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    // GET /api/invoices/admin — all invoices across all users
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAdmin([FromQuery] InvoiceFilterDto filter) =>
        Ok(await invoiceService.GetAllAdminAsync(filter));

    // GET /api/invoices/admin/{id} — any invoice by id, regardless of owner
    [HttpGet("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByIdAdmin(int id)
    {
        var invoice = await invoiceService.GetByIdAdminAsync(id);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    // DELETE /api/invoices/admin/{id} — delete any invoice regardless of owner
    [HttpDelete("admin/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAdmin(int id)
    {
        var deleted = await invoiceService.DeleteAdminAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // send marks the invoice as sent then emails it, if its already sent we just return ok
    // paid and cancelled invoices cant be sent, that would be weird
    [HttpPost("{id:int}/send")]
    public async Task<IActionResult> Send(int id, [FromBody] SendInvoiceDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var invoice = await invoiceService.GetByIdAsync(id, userId);
        if (invoice is null) return NotFound();

        if (invoice.Status == InvoiceStatus.Sent || invoice.Status == InvoiceStatus.Overdue)
            return Ok(new { message = "Invoice already sent." });

        if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
            return Conflict(new { message = $"Cannot send an invoice with status {invoice.Status}." });

        await invoiceService.UpdateStatusAsync(id, userId, InvoiceStatus.Sent);
        await emailService.SendInvoiceAsync(dto.ToEmail, invoice);

        return Ok(new { message = "Invoice sent successfully." });
    }
}

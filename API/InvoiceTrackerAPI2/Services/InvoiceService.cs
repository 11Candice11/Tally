using AutoMapper;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Models.Enums;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using InvoiceTrackerAPI2.Services.Interfaces;

namespace InvoiceTrackerAPI2.Services;

// status transitions are enforced here, cant go from paid back to draft etc
// the AllowedTransitions dict below defines what moves are legal
public class InvoiceService(IInvoiceRepository repo, IMapper mapper, IInvoiceNumberAllocator allocator) : IInvoiceService
{
    public async Task<PagedResult<InvoiceDto>> GetAllAsync(int userId, InvoiceFilterDto filter)
    {
        var (items, total) = await repo.GetAllAsync(userId, filter);
        return new PagedResult<InvoiceDto>
        {
            Items    = mapper.Map<IEnumerable<InvoiceDto>>(items),
            Total    = total,
            Page     = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<InvoiceDto?> GetByIdAsync(int id, int userId)
    {
        var invoice = await repo.GetByIdAsync(id, userId);
        return invoice is null ? null : mapper.Map<InvoiceDto>(invoice);
    }

    // sanitize inputs before mapping to the entity, strip any html tags someone might try sneak in
    public async Task<InvoiceDto> CreateAsync(int userId, CreateInvoiceDto dto)
    {
        var sanitized = dto with
        {
            ClientName  = Sanitizer.Name(dto.ClientName),
            ClientEmail = Sanitizer.Email(dto.ClientEmail),
            Notes       = Sanitizer.NullableText(dto.Notes),
            LineItems   = dto.LineItems
                .Select(l => l with { Description = Sanitizer.Text(l.Description) })
                .ToList(),
        };

        var invoice = mapper.Map<Invoice>(sanitized);
        invoice.UserId        = userId;
        invoice.InvoiceNumber = await allocator.NextAsync(userId);

        var created = await repo.CreateAsync(invoice);
        return mapper.Map<InvoiceDto>(created);
    }

    public async Task<InvoiceDto?> UpdateAsync(int id, int userId, UpdateInvoiceDto dto)
    {
        var existing = await repo.GetByIdAsync(id, userId);
        if (existing is null) return null;

        var sanitized = dto with
        {
            ClientName  = dto.ClientName  is not null ? Sanitizer.Name(dto.ClientName)  : null,
            ClientEmail = dto.ClientEmail is not null ? Sanitizer.Email(dto.ClientEmail) : null,
            Notes       = dto.Notes       is not null ? Sanitizer.NullableText(dto.Notes) : null,
            LineItems   = dto.LineItems?
                .Select(l => l with { Description = Sanitizer.Text(l.Description) })
                .ToList(),
        };

        mapper.Map(sanitized, existing);
        var updated = await repo.UpdateAsync(existing);
        return mapper.Map<InvoiceDto>(updated);
    }

    public async Task<InvoiceDto?> UpdateStatusAsync(int id, int userId, InvoiceStatus status)
    {
        var existing = await repo.GetByIdAsync(id, userId);
        if (existing is null) return null;

        if (!AllowedTransitions.TryGetValue(existing.Status, out var allowed) || !allowed.Contains(status))
            throw new InvalidOperationException(
                $"Cannot transition from {existing.Status} to {status}.");

        existing.Status = status;
        var updated = await repo.UpdateAsync(existing);
        return mapper.Map<InvoiceDto>(updated);
    }

    private static readonly Dictionary<InvoiceStatus, InvoiceStatus[]> AllowedTransitions = new()
    {
        [InvoiceStatus.Draft]     = [InvoiceStatus.Sent,    InvoiceStatus.Cancelled],
        [InvoiceStatus.Sent]      = [InvoiceStatus.Paid,    InvoiceStatus.Overdue,  InvoiceStatus.Cancelled],
        [InvoiceStatus.Overdue]   = [InvoiceStatus.Paid,    InvoiceStatus.Cancelled],
        [InvoiceStatus.Paid]      = [],
        [InvoiceStatus.Cancelled] = [],
    };

    public Task<bool> DeleteAsync(int id, int userId) => repo.DeleteAsync(id, userId);

    public Task<InvoiceSummaryDto> GetSummaryAsync(int userId) => repo.GetSummaryAsync(userId);

    // ── Admin methods ─────────────────────────────────────────────────────────

    public async Task<PagedResult<InvoiceDto>> GetAllAdminAsync(InvoiceFilterDto filter)
    {
        var (items, total) = await repo.GetAllAdminAsync(filter);
        return new PagedResult<InvoiceDto>
        {
            Items    = mapper.Map<IEnumerable<InvoiceDto>>(items),
            Total    = total,
            Page     = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<InvoiceDto?> GetByIdAdminAsync(int id)
    {
        var invoice = await repo.GetByIdAdminAsync(id);
        return invoice is null ? null : mapper.Map<InvoiceDto>(invoice);
    }

    public Task<bool> DeleteAdminAsync(int id) => repo.DeleteAdminAsync(id);

}

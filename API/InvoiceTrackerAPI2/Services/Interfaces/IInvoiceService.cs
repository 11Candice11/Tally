using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models.Enums;

namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetAllAsync(int userId, InvoiceFilterDto filter);
    Task<InvoiceDto?> GetByIdAsync(int id, int userId);
    Task<InvoiceDto> CreateAsync(int userId, CreateInvoiceDto dto);
    Task<InvoiceDto?> UpdateAsync(int id, int userId, UpdateInvoiceDto dto);
    Task<InvoiceDto?> UpdateStatusAsync(int id, int userId, InvoiceStatus status);
    Task<bool> DeleteAsync(int id, int userId);
    Task<InvoiceSummaryDto> GetSummaryAsync(int userId);

    // Admin — no userId ownership filter
    Task<PagedResult<InvoiceDto>> GetAllAdminAsync(InvoiceFilterDto filter);
    Task<InvoiceDto?> GetByIdAdminAsync(int id);
    Task<bool> DeleteAdminAsync(int id);
}

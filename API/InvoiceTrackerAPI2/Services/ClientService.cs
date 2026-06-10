using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Services;

// does a single query joining clients to their invoices to get the stats
// way better than loading all invoices separately for each client
public class ClientService(IClientRepository repo, AppDbContext db) : IClientService
{
    public async Task<IEnumerable<ClientDto>> GetAllAsync(int userId)
    {
        // Single query: join clients → invoices, aggregate stats in SQL
        var rows = await db.Clients
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new ClientDto
            {
                Id           = c.Id,
                Name         = c.Name,
                Email        = c.Email,
                Phone        = c.Phone,
                InvoiceCount = c.Invoices.Count,
                TotalBilled  = c.Invoices.Sum(i =>
                    i.LineItems.Sum(l => (decimal)l.Qty * l.UnitPrice) * (1 + i.VatRate)
                    - i.Discount + i.LateFee),
                LastInvoice  = c.Invoices
                    .OrderByDescending(i => i.IssueDate)
                    .Select(i => i.IssueDate.ToString())
                    .FirstOrDefault(),
            })
            .ToListAsync();

        return rows;
    }

    public async Task<ClientDto?> GetByIdAsync(int id, int userId)
    {
        var client = await repo.GetByIdAsync(id, userId);
        return client is null ? null : ToDto(client);
    }

    public async Task<ClientDto> CreateAsync(int userId, CreateClientDto dto)
    {
        // always lowercase the email before saving so lookups are case insensitive
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await repo.FindByEmailAsync(userId, email) is not null)
            throw new InvalidOperationException("A client with that email already exists.");

        var client = new Client
        {
            UserId = userId,
            Name   = dto.Name.Trim(),
            Email  = email,
            Phone  = dto.Phone?.Trim(),
        };

        var created = await repo.CreateAsync(client);
        return ToDto(created);
    }

    public async Task<ClientDto?> UpdateAsync(int id, int userId, UpdateClientDto dto)
    {
        var client = await repo.GetByIdAsync(id, userId);
        if (client is null) return null;

        if (dto.Name  is not null) client.Name  = dto.Name.Trim();
        if (dto.Phone is not null) client.Phone = dto.Phone.Trim();

        if (dto.Email is not null)
        {
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            if (newEmail != client.Email)
            {
                if (await repo.FindByEmailAsync(userId, newEmail) is not null)
                    throw new InvalidOperationException("A client with that email already exists.");
                client.Email = newEmail;
            }
        }

        var updated = await repo.UpdateAsync(client);
        return updated is null ? null : ToDto(updated);
    }

    public Task<bool> DeleteAsync(int id, int userId) => repo.DeleteAsync(id, userId);

    private static ClientDto ToDto(Client c) => new()
    {
        Id    = c.Id,
        Name  = c.Name,
        Email = c.Email,
        Phone = c.Phone,
    };
}

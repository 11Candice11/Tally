using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Models;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerAPI2.Repositories;

// client repo is pretty simple, just CRUD with userId scoping
// FindByEmailAsync is used to check for duplicates before creating or updating
public class ClientRepository(AppDbContext db) : IClientRepository
{
    public Task<IEnumerable<Client>> GetAllAsync(int userId) =>
        db.Clients
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync()
            .ContinueWith(t => (IEnumerable<Client>)t.Result);

    public Task<Client?> GetByIdAsync(int id, int userId) =>
        db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    public Task<Client?> FindByEmailAsync(int userId, string email) =>
        db.Clients.FirstOrDefaultAsync(c => c.UserId == userId && c.Email == email);

    public async Task<Client> CreateAsync(Client client)
    {
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    public async Task<Client?> UpdateAsync(Client client)
    {
        db.Clients.Update(client);
        await db.SaveChangesAsync();
        return client;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (client is null) return false;
        db.Clients.Remove(client);
        await db.SaveChangesAsync();
        return true;
    }
}

using InvoiceTrackerAPI2.Models;

namespace InvoiceTrackerAPI2.Repositories.Interfaces;

public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync(int userId);
    Task<Client?>             GetByIdAsync(int id, int userId);
    Task<Client?>             FindByEmailAsync(int userId, string email);
    Task<Client>              CreateAsync(Client client);
    Task<Client?>             UpdateAsync(Client client);
    Task<bool>                DeleteAsync(int id, int userId);
}

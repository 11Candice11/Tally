using InvoiceTrackerAPI2.DTOs;

namespace InvoiceTrackerAPI2.Services.Interfaces;

public interface IClientService
{
    Task<IEnumerable<ClientDto>> GetAllAsync(int userId);
    Task<ClientDto?>             GetByIdAsync(int id, int userId);
    Task<ClientDto>              CreateAsync(int userId, CreateClientDto dto);
    Task<ClientDto?>             UpdateAsync(int id, int userId, UpdateClientDto dto);
    Task<bool>                   DeleteAsync(int id, int userId);
}

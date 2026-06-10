using System.Security.Claims;
using InvoiceTrackerAPI2.DTOs;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceTrackerAPI2.Controllers;

// all endpoints require a valid JWT, TryGetUserId pulls the user id out of the token claims
// each action is scoped to the logged in user so you cant touch other peoples clients
[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController(IClientService clientService) : ControllerBase
{
    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return int.TryParse(raw, out userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await clientService.GetAllAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var client = await clientService.GetByIdAsync(id, userId);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateClientDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var created = await clientService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
// conflict means duplicate email, the service throws InvalidOperationException for that
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateClientDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var updated = await clientService.UpdateAsync(id, userId, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var deleted = await clientService.DeleteAsync(id, userId);
        return deleted ? NoContent() : NotFound();
    }
}

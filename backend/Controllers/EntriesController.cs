using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/entries")]
public class EntriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public EntriesController(AppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    /// <summary>Guarda el evento y/o gasto ya confirmados por el usuario (RF-07). Todo o nada.</summary>
    [HttpPost]
    public async Task<ActionResult<ConfirmEntryResponse>> Confirm(ConfirmEntryRequest request)
    {
        if (request.Event is null && request.Expense is null)
        {
            return BadRequest("Hay que confirmar al menos un evento o un gasto.");
        }

        var category = request.Expense is null
            ? null
            : TextParserService.Categories.FirstOrDefault(c =>
                string.Equals(c, request.Expense.Category.Trim(), StringComparison.OrdinalIgnoreCase));
        if (request.Expense is not null && category is null)
        {
            return BadRequest($"Categoría inválida. Opciones: {string.Join(", ", TextParserService.Categories)}.");
        }

        // El dueño siempre es el usuario autenticado, nunca un valor del cuerpo (RNF-03).
        var userId = _currentUserService.UserId;

        Event? created = null;
        if (request.Event is not null)
        {
            created = new Event { UserId = userId, Title = request.Event.Title.Trim(), StartsAt = request.Event.StartsAt };
            _db.Events.Add(created);
        }

        Expense? expense = null;
        if (request.Expense is not null)
        {
            expense = new Expense
            {
                UserId = userId,
                Amount = request.Expense.Amount,
                Category = category!,
                Date = request.Expense.Date,
            };
            _db.Expenses.Add(expense);
        }

        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new ConfirmEntryResponse(created, expense));
    }
}

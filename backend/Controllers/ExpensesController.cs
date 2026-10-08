using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public ExpensesController(AppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses(DateTime? from, DateTime? to)
    {
        var userId = _currentUserService.UserId;
        var query = _db.Expenses.AsNoTracking().Where(e => e.UserId == userId);

        if (from is not null)
        {
            query = query.Where(e => e.Date >= from);
        }

        if (to is not null)
        {
            query = query.Where(e => e.Date < to);
        }

        return Ok(await query.OrderBy(e => e.Date).ToListAsync());
    }
}

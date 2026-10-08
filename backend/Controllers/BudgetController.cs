using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budget")]
public class BudgetController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public BudgetController(AppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<BudgetResponse>> GetBudget(int year, int month)
    {
        if (!IsValidPeriod(year, month))
        {
            return BadRequest("Año o mes inválido.");
        }

        var userId = _currentUserService.UserId;
        var budget = await _db.Budgets.AsNoTracking()
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Year == year && b.Month == month);

        if (budget is null)
        {
            return NotFound();
        }

        return Ok(await ToResponseAsync(budget));
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> CreateBudget(BudgetRequest request)
    {
        var userId = _currentUserService.UserId;
        var exists = await _db.Budgets
            .AnyAsync(b => b.UserId == userId && b.Year == request.Year && b.Month == request.Month);

        if (exists)
        {
            return Conflict("Ya existe un presupuesto para ese mes; usá PUT para editarlo.");
        }

        var budget = new Budget
        {
            UserId = userId,
            Year = request.Year,
            Month = request.Month,
            Amount = request.Amount
        };
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();

        var response = await ToResponseAsync(budget);
        return CreatedAtAction(nameof(GetBudget), new { year = budget.Year, month = budget.Month }, response);
    }

    [HttpPut]
    public async Task<ActionResult<BudgetResponse>> UpdateBudget(BudgetRequest request)
    {
        var userId = _currentUserService.UserId;
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Year == request.Year && b.Month == request.Month);

        if (budget is null)
        {
            return NotFound("No hay presupuesto definido para ese mes; usá POST para crearlo.");
        }

        budget.Amount = request.Amount;
        await _db.SaveChangesAsync();

        return Ok(await ToResponseAsync(budget));
    }

    private static bool IsValidPeriod(int year, int month) =>
        year is >= 2000 and <= 2100 && month is >= 1 and <= 12;

    private async Task<BudgetResponse> ToResponseAsync(Budget budget)
    {
        var start = new DateTime(budget.Year, budget.Month, 1);
        var end = start.AddMonths(1);

        var spent = await _db.Expenses
            .Where(e => e.UserId == budget.UserId && e.Date >= start && e.Date < end)
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

        return new BudgetResponse
        {
            Year = budget.Year,
            Month = budget.Month,
            Amount = budget.Amount,
            Spent = spent,
            Available = budget.Amount - spent
        };
    }
}

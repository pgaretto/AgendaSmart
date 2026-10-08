using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public EventsController(AppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Event>>> GetEvents(DateTime? from, DateTime? to)
    {
        var userId = _currentUserService.UserId;
        var query = _db.Events.AsNoTracking().Where(e => e.UserId == userId);

        if (from is not null)
        {
            query = query.Where(e => e.StartsAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(e => e.StartsAt < to);
        }

        return Ok(await query.OrderBy(e => e.StartsAt).ToListAsync());
    }
}

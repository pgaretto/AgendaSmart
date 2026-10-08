using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/parse")]
public class ParseController : ControllerBase
{
    private readonly ITextParserService _parser;

    public ParseController(ITextParserService parser) => _parser = parser;

    /// <summary>Interpreta la frase y devuelve la propuesta para el modal de confirmación. No guarda nada (RF-07).</summary>
    [HttpPost]
    [EnableRateLimiting("ai")]
    public async Task<ActionResult<ParseResponse>> Parse(ParseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("El texto no puede estar vacío.");
        }

        try
        {
            var result = await _parser.ParseAsync(request.Text, request.ClientNow ?? DateTime.Now, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex) when (ex is InvalidDataException or HttpRequestException or TaskCanceledException
                                       or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "No se pudo interpretar el texto. Probá de nuevo.");
        }
    }
}

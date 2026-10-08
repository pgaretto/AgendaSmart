using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Services;

public interface ITextParserService
{
    /// <summary>Interpreta una frase libre y extrae el evento y/o gasto que contiene.</summary>
    Task<ParseResponse> ParseAsync(string text, DateTime now, CancellationToken cancellationToken = default);
}

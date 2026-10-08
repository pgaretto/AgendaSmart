namespace SmartAgenda.Api.Services;

public interface IClaudeService
{
    /// <summary>Envía un prompt a Claude y devuelve el texto de la respuesta.</summary>
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}

using System.Globalization;
using System.Text.Json;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Services;

public class TextParserService : ITextParserService
{
    public static readonly string[] Categories = ["Comida", "Transporte", "Salidas", "Salud", "Otros"];

    private static readonly CultureInfo SpanishCulture = CultureInfo.GetCultureInfo("es-AR");

    private readonly IClaudeService _claude;

    public TextParserService(IClaudeService claude) => _claude = claude;

    public async Task<ParseResponse> ParseAsync(string text, DateTime now, CancellationToken cancellationToken = default)
    {
        var raw = await _claude.CompleteAsync(BuildSystemPrompt(now), text, cancellationToken);
        return ParseModelOutput(raw);
    }

    internal static string BuildSystemPrompt(DateTime now) => $$"""
        Extraés eventos y gastos de frases en español rioplatense escritas por un usuario.
        Fecha y hora actuales del usuario: {{now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}} ({{now.ToString("dddd", SpanishCulture)}}).
        Respondé SOLO con un objeto JSON, sin texto adicional ni bloques de código, con esta forma:
        {"event": {"title": string, "startsAt": "yyyy-MM-ddTHH:mm:ss"} | null, "expense": {"amount": number, "category": string} | null}
        Reglas:
        - "event" es null si la frase no menciona ningún evento; "expense" es null si no menciona ningún gasto.
        - title: nombre corto del evento, con la primera letra en mayúscula (ej. "Dentista", "Reunión con mamá").
        - startsAt: resolvé fechas relativas ("mañana", "el martes que viene") a partir de la fecha actual. Si no hay hora, usá 09:00:00.
        - "4 de la tarde" es 16:00; "a las 10" sin otra pista es 10:00.
        - amount: número en pesos sin símbolos ni separadores ("25 mil" es 25000, "2,5 lucas" es 2500).
        - category: exactamente una de: {{string.Join(", ", Categories)}}. Pasajes, SUBE y combustible son Transporte; cafés y comidas son Comida; médico, dentista y farmacia son Salud; bares, cine y salidas son Salidas; si ninguna aplica o el gasto es ambiguo (por ejemplo, un kiosco, donde no se sabe qué se compró), Otros.
        - No inventes datos que la frase no contenga.
        """;

    internal static ParseResponse ParseModelOutput(string raw)
    {
        var json = ExtractJsonObject(raw);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ParseResponse(ReadEvent(root), ReadExpense(root));
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            throw new InvalidDataException("La respuesta de la IA no tiene el formato esperado.", ex);
        }
    }

    private static ParsedEvent? ReadEvent(JsonElement root)
    {
        if (!root.TryGetProperty("event", out var e) || e.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var title = e.GetProperty("title").GetString()?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        var startsAt = DateTime.Parse(
            e.GetProperty("startsAt").GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.None);

        return new ParsedEvent(title, startsAt);
    }

    private static ParsedExpense? ReadExpense(JsonElement root)
    {
        if (!root.TryGetProperty("expense", out var x) || x.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var amount = x.GetProperty("amount").GetDecimal();
        if (amount <= 0)
        {
            return null;
        }

        var category = x.TryGetProperty("category", out var c) ? c.GetString() : null;
        var normalized = Categories.FirstOrDefault(k => string.Equals(k, category?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? "Otros";

        return new ParsedExpense(amount, normalized);
    }

    /// <summary>Tolera que el modelo envuelva el JSON en un bloque de código o agregue texto alrededor.</summary>
    private static string ExtractJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        return start >= 0 && end > start
            ? raw[start..(end + 1)]
            : throw new InvalidDataException("La respuesta de la IA no contiene un JSON.");
    }
}

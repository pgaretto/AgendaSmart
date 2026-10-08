using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SmartAgenda.Api.Services;
using Xunit.Abstractions;

namespace SmartAgenda.Api.Tests.Eval;

/// <summary>Se saltea salvo que exista la variable de entorno ANTHROPIC_EVAL_KEY (hace llamadas reales y cuesta plata).</summary>
public sealed class EvalFactAttribute : FactAttribute
{
    public EvalFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_EVAL_KEY")))
        {
            Skip = "Eval real contra Claude: definir ANTHROPIC_EVAL_KEY para correrlo.";
        }
    }
}

public record EvalCase(string Text, decimal? Amount, string? Category);

/// <summary>RNF-06: monto y categoría exactos en al menos 90% de un dataset de 50+ frases coloquiales.</summary>
public class ExtractionAccuracyEval
{
    private const double MinimumAccuracy = 0.90;
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0);

    private readonly ITestOutputHelper _output;

    public ExtractionAccuracyEval(ITestOutputHelper output) => _output = output;

    [EvalFact]
    public async Task Monto_y_categoria_coinciden_en_al_menos_90_por_ciento()
    {
        var cases = JsonSerializer.Deserialize<List<EvalCase>>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Eval", "phrases.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.True(cases.Count >= 50, "RNF-06 exige al menos 50 frases.");

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Anthropic:ApiKey"] = Environment.GetEnvironmentVariable("ANTHROPIC_EVAL_KEY"),
        }).Build();
        var parser = new TextParserService(new ClaudeService(config));

        using var gate = new SemaphoreSlim(5);
        var results = await Task.WhenAll(cases.Select(async c =>
        {
            await gate.WaitAsync();
            try
            {
                var r = await parser.ParseAsync(c.Text, Now);
                return (Case: c, Got: r.Expense, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Case: c, Got: null, Error: ex.Message);
            }
            finally
            {
                gate.Release();
            }
        }));

        var failures = results.Where(x => x.Error is not null
            || x.Got?.Amount != x.Case.Amount
            || x.Got?.Category != x.Case.Category).ToList();

        foreach (var f in failures)
        {
            _output.WriteLine($"FALLA: \"{f.Case.Text}\" esperado ({f.Case.Amount}, {f.Case.Category}) " +
                $"obtenido ({f.Got?.Amount}, {f.Got?.Category}) {f.Error}");
        }

        var accuracy = 1.0 - (double)failures.Count / cases.Count;
        _output.WriteLine($"Precisión: {accuracy:P1} ({cases.Count - failures.Count}/{cases.Count})");
        Assert.True(accuracy >= MinimumAccuracy, $"Precisión {accuracy:P1} < {MinimumAccuracy:P0}");
    }
}

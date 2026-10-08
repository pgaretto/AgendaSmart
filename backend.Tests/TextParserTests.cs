using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartAgenda.Api.Models;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Tests;

public class FakeClaudeService : IClaudeService
{
    public string Response { get; set; } = "{}";
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserMessage { get; private set; }

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        LastSystemPrompt = systemPrompt;
        LastUserMessage = userMessage;
        return Task.FromResult(Response);
    }
}

public class TextParserServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0);

    private static Task<ParseResponse> ParseAsync(string modelOutput) =>
        new TextParserService(new FakeClaudeService { Response = modelOutput }).ParseAsync("frase", Now);

    [Fact]
    public async Task Extrae_evento_y_gasto()
    {
        var result = await ParseAsync("""
            {"event":{"title":"Dentista","startsAt":"2026-10-13T16:00:00"},"expense":{"amount":25000,"category":"Salud"}}
            """);

        Assert.Equal(new ParsedEvent("Dentista", new DateTime(2026, 10, 13, 16, 0, 0)), result.Event);
        Assert.Equal(new ParsedExpense(25000m, "Salud"), result.Expense);
    }

    [Fact]
    public async Task Solo_gasto_no_crea_evento()
    {
        var result = await ParseAsync("""{"event":null,"expense":{"amount":2500,"category":"Comida"}}""");

        Assert.Null(result.Event);
        Assert.Equal(new ParsedExpense(2500m, "Comida"), result.Expense);
    }

    [Fact]
    public async Task Solo_evento_no_registra_gasto()
    {
        var result = await ParseAsync("""{"event":{"title":"Reunión con mamá","startsAt":"2026-10-09T10:00:00"},"expense":null}""");

        Assert.NotNull(result.Event);
        Assert.Null(result.Expense);
    }

    [Fact]
    public async Task Tolera_bloque_de_codigo_alrededor_del_json()
    {
        var result = await ParseAsync("```json\n{\"event\":null,\"expense\":{\"amount\":5000,\"category\":\"transporte\"}}\n```");

        Assert.Equal(new ParsedExpense(5000m, "Transporte"), result.Expense);
    }

    [Fact]
    public async Task Categoria_desconocida_cae_en_otros()
    {
        var result = await ParseAsync("""{"event":null,"expense":{"amount":100,"category":"Mascotas"}}""");

        Assert.Equal("Otros", result.Expense!.Category);
    }

    [Fact]
    public async Task Monto_no_positivo_se_descarta()
    {
        var result = await ParseAsync("""{"event":null,"expense":{"amount":0,"category":"Comida"}}""");

        Assert.Null(result.Expense);
    }

    [Theory]
    [InlineData("no sé qué decirte")]
    [InlineData("{\"event\":{\"title\":\"X\",\"startsAt\":\"no-es-fecha\"},\"expense\":null}")]
    [InlineData("{\"event\":null,\"expense\":{\"amount\":\"mucho\",\"category\":\"Comida\"}}")]
    public async Task Salida_invalida_lanza_InvalidDataException(string output)
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => ParseAsync(output));
    }

    [Fact]
    public async Task El_prompt_incluye_fecha_actual_y_categorias()
    {
        var fake = new FakeClaudeService { Response = """{"event":null,"expense":null}""" };

        await new TextParserService(fake).ParseAsync("mañana almuerzo", Now);

        Assert.Contains("2026-10-08 12:00", fake.LastSystemPrompt);
        Assert.Contains("jueves", fake.LastSystemPrompt);
        Assert.Contains("Comida, Transporte, Salidas, Salud, Otros", fake.LastSystemPrompt);
        Assert.Equal("mañana almuerzo", fake.LastUserMessage);
    }
}

public class ParseEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly FakeClaudeService _fake = new();

    public ParseEndpointTests(ApiFactory factory) => _factory = factory;

    private HttpClient FakedClient(HttpClient authed)
    {
        // Reutiliza el token del cliente autenticado sobre una factory con la IA simulada.
        var factory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.Replace(ServiceDescriptor.Singleton<IClaudeService>(_fake))));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = authed.DefaultRequestHeaders.Authorization;
        return client;
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/parse", new { text = "hola" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Devuelve_la_propuesta_sin_guardar_nada()
    {
        var (_, authed) = await _factory.CreateUserClientAsync("parse1@test.com");
        var client = FakedClient(authed);
        _fake.Response = """{"event":null,"expense":{"amount":2500,"category":"Comida"}}""";

        var response = await client.PostAsJsonAsync("/api/parse", new { text = "Me tomé un café 2500" });

        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<ParseResponse>())!;
        Assert.Null(body.Event);
        Assert.Equal(new ParsedExpense(2500m, "Comida"), body.Expense);

        var expenses = await authed.GetFromJsonAsync<List<Expense>>("/api/expenses");
        Assert.Empty(expenses!);
    }

    [Fact]
    public async Task Texto_vacio_devuelve_400()
    {
        var (_, authed) = await _factory.CreateUserClientAsync("parse2@test.com");

        var response = await FakedClient(authed).PostAsJsonAsync("/api/parse", new { text = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Respuesta_invalida_de_la_IA_devuelve_502()
    {
        var (_, authed) = await _factory.CreateUserClientAsync("parse3@test.com");
        var client = FakedClient(authed);
        _fake.Response = "basura";

        var response = await client.PostAsJsonAsync("/api/parse", new { text = "algo" });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}

public class ParseRateLimitTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ParseRateLimitTests(ApiFactory factory) => _factory = factory;

    private WebApplicationFactory<Program> Limited(int permits) => _factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("RateLimit:AiPermitPerMinute", permits.ToString());
        b.ConfigureServices(s => s.Replace(ServiceDescriptor.Singleton<IClaudeService>(
            new FakeClaudeService { Response = """{"event":null,"expense":null}""" })));
    });

    private static HttpClient As(WebApplicationFactory<Program> factory, HttpClient authed)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = authed.DefaultRequestHeaders.Authorization;
        return client;
    }

    [Fact]
    public async Task Supera_el_limite_y_recibe_429_con_Retry_After()
    {
        var factory = Limited(2);
        var (_, authed) = await _factory.CreateUserClientAsync($"{Guid.NewGuid():N}@test.com");
        var client = As(factory, authed);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/parse", new { text = "a" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/parse", new { text = "b" })).StatusCode);
        var limited = await client.PostAsJsonAsync("/api/parse", new { text = "c" });

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task El_limite_es_por_usuario()
    {
        var factory = Limited(1);
        var (_, fede) = await _factory.CreateUserClientAsync($"{Guid.NewGuid():N}@test.com");
        var (_, sofi) = await _factory.CreateUserClientAsync($"{Guid.NewGuid():N}@test.com");
        var fedeClient = As(factory, fede);
        var sofiClient = As(factory, sofi);

        await fedeClient.PostAsJsonAsync("/api/parse", new { text = "a" });
        var fedeSecond = await fedeClient.PostAsJsonAsync("/api/parse", new { text = "b" });
        var sofiFirst = await sofiClient.PostAsJsonAsync("/api/parse", new { text = "a" });

        Assert.Equal(HttpStatusCode.TooManyRequests, fedeSecond.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sofiFirst.StatusCode);
    }

    [Fact]
    public async Task Guardar_lo_confirmado_no_cuenta_para_el_limite()
    {
        var factory = Limited(1);
        var (_, authed) = await _factory.CreateUserClientAsync($"{Guid.NewGuid():N}@test.com");
        var client = As(factory, authed);

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/entries",
                new { expense = new { amount = 100, category = "Comida", date = DateTime.Today } });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}

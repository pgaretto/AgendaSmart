using System.Net;
using System.Net.Http.Json;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Tests;

public class EntriesEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EntriesEndpointTests(ApiFactory factory) => _factory = factory;

    private static string Email() => $"{Guid.NewGuid():N}@test.com";

    private static readonly DateTime Day = new(2026, 10, 13, 16, 0, 0);

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/entries",
            new { expense = new { amount = 100, category = "Comida", date = Day } });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Guarda_evento_y_gasto_del_usuario_autenticado()
    {
        var (userId, client) = await _factory.CreateUserClientAsync(Email());

        var response = await client.PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "Dentista", startsAt = Day },
            expense = new { amount = 25000, category = "salud", date = Day },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ConfirmEntryResponse>())!;
        Assert.Equal(userId, body.Event!.UserId);
        Assert.Equal(userId, body.Expense!.UserId);

        var events = await client.GetFromJsonAsync<List<Event>>("/api/events");
        var expenses = await client.GetFromJsonAsync<List<Expense>>("/api/expenses");
        Assert.Equal("Dentista", Assert.Single(events!).Title);
        var saved = Assert.Single(expenses!);
        Assert.Equal(25000m, saved.Amount);
        Assert.Equal("Salud", saved.Category);
    }

    [Fact]
    public async Task Solo_gasto_no_crea_evento()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var response = await client.PostAsJsonAsync("/api/entries",
            new { expense = new { amount = 2500, category = "Comida", date = Day } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<Event>>("/api/events"))!);
        Assert.Single((await client.GetFromJsonAsync<List<Expense>>("/api/expenses"))!);
    }

    [Fact]
    public async Task Solo_evento_no_registra_gasto()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var response = await client.PostAsJsonAsync("/api/entries",
            new { @event = new { title = "Reunión con mamá", startsAt = Day } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<List<Event>>("/api/events"))!);
        Assert.Empty((await client.GetFromJsonAsync<List<Expense>>("/api/expenses"))!);
    }

    [Fact]
    public async Task Sin_evento_ni_gasto_devuelve_400()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/entries", new { })).StatusCode);
    }

    [Theory]
    [InlineData(0, "Comida")]
    [InlineData(-5, "Comida")]
    [InlineData(100, "Mascotas")]
    [InlineData(100, "")]
    public async Task Gasto_invalido_devuelve_400_y_no_guarda_el_evento(decimal amount, string category)
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var response = await client.PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "Dentista", startsAt = Day },
            expense = new { amount, category, date = Day },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<Event>>("/api/events"))!);
    }

    [Fact]
    public async Task Evento_con_titulo_vacio_devuelve_400()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var response = await client.PostAsJsonAsync("/api/entries", new { @event = new { title = " ", startsAt = Day } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ignora_un_UserId_enviado_en_el_cuerpo_y_aisla_entre_usuarios()
    {
        var (fedeId, fede) = await _factory.CreateUserClientAsync(Email());
        var (sofiId, sofi) = await _factory.CreateUserClientAsync(Email());

        var response = await fede.PostAsJsonAsync("/api/entries", new
        {
            @event = new { title = "Privado de Fede", startsAt = Day, userId = sofiId },
            expense = new { amount = 100, category = "Comida", date = Day, userId = sofiId },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ConfirmEntryResponse>())!;
        Assert.Equal(fedeId, body.Event!.UserId);
        Assert.Equal(fedeId, body.Expense!.UserId);
        Assert.Empty((await sofi.GetFromJsonAsync<List<Event>>("/api/events"))!);
        Assert.Empty((await sofi.GetFromJsonAsync<List<Expense>>("/api/expenses"))!);
    }

    [Fact]
    public async Task El_gasto_confirmado_descuenta_del_presupuesto()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());
        await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 200000 });

        await client.PostAsJsonAsync("/api/entries",
            new { expense = new { amount = 50000, category = "Salud", date = Day } });

        var budget = (await client.GetFromJsonAsync<BudgetResponse>("/api/budget?year=2026&month=10"))!;
        Assert.Equal(150000m, budget.Available);
    }
}

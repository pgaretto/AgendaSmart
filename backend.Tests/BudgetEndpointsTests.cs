using System.Net;
using System.Net.Http.Json;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Tests;

public class BudgetEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public BudgetEndpointsTests(ApiFactory factory) => _factory = factory;

    private static string Email() => $"{Guid.NewGuid():N}@test.com";

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/budget?year=2026&month=10")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 100 })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 100 })).StatusCode);
    }

    [Fact]
    public async Task Post_crea_presupuesto_y_get_lo_devuelve()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var post = await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 200000 });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var get = await client.GetAsync("/api/budget?year=2026&month=10");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = (await get.Content.ReadFromJsonAsync<BudgetResponse>())!;
        Assert.Equal(200000m, body.Amount);
        Assert.Equal(0m, body.Spent);
        Assert.Equal(200000m, body.Available);
    }

    [Fact]
    public async Task Get_sin_presupuesto_devuelve_404()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/budget?year=2026&month=10")).StatusCode);
    }

    [Fact]
    public async Task Post_duplicado_devuelve_409()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());
        var payload = new { year = 2026, month = 10, amount = 1000 };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/budget", payload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/budget", payload)).StatusCode);
    }

    [Fact]
    public async Task Put_sin_presupuesto_devuelve_404()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        var put = await client.PutAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 1000 });

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task AC06_saldo_es_presupuesto_menos_gastos_del_mes()
    {
        var (userId, client) = await _factory.CreateUserClientAsync(Email());
        await _factory.SeedExpenseAsync(userId, 30000, new DateTime(2026, 10, 3));
        await _factory.SeedExpenseAsync(userId, 20000, new DateTime(2026, 10, 31, 23, 59, 0));
        await _factory.SeedExpenseAsync(userId, 9999, new DateTime(2026, 9, 30)); // mes anterior
        await _factory.SeedExpenseAsync(userId, 8888, new DateTime(2026, 11, 1)); // mes siguiente

        var post = await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 200000 });
        var body = (await post.Content.ReadFromJsonAsync<BudgetResponse>())!;

        Assert.Equal(50000m, body.Spent);
        Assert.Equal(150000m, body.Available);
    }

    [Fact]
    public async Task AC08_put_reemplaza_monto_y_recalcula_saldo()
    {
        var (userId, client) = await _factory.CreateUserClientAsync(Email());
        await _factory.SeedExpenseAsync(userId, 50000, new DateTime(2026, 10, 5));
        await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 200000 });

        var put = await client.PutAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 250000 });

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var body = (await put.Content.ReadFromJsonAsync<BudgetResponse>())!;
        Assert.Equal(250000m, body.Amount);
        Assert.Equal(200000m, body.Available);
    }

    [Fact]
    public async Task Gastos_sin_presupuesto_en_otro_mes_no_afectan_otro_mes()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());
        await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 100 });

        var other = await client.PostAsJsonAsync("/api/budget", new { year = 2026, month = 11, amount = 500 });

        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Theory]
    [InlineData(2026, 0, 100)]
    [InlineData(2026, 13, 100)]
    [InlineData(1999, 5, 100)]
    [InlineData(2026, 5, 0)]
    [InlineData(2026, 5, -10)]
    public async Task Datos_invalidos_devuelven_400(int year, int month, decimal amount)
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/budget", new { year, month, amount })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PutAsJsonAsync("/api/budget", new { year, month, amount })).StatusCode);
    }

    [Fact]
    public async Task Get_con_periodo_invalido_devuelve_400()
    {
        var (_, client) = await _factory.CreateUserClientAsync(Email());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/budget?year=2026&month=13")).StatusCode);
    }

    [Fact]
    public async Task RNF03_un_usuario_no_ve_ni_modifica_el_presupuesto_de_otro()
    {
        var (aId, a) = await _factory.CreateUserClientAsync(Email());
        var (bId, b) = await _factory.CreateUserClientAsync(Email());
        await _factory.SeedExpenseAsync(aId, 40000, new DateTime(2026, 10, 2));
        await a.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 100000 });

        // B no ve el presupuesto de A
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync("/api/budget?year=2026&month=10")).StatusCode);
        // B no puede editarlo
        Assert.Equal(HttpStatusCode.NotFound,
            (await b.PutAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 1 })).StatusCode);
        // B puede crear el suyo para el mismo mes, y sus gastos no incluyen los de A
        await _factory.SeedExpenseAsync(bId, 5, new DateTime(2026, 10, 2));
        var bPost = await b.PostAsJsonAsync("/api/budget", new { year = 2026, month = 10, amount = 777 });
        Assert.Equal(HttpStatusCode.Created, bPost.StatusCode);
        var bBody = (await bPost.Content.ReadFromJsonAsync<BudgetResponse>())!;
        Assert.Equal(5m, bBody.Spent);
        Assert.Equal(772m, bBody.Available);

        // El de A quedó intacto
        var aBody = (await (await a.GetAsync("/api/budget?year=2026&month=10")).Content.ReadFromJsonAsync<BudgetResponse>())!;
        Assert.Equal(100000m, aBody.Amount);
        Assert.Equal(60000m, aBody.Available);
    }
}

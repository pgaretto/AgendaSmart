using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartAgenda.Api.Services;

namespace SmartAgenda.Api.Tests;

public class ClaudeServiceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ClaudeServiceTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public void El_servicio_esta_registrado_en_la_api()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.IsType<ClaudeService>(scope.ServiceProvider.GetRequiredService<IClaudeService>());
    }

    [Fact]
    public async Task Sin_api_key_falla_con_un_mensaje_claro()
    {
        var config = new ConfigurationBuilder().Build();
        var service = new ClaudeService(config);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync("sistema", "hola"));

        Assert.Contains("Anthropic:ApiKey", ex.Message);
    }

    [Fact]
    public void Sin_api_key_no_rompe_el_arranque_ni_la_construccion()
    {
        var exception = Record.Exception(() => new ClaudeService(new ConfigurationBuilder().Build()));

        Assert.Null(exception);
    }
}

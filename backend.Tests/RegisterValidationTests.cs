using System.Net;
using System.Net.Http.Json;

namespace SmartAgenda.Api.Tests;

public class RegisterValidationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RegisterValidationTests(ApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("1")]
    [InlineData("1234567")]
    public async Task Contrasena_de_menos_de_8_caracteres_devuelve_400(string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@test.com", password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Contrasena_de_8_caracteres_se_acepta()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { email = $"{Guid.NewGuid():N}@test.com", password = "12345678" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

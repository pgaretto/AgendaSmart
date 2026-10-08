using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "clave-de-test-con-largo-suficiente-para-hs256-0123456789");
        builder.UseSetting("Jwt:Issuer", "SmartAgenda.Api");
        builder.UseSetting("Jwt:Audience", "SmartAgenda.Client");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused");

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType.FullName!.Contains("IDbContextOptionsConfiguration")
                       && d.ServiceType.GenericTypeArguments.FirstOrDefault() == typeof(AppDbContext))
                .ToList();
            foreach (var d in toRemove)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Registra un usuario, hace login y devuelve (userId, cliente autenticado).</summary>
    public async Task<(int UserId, HttpClient Client)> CreateUserClientAsync(string email)
    {
        var anon = CreateClient();
        var reg = await anon.PostAsJsonAsync("/api/auth/register", new { email, password = "Passw0rd!" });
        reg.EnsureSuccessStatusCode();
        var user = (await reg.Content.ReadFromJsonAsync<UserResponse>())!;

        var login = await anon.PostAsJsonAsync("/api/auth/login", new { email, password = "Passw0rd!" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (user.Id, client);
    }

    public async Task SeedExpenseAsync(int userId, decimal amount, DateTime date)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Expenses.Add(new Expense { UserId = userId, Amount = amount, Category = "Comida", Date = date });
        await db.SaveChangesAsync();
    }
}

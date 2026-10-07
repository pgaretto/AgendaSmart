using Microsoft.EntityFrameworkCore;
using SmartAgenda.Api.Data;
using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> RegisterAsync(string email, string password)
    {
        var emailInUse = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailInUse)
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }
}

using SmartAgenda.Api.Models;

namespace SmartAgenda.Api.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

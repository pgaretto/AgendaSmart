using System.ComponentModel.DataAnnotations;

namespace SmartAgenda.Api.Models;

public class RegisterRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SmartAgenda.Api.Models;

public class ParseRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = "";

    /// <summary>Fecha y hora local del usuario, para resolver "mañana", "el martes que viene", etc.</summary>
    public DateTime? ClientNow { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SmartAgenda.Api.Models;

public class ConfirmEventData
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = "";

    public DateTime StartsAt { get; set; }
}

public class ConfirmExpenseData
{
    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }

    [Required]
    public string Category { get; set; } = "";

    public DateTime Date { get; set; }
}

/// <summary>Datos revisados por el usuario en el modal de confirmación (RF-07). Al menos uno de los dos.</summary>
public class ConfirmEntryRequest
{
    public ConfirmEventData? Event { get; set; }

    public ConfirmExpenseData? Expense { get; set; }
}

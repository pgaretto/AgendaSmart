using System.ComponentModel.DataAnnotations;

namespace SmartAgenda.Api.Models;

public class BudgetRequest
{
    [Range(2000, 2100)]
    public int Year { get; set; }

    [Range(1, 12)]
    public int Month { get; set; }

    [Range(typeof(decimal), "0.01", "999999999999")]
    public decimal Amount { get; set; }
}

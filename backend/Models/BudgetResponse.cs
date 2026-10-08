namespace SmartAgenda.Api.Models;

public class BudgetResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Available { get; set; }
}

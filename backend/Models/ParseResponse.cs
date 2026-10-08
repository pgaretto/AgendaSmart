namespace SmartAgenda.Api.Models;

public record ParsedEvent(string Title, DateTime StartsAt);

public record ParsedExpense(decimal Amount, string Category);

/// <summary>Propuesta de la IA para el modal de confirmación (RF-07). No se persiste nada.</summary>
public record ParseResponse(ParsedEvent? Event, ParsedExpense? Expense);

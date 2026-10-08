namespace SmartAgenda.Api.Models;

public class Event
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Title { get; set; }
    public DateTime StartsAt { get; set; }
}

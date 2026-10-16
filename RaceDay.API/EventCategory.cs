namespace RaceDay.API.Models;

public class EventCategory
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
namespace RaceDay.API.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
}

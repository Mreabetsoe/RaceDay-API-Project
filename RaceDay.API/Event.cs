namespace RaceDay.API.Models;

public class Event
{
    public int Id { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int Capacity { get; set; }
    public string EventType { get; set; } = string.Empty; // Run, Walk, Cycle
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Organiser { get; set; } = null!;
    public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
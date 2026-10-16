namespace RaceDay.API.Models;

public class Enrolment
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }   // the category the participant chose
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Confirmed"; // Pending, Confirmed, Cancelled, Completed

    public Event Event { get; set; } = null!;
    public User User { get; set; } = null!;
    public EventCategory EventCategory { get; set; } = null!;
    public Result? Result { get; set; }
}

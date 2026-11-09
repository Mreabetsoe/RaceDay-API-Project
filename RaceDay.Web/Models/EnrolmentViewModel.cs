
namespace RaceDay.Web.Models;

public class EnrolmentViewModel
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = "";

    public int CategoryId { get; set; }

    public string Status { get; set; } = "";

    public DateTime EnrolmentDate { get; set; }
}

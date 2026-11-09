
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class ResultViewModel
{
    public int Id { get; set; }

    public int EnrolmentId { get; set; }

    public string EventName { get; set; } = "";

    public DateOnly EventDate { get; set; }

    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? Position { get; set; }
}

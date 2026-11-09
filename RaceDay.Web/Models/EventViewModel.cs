
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    public DateOnly EventDate { get; set; }

    [Required]
    public string Location { get; set; } = "";

    [Range(0.1, 1000)]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000)]
    public int Capacity { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    public string? BannerUrl { get; set; }
}
